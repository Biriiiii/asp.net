using BookStore.Application.DTOs.Order;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Services;

// ── OrderService ──────────────────────────────────────────

public class OrderService : IOrderService
{
    private readonly IOrderRepository         _orders;
    private readonly IRefundRepository        _refunds;
    private readonly ICartRepository          _carts;
    private readonly IVoucherRepository       _vouchers;
    private readonly IInventoryReserveService _inventory;
    private readonly IShippingFeeService      _shippingFee;
    private readonly IPaymentGatewayService   _payment;
    private readonly IOrderCodeGenerator      _codeGen;
    private readonly AppDbContext             _db;

    public OrderService(
        IOrderRepository orders, IRefundRepository refunds,
        ICartRepository carts, IVoucherRepository vouchers,
        IInventoryReserveService inventory, IShippingFeeService shippingFee,
        IPaymentGatewayService payment, IOrderCodeGenerator codeGen,
        AppDbContext db)
    {
        _orders = orders; _refunds = refunds; _carts = carts;
        _vouchers = vouchers; _inventory = inventory;
        _shippingFee = shippingFee; _payment = payment; _codeGen = codeGen;
        _db = db;
    }

    public async Task<CheckoutSummaryDto> PreviewAsync(Guid userId, CreateOrderRequest req)
    {
        var cart = await _carts.GetWithItemsAsync(userId)
            ?? throw new InvalidOperationException("Giỏ hàng trống.");

        var items = req.CartItemIds?.Any() == true
            ? cart.Items.Where(i => req.CartItemIds.Contains(i.Id) || req.CartItemIds.Contains(i.ProductId)).ToList()
            : cart.Items.ToList();

        var subtotal    = items.Sum(i => i.UnitPrice * i.Quantity);
        var weightGram  = items.Sum(i => (i.Product?.WeightGram ?? 300) * i.Quantity);
        var shippingFee = await _shippingFee.CalculateAsync(req.ShippingAddress.Province, req.ShippingMethod, weightGram);

        var (discount, _) = await GetVoucherDiscountAsync(req.VoucherCode, userId, subtotal);

        return new CheckoutSummaryDto(subtotal, shippingFee, discount,
            subtotal + shippingFee - discount, req.VoucherCode, discount);
    }

    public async Task<OrderDetailDto> CreateAsync(Guid userId, string? sessionId, CreateOrderRequest req)
    {
        // Query trực tiếp vào DB (bypass repository) để đảm bảo tìm đúng cart
        var cart = await _db.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Images)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.ProductAuthors)
                        .ThenInclude(pa => pa.Author)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Inventory)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        // Dùng query đơn giản hơn nếu trên fail
        if (cart == null)
            cart = await _db.Carts
                .IgnoreQueryFilters()
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Images)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Inventory)
                .FirstOrDefaultAsync(c => c.UserId == userId);

        // Fallback: tìm theo sessionId
        if ((cart == null || !cart.Items.Any()) && !string.IsNullOrEmpty(sessionId))
            cart = await _db.Carts
                .IgnoreQueryFilters()
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);


        if (cart == null || !cart.Items.Any())
            throw new InvalidOperationException("Giỏ hàng của bạn trên hệ thống đang trống. Vui lòng nhấn 'Thêm vào giỏ hàng' lại để đồng bộ.");

        List<CartItem> cartItems;

        if (req.CartItemIds?.Any() == true)
        {
            // Thử match theo ItemId trước
            cartItems = cart.Items.Where(i => req.CartItemIds.Contains(i.Id)).ToList();

            // Nếu không match được (FE có thể gửi ProductId), thử match theo ProductId
            if (!cartItems.Any())
                cartItems = cart.Items.Where(i => req.CartItemIds.Contains(i.ProductId)).ToList();

            // Nếu vẫn không match, lấy toàn bộ cart
            if (!cartItems.Any())
                cartItems = cart.Items.ToList();
        }
        else
        {
            // Không có filter → lấy toàn bộ
            cartItems = cart.Items.ToList();
        }

        if (!cartItems.Any())
            throw new InvalidOperationException("Không có sản phẩm nào được chọn.");

        // 1. Kiểm tra tồn kho
        if (!await _inventory.CheckAvailabilityAsync(cartItems.Select(i => (i.ProductId, i.Quantity))))
            throw new InvalidOperationException("Một số sản phẩm đã hết hàng.");

        var subtotal = cartItems.Sum(i => i.UnitPrice * i.Quantity);
        var weightGram = cartItems.Sum(i => (i.Product?.WeightGram ?? 300) * i.Quantity);
        var shippingFee = await _shippingFee.CalculateAsync(req.ShippingAddress.Province, req.ShippingMethod, weightGram);
        var (discount, voucherId) = await GetVoucherDiscountAsync(req.VoucherCode, userId, subtotal);
        var total = subtotal + shippingFee - discount;

        // 2. Tạo đơn
        var order = new Order
        {
            UserId = userId,
            OrderCode = await _codeGen.GenerateAsync(),
            ShippingRecipientName = req.ShippingAddress.RecipientName.Trim(),
            ShippingPhone = req.ShippingAddress.Phone.Trim(),
            ShippingProvince = req.ShippingAddress.Province.Trim(),
            ShippingDistrict = req.ShippingAddress.District.Trim(),
            ShippingWard = req.ShippingAddress.Ward.Trim(),
            ShippingAddressLine = req.ShippingAddress.AddressLine.Trim(),
            ShippingMethod = req.ShippingMethod,
            ShippingFee = shippingFee,
            Subtotal = subtotal,
            DiscountAmount = discount,
            TotalAmount = total,
            PaymentMethod = req.PaymentMethod,
            VoucherId = voucherId,
            Note = req.Note
        };

        // 3. Thêm items (snapshot)
        foreach (var ci in cartItems)
            order.Items.Add(new OrderItem
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice,
                SnapshotTitle = ci.Product?.Title ?? "",
                SnapshotIsbn = ci.Product?.Isbn,
                SnapshotCoverUrl = ci.Product?.Images?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                SnapshotAuthorNames = ci.Product?.ProductAuthors != null
                    ? string.Join(", ", ci.Product.ProductAuthors.Select(pa => pa.Author?.Name ?? ""))
                    : null
            });

        // 4. Payment record
        order.Payments.Add(new Payment
        {
            Provider = req.PaymentMethod.ToString().ToLower(),
            IdempotencyKey = $"{order.OrderCode}_{req.PaymentMethod}",
            Amount = total,
            ExpiredAt = req.PaymentMethod == PaymentMethod.COD ? null
                              : DateTime.UtcNow.AddMinutes(15)
        });

        // 5. Status log ban đầu
        order.StatusLogs.Add(new OrderStatusLog
        {
            FromStatus = OrderStatus.Pending,
            ToStatus = OrderStatus.Pending,
            Note = "Đơn hàng vừa được tạo"
        });

        // 🚀 BƯỚC ĐỘT PHÁ: XỬ LÝ COD TRÊN MEMORY TRƯỚC KHI LƯU 🚀
        if (req.PaymentMethod == PaymentMethod.COD)
        {
            order.Status = OrderStatus.Confirmed;
            order.ConfirmedAt = DateTime.UtcNow;
            order.PaymentStatus = PaymentStatus.Unpaid;

            order.StatusLogs.Add(new OrderStatusLog
            {
                FromStatus = OrderStatus.Pending,
                ToStatus = OrderStatus.Confirmed,
                Note = "Đơn hàng đã xác nhận"
            });
        }

        // 6. Reserve tồn kho tạm
        await _inventory.ReserveAsync(cartItems.Select(i => (i.ProductId, i.Quantity)));

        // 7. LƯU ĐƠN HÀNG XUỐNG DB (CHỈ 1 LẦN DUY NHẤT)
        await _orders.AddAsync(order);
        await _orders.SaveChangesAsync();

        // 8. Xóa cart items đã checkout
        await _carts.RemoveItemsAsync(cart.Id, cartItems.Select(i => i.Id));
        await _carts.SaveChangesAsync();

        return await GetDetailAsync(order.Id);
    }

    public async Task<PagedResult<OrderListItemDto>> GetMyOrdersAsync(Guid userId, OrderQueryParams q)
    {
        var (items, total) = await _orders.GetByUserAsync(userId, MapFilter(q));
        return new PagedResult<OrderListItemDto>(items.Select(MapListItem), total, q.Page, q.PageSize);
    }

    public async Task<OrderDetailDto> GetMyDetailAsync(Guid userId, Guid orderId)
    {
        var order = await _orders.GetDetailAsync(orderId)
            ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");
        if (order.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền xem đơn hàng này.");
        return MapDetail(order);
    }

    public async Task<OrderDetailDto> CancelAsync(Guid userId, Guid orderId, CancelOrderRequest req)
    {
        var order = await _orders.GetDetailAsync(orderId)
            ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");
        if (order.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền hủy đơn hàng này.");
        if (!order.CanCancel)
            throw new InvalidOperationException("Không thể hủy đơn hàng ở trạng thái hiện tại.");

        await CancelInternalAsync(order, req.Reason, userId);
        return MapDetail(order);
    }


    public async Task<string> CreatePaymentUrlAsync(Guid userId, Guid orderId, string returnUrl)
    {
        var order = await _orders.GetDetailAsync(orderId)
            ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");
        if (order.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền thanh toán đơn hàng này.");
        if (order.PaymentMethod == PaymentMethod.COD)
            throw new InvalidOperationException("Đơn COD không cần thanh toán online.");
        if (order.PaymentStatus == PaymentStatus.Paid)
            throw new InvalidOperationException("Đơn hàng đã được thanh toán.");

        return await _payment.CreatePaymentUrlAsync(
            order.OrderCode,
            order.TotalAmount,
            order.PaymentMethod,
            returnUrl);
    }
    public async Task HandlePaymentCallbackAsync(PaymentCallbackRequest cb)
    {
        if (!_payment.VerifyCallback(cb))
            throw new InvalidOperationException("Chữ ký thanh toán không hợp lệ.");

        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.OrderCode == cb.OrderCode)
            ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");

        if (order.PaymentStatus == PaymentStatus.Paid) return;

        var payment = order.Payments.FirstOrDefault(p => p.Provider == cb.Provider.ToLower());
        if (payment == null) return;

        if (cb.Status == "success")
        {
            await _db.Payments
                .Where(p => p.Id == payment.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, PaymentStatus.Paid)
                    .SetProperty(p => p.TransactionId, cb.TransactionId)
                    .SetProperty(p => p.PaidAt, DateTime.UtcNow)
                    .SetProperty(p => p.ProviderResponse, cb.RawData)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

            await _db.Orders
                .Where(o => o.Id == order.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.Status, OrderStatus.Confirmed)
                    .SetProperty(o => o.PaymentStatus, PaymentStatus.Paid)
                    .SetProperty(o => o.ConfirmedAt, DateTime.UtcNow)
                    .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));

            await _db.OrderStatusLogs.AddAsync(new OrderStatusLog
            {
                OrderId = order.Id,
                FromStatus = order.Status,
                ToStatus = OrderStatus.Confirmed,
                Note = "Đơn hàng đã xác nhận"
            });
        }
        else
        {
            await _db.Payments
                .Where(p => p.Id == payment.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, PaymentStatus.Failed)
                    .SetProperty(p => p.FailureReason, cb.RawData)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
        }

        await _db.SaveChangesAsync();
    }
    public async Task<PagedResult<OrderListItemDto>> GetAllAsync(OrderQueryParams q)
    {
        var (items, total) = await _orders.GetPagedAsync(MapFilter(q));
        return new PagedResult<OrderListItemDto>(items.Select(MapListItem), total, q.Page, q.PageSize);
    }

    public async Task<OrderDetailDto> GetDetailAsync(Guid orderId)
    {
        var order = await _orders.GetDetailAsync(orderId)
            ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");
        return MapDetail(order);
    }

    public async Task<OrderDetailDto> UpdateStatusAsync(Guid orderId, AdminUpdateOrderStatusRequest req, Guid staffId)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng này.");

        ValidateTransition(order.Status, req.Status);

        if (req.Status == OrderStatus.Delivered)
        {
            await _inventory.CommitAsync(order.Items.Select(i => (i.ProductId, i.Quantity)));
        }

        var affected = req.Status == OrderStatus.Delivered
            ? await _db.Orders
                .Where(o => o.Id == orderId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.Status, req.Status)
                    .SetProperty(o => o.PaymentStatus, PaymentStatus.Paid)
                    .SetProperty(o => o.UpdatedAt, DateTime.UtcNow))
            : await _db.Orders
                .Where(o => o.Id == orderId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.Status, req.Status)
                    .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));

        if (affected == 0)
            throw new KeyNotFoundException("Không tìm thấy đơn hàng này.");

        await _db.OrderStatusLogs.AddAsync(new OrderStatusLog
        {
            OrderId = orderId,
            FromStatus = order.Status,
            ToStatus = req.Status,
            Note = req.Note,
            ChangedBy = staffId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return await GetDetailAsync(orderId);
    }
    public async Task<OrderDetailDto> AssignShipmentAsync(Guid orderId, UpdateShipmentRequest req, Guid staffId)
    {
        var order = await _orders.GetDetailAsync(orderId)
            ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");

        if (order.Status != OrderStatus.Processing)
            throw new InvalidOperationException("Chỉ gán vận đơn khi đơn đang Processing.");

        order.Shipments.Add(new Shipment
        {
            Carrier = req.Carrier, TrackingNumber = req.TrackingNumber,
            Status = ShipmentStatus.PickedUp, ShippedAt = DateTime.UtcNow,
            EstimatedDelivery = req.EstimatedDelivery
        });
        order.Status = OrderStatus.Shipping;
        order.StatusLogs.Add(new OrderStatusLog
        {
            FromStatus = OrderStatus.Processing, ToStatus = OrderStatus.Shipping,
            Note = $"{req.Carrier} - {req.TrackingNumber}", ChangedBy = staffId
        });

        await _orders.SaveChangesAsync();
        return MapDetail(order);
    }

    public async Task<RefundDto> ProcessRefundAsync(Guid orderId, ProcessRefundRequest req, Guid adminId)
    {
        var order = await _orders.GetDetailAsync(orderId)
            ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");

        var refund = order.RefundRequest
            ?? throw new InvalidOperationException("Không có yêu cầu hoàn tiền.");

        if (refund.Status != RefundStatus.Pending)
            throw new InvalidOperationException("Yêu cầu hoàn tiền đã được xử lý.");

        refund.Status        = req.Approved ? RefundStatus.Completed : RefundStatus.Rejected;
        refund.AdminNote     = req.AdminNote;
        refund.TransactionId = req.TransactionId;
        refund.ProcessedAt   = DateTime.UtcNow;
        refund.ProcessedBy   = adminId;

        if (req.Approved)
        {
            order.PaymentStatus = PaymentStatus.Refunded;
            order.Status        = OrderStatus.Refunded;
        }

        await _orders.SaveChangesAsync();
        return MapRefund(refund);
    }

    // ── Private helpers ───────────────────────────────────

    private async Task ConfirmInternalAsync(Order order)
    {
        var prevStatus = order.Status;
        order.Status = OrderStatus.Confirmed;
        order.ConfirmedAt = DateTime.UtcNow;
        order.PaymentStatus = order.PaymentMethod == PaymentMethod.COD
            ? PaymentStatus.Unpaid : PaymentStatus.Paid;
        order.StatusLogs.Add(new OrderStatusLog
        {
            FromStatus = prevStatus,
            ToStatus = OrderStatus.Confirmed,
            Note = "Đơn hàng đã xác nhận"
        });

        // 🚫 KHÔNG ĐƯỢC GỌI _orders.Update(order) Ở ĐÂY 🚫

        await _orders.SaveChangesAsync();
    }

    private async Task CancelInternalAsync(Order order, string reason, Guid? cancelledBy = null)
    {
        var orderId = order.Id;
        var prevStatus = order.Status;
        var paymentStatus = order.PaymentStatus;
        var totalAmount = order.TotalAmount;
        var items = order.Items.Select(i => (i.ProductId, i.Quantity)).ToList();

        await _inventory.ReleaseAsync(items);

        _db.ChangeTracker.Clear();

        var affected = await _db.Orders
            .Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, OrderStatus.Cancelled)
                .SetProperty(o => o.CancelReason, reason)
                .SetProperty(o => o.CancelledAt, DateTime.UtcNow)
                .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));

        if (affected == 0)
            throw new KeyNotFoundException("Đơn hàng không tồn tại hoặc đã bị thay đổi.");

        await _db.OrderStatusLogs.AddAsync(new OrderStatusLog
        {
            OrderId = orderId,
            FromStatus = prevStatus,
            ToStatus = OrderStatus.Cancelled,
            Note = reason,
            ChangedBy = cancelledBy
        });

        if (paymentStatus == PaymentStatus.Paid)
        {
            await _db.RefundRequests.AddAsync(new RefundRequest
            {
                OrderId = orderId,
                Reason = "Khách hủy đơn hàng.",
                Amount = totalAmount
            });
        }

        await _db.SaveChangesAsync();
    }

    private async Task<(decimal Discount, Guid? VoucherId)> GetVoucherDiscountAsync(
        string? code, Guid userId, decimal subtotal)
    {
        if (string.IsNullOrWhiteSpace(code)) return (0, null);

        var voucher = await _vouchers.GetByCodeAsync(code)
            ?? throw new InvalidOperationException($"Mã voucher '{code}' không tồn tại.");

        if (!voucher.IsActive || DateTime.UtcNow < voucher.StartDate || DateTime.UtcNow > voucher.EndDate)
            throw new InvalidOperationException("Voucher đã hết hạn.");

        if (subtotal < voucher.MinOrderValue)
            throw new InvalidOperationException($"Đơn tối thiểu {voucher.MinOrderValue:N0}đ.");

        if (voucher.TotalUsageLimit > 0 && voucher.UsedCount >= voucher.TotalUsageLimit)
            throw new InvalidOperationException("Voucher đã hết lượt dùng.");

        var userUsed = await _vouchers.GetUserUsageCountAsync(voucher.Id, userId);
        if (voucher.PerUserLimit > 0 && userUsed >= voucher.PerUserLimit)
            throw new InvalidOperationException("Bạn đã dùng hết lượt voucher này.");

        decimal discount = voucher.DiscountType == "percent"
            ? Math.Min(subtotal * voucher.DiscountValue / 100, voucher.MaxDiscountAmount ?? decimal.MaxValue)
            : Math.Min(voucher.DiscountValue, subtotal);

        return (discount, voucher.Id);
    }

    private static void ValidateTransition(OrderStatus from, OrderStatus to)
    {
        var allowed = new Dictionary<OrderStatus, HashSet<OrderStatus>>
        {
            [OrderStatus.Pending]    = new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed]  = new() { OrderStatus.Processing, OrderStatus.Shipping, OrderStatus.Cancelled },
            [OrderStatus.Processing] = new() { OrderStatus.Shipping },
            [OrderStatus.Shipping]   = new() { OrderStatus.Delivered },
            [OrderStatus.Delivered]  = new() { OrderStatus.Refunded },
            [OrderStatus.Cancelled]  = new() { OrderStatus.Refunded },
        };
        if (!allowed.TryGetValue(from, out var ok) || !ok.Contains(to))
            throw new InvalidOperationException($"Không thể chuyển trạng thái {from} → {to}.");
    }

    private static OrderFilter MapFilter(OrderQueryParams q) => new()
    {
        Keyword = q.Keyword, Status = q.Status, PaymentStatus = q.PaymentStatus,
        PaymentMethod = q.PaymentMethod, FromDate = q.FromDate, ToDate = q.ToDate,
        SortBy = q.SortBy, Page = q.Page, PageSize = q.PageSize
    };

    private static OrderListItemDto MapListItem(Order o) =>
        new(o.Id, o.OrderCode, o.Status.ToString(),
            o.ShippingRecipientName, o.ShippingPhone,
            o.PaymentMethod.ToString(), o.PaymentStatus.ToString(),
            o.Items.Sum(i => i.Quantity),
            o.TotalAmount, o.CreatedAt, o.ConfirmedAt);

    private static OrderDetailDto MapDetail(Order o) =>
        new(o.Id, o.OrderCode, o.Status.ToString(), o.CanCancel,
            new ShippingAddressDto(o.ShippingRecipientName, o.ShippingPhone,
                o.ShippingProvince, o.ShippingDistrict, o.ShippingWard, o.ShippingAddressLine),
            o.ShippingMethod.ToString(), o.ShippingFee,
            o.Subtotal, o.DiscountAmount, o.TotalAmount,
            o.PaymentMethod.ToString(), o.PaymentStatus.ToString(),
            o.Note, o.CancelReason,
            o.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.SnapshotTitle, i.SnapshotIsbn,
                i.SnapshotCoverUrl, i.SnapshotAuthorNames, i.Quantity, i.UnitPrice, i.DiscountAmount, i.LineTotal)),
            o.Payments.Select(p => new PaymentDto(p.Id, p.Provider, p.TransactionId, p.Amount,
                p.Status.ToString(), p.FailureReason, p.PaidAt, p.ExpiredAt, p.CreatedAt)),
            o.Shipments.Select(s => new ShipmentDto(s.Id, s.Carrier, s.TrackingNumber, s.Status.ToString(),
                s.ShippedAt, s.EstimatedDelivery, s.DeliveredAt,
                s.TrackingEvents.OrderByDescending(e => e.OccurredAt)
                    .Select(e => new TrackingEventDto(e.Status, e.Description, e.Location, e.OccurredAt)))).FirstOrDefault(),
            o.StatusLogs.OrderBy(l => l.CreatedAt)
                .Select(l => new OrderStatusLogDto(l.FromStatus.ToString(), l.ToStatus.ToString(), l.Note, l.CreatedAt)),
            o.RefundRequest == null ? null : MapRefund(o.RefundRequest),
            o.CreatedAt, o.ConfirmedAt, o.CancelledAt);

    private static RefundDto MapRefund(RefundRequest r) =>
        new(r.Id, r.Amount, r.Reason, r.Status.ToString(),
            r.AdminNote, r.TransactionId, r.ProcessedAt, r.CreatedAt);
}

// ── OrderCodeGenerator ────────────────────────────────────

public class OrderCodeGenerator : IOrderCodeGenerator
{
    private readonly AppDbContext _db;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public OrderCodeGenerator(AppDbContext db) => _db = db;

    public async Task<string> GenerateAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var today   = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix  = $"DH{today}";
            var last    = await _db.Orders
                .Where(o => o.OrderCode.StartsWith(prefix))
                .OrderByDescending(o => o.OrderCode)
                .Select(o => o.OrderCode)
                .FirstOrDefaultAsync();
            var seq = last == null ? 1 : int.Parse(last[^4..]) + 1;
            return $"{prefix}{seq:D4}";
        }
        finally { _lock.Release(); }
    }
}

// ── ShippingFeeService ────────────────────────────────────

public class ShippingFeeService : IShippingFeeService
{
    private static readonly Dictionary<ShippingMethod, decimal> BaseFees = new()
    {
        [ShippingMethod.Standard] = 25_000m,
        [ShippingMethod.Express]  = 45_000m,
        [ShippingMethod.SameDay]  = 60_000m,
    };

    public Task<decimal> CalculateAsync(string province, ShippingMethod method, decimal weightGram)
    {
        var fee   = BaseFees[method];
        var extra = weightGram > 500 ? Math.Floor((weightGram - 500) / 500) * 5_000m : 0;
        var total = fee + extra;

        if (province.ToUpper().Contains("HCM") || province.Contains("Hà Nội"))
            total = Math.Max(0, total - 5_000m);

        return Task.FromResult(total);
    }
}

// ── InventoryReserveService ───────────────────────────────

public class InventoryReserveService : IInventoryReserveService
{
    private readonly AppDbContext _db;
    private readonly ILogger<InventoryReserveService> _logger;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public InventoryReserveService(AppDbContext db, ILogger<InventoryReserveService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<bool> CheckAvailabilityAsync(IEnumerable<(Guid ProductId, int Qty)> items)
    {
        foreach (var (productId, qty) in items)
        {
            var inv = await _db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ProductId == productId);

            if (inv == null || inv.QtyAvailable < qty)
                return false;
        }
        return true;
    }

    public async Task ReserveAsync(IEnumerable<(Guid ProductId, int Qty)> items)
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var (productId, qty) in items)
            {
                var affected = await _db.Inventories
                    .Where(i => i.ProductId == productId && i.QtyAvailable >= qty)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(i => i.QtyAvailable, i => i.QtyAvailable - qty)
                        .SetProperty(i => i.QtyReserved, i => i.QtyReserved + qty)
                        .SetProperty(i => i.UpdatedAt, DateTime.UtcNow));

                if (affected == 0)
                    throw new InvalidOperationException($"Sản phẩm {productId} không đủ tồn kho để đặt.");
            }
        }
        finally { _lock.Release(); }
    }

    public async Task CommitAsync(IEnumerable<(Guid ProductId, int Qty)> items)
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var (productId, qty) in items)
            {
                await _db.Inventories
                    .Where(i => i.ProductId == productId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(i => i.QtyReserved, i => i.QtyReserved - qty < 0 ? 0 : i.QtyReserved - qty)
                        .SetProperty(i => i.QtySold, i => i.QtySold + qty)
                        .SetProperty(i => i.UpdatedAt, DateTime.UtcNow));
            }
        }
        finally { _lock.Release(); }
    }

    public async Task ReleaseAsync(IEnumerable<(Guid ProductId, int Qty)> items)
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var (productId, qty) in items)
            {
                await _db.Inventories
                    .Where(i => i.ProductId == productId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(i => i.QtyAvailable, i => i.QtyAvailable + qty)
                        .SetProperty(i => i.QtyReserved, i => i.QtyReserved - qty < 0 ? 0 : i.QtyReserved - qty)
                        .SetProperty(i => i.UpdatedAt, DateTime.UtcNow));

                _logger.LogInformation(
                    "Released {Qty} reserved stock for product {ProductId}", qty, productId);
            }
        }
        finally { _lock.Release(); }
    }
}