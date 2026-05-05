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
    private readonly IFlashSaleRepository     _flashSales;
    private readonly IInventoryReserveService _inventory;
    private readonly IShippingFeeService      _shippingFee;
    private readonly IPaymentGatewayService   _payment;
    private readonly IOrderCodeGenerator      _codeGen;
    private readonly AppDbContext             _db;

    public OrderService(
        IOrderRepository orders, IRefundRepository refunds,
        ICartRepository carts, IVoucherRepository vouchers,
        IFlashSaleRepository flashSales,
        IInventoryReserveService inventory, IShippingFeeService shippingFee,
        IPaymentGatewayService payment, IOrderCodeGenerator codeGen,
        AppDbContext db)
    {
        _orders = orders; _refunds = refunds; _carts = carts;
        _vouchers = vouchers; _flashSales = flashSales;
        _inventory = inventory;
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

        var activeFS = await _flashSales.GetActiveAsync();
        var subtotal = items.Sum(i => {
            var fsItem = activeFS?.Items.FirstOrDefault(f => f.ProductId == i.ProductId && f.IsAvailable);
            return (fsItem != null ? fsItem.SalePrice : i.UnitPrice) * i.Quantity;
        });

        var weightGram  = items.Sum(i => (i.Product?.WeightGram ?? 300) * i.Quantity);
        var shippingFee = await _shippingFee.CalculateAsync(req.ShippingAddress.Province, req.ShippingMethod, weightGram);

        var (discount, _) = await GetVoucherDiscountAsync(req.VoucherCode, userId, subtotal);

        return new CheckoutSummaryDto(subtotal, shippingFee, discount,
            subtotal + shippingFee - discount, req.VoucherCode, discount);
    }

    public async Task<OrderDetailDto> CreateAsync(Guid userId, string? sessionId, CreateOrderRequest req)
    {
        var cart = await _db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Images)
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.ProductAuthors).ThenInclude(pa => pa.Author)
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Inventory)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any())
            throw new InvalidOperationException("Giỏ hàng trống.");

        var cartItems = req.CartItemIds?.Any() == true
            ? cart.Items.Where(i => req.CartItemIds.Contains(i.Id) || req.CartItemIds.Contains(i.ProductId)).ToList()
            : cart.Items.ToList();

        if (!cartItems.Any())
            throw new InvalidOperationException("Không có sản phẩm nào được chọn.");

        if (!await _inventory.CheckAvailabilityAsync(cartItems.Select(i => (i.ProductId, i.Quantity))))
            throw new InvalidOperationException("Một số sản phẩm đã hết hàng.");

        var activeFS = await _flashSales.GetActiveAsync();
        var subtotal = cartItems.Sum(i => {
            var fsItem = activeFS?.Items.FirstOrDefault(f => f.ProductId == i.ProductId && f.IsAvailable);
            return (fsItem != null ? fsItem.SalePrice : i.UnitPrice) * i.Quantity;
        });

        var weightGram = cartItems.Sum(i => (i.Product?.WeightGram ?? 300) * i.Quantity);
        var shippingFee = await _shippingFee.CalculateAsync(req.ShippingAddress.Province, req.ShippingMethod, weightGram);
        var (discount, voucherId) = await GetVoucherDiscountAsync(req.VoucherCode, userId, subtotal);
        var total = subtotal + shippingFee - discount;

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

        foreach (var ci in cartItems)
        {
            var fsItem = activeFS?.Items.FirstOrDefault(f => f.ProductId == ci.ProductId && f.IsAvailable);
            var finalPrice = fsItem != null ? fsItem.SalePrice : ci.UnitPrice;

            order.Items.Add(new OrderItem
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = finalPrice,
                SnapshotTitle = ci.Product?.Title ?? "",
                SnapshotIsbn = ci.Product?.Isbn,
                SnapshotCoverUrl = ci.Product?.Images?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                SnapshotAuthorNames = ci.Product?.ProductAuthors != null
                    ? string.Join(", ", ci.Product.ProductAuthors.Select(pa => pa.Author?.Name ?? ""))
                    : null
            });

            if (fsItem != null)
            {
                fsItem.SoldCount += ci.Quantity;
                _db.Set<FlashSaleItem>().Update(fsItem);
            }
        }

        order.Payments.Add(new Payment
        {
            Provider = req.PaymentMethod.ToString().ToLower(),
            IdempotencyKey = $"{order.OrderCode}_{req.PaymentMethod}",
            Amount = total,
            ExpiredAt = req.PaymentMethod == PaymentMethod.COD ? null : DateTime.UtcNow.AddMinutes(15)
        });

        order.StatusLogs.Add(new OrderStatusLog { FromStatus = OrderStatus.Pending, ToStatus = OrderStatus.Pending, Note = "Đơn hàng vừa được tạo" });

        if (req.PaymentMethod == PaymentMethod.COD)
        {
            order.Status = OrderStatus.Confirmed;
            order.ConfirmedAt = DateTime.UtcNow;
            order.PaymentStatus = PaymentStatus.Unpaid;
            order.StatusLogs.Add(new OrderStatusLog { FromStatus = OrderStatus.Pending, ToStatus = OrderStatus.Confirmed, Note = "Đơn hàng đã xác nhận" });
        }

        await _inventory.ReserveAsync(cartItems.Select(i => (i.ProductId, i.Quantity)));
        
        // ── Ghi nhận sử dụng Voucher ──────────────────────────
        if (voucherId.HasValue)
        {
            await _vouchers.IncrementUsageAsync(voucherId.Value, userId, order.Id, discount);
        }

        await _orders.AddAsync(order);
        await _orders.SaveChangesAsync();
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
        var order = await _orders.GetDetailAsync(orderId) ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");
        if (order.UserId != userId) throw new UnauthorizedAccessException("Bạn không có quyền xem đơn hàng này.");
        return MapDetail(order);
    }

    public async Task<OrderDetailDto> CancelAsync(Guid userId, Guid orderId, CancelOrderRequest req)
    {
        var order = await _orders.GetDetailAsync(orderId) ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");
        if (order.UserId != userId) throw new UnauthorizedAccessException("Bạn không có quyền hủy đơn hàng này.");
        if (!order.CanCancel) throw new InvalidOperationException("Không thể hủy đơn hàng.");
        await CancelInternalAsync(order, req.Reason, userId);
        return MapDetail(order);
    }

    public async Task<string> CreatePaymentUrlAsync(Guid userId, Guid orderId, string returnUrl)
    {
        var order = await _orders.GetDetailAsync(orderId) ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");
        if (order.UserId != userId) throw new UnauthorizedAccessException("Quyền truy cập bị từ chối.");
        return await _payment.CreatePaymentUrlAsync(order.OrderCode, order.TotalAmount, order.PaymentMethod, returnUrl);
    }

    public async Task HandlePaymentCallbackAsync(PaymentCallbackRequest cb)
    {
        if (!_payment.VerifyCallback(cb)) throw new InvalidOperationException("Chữ ký không hợp lệ.");
        var order = await _db.Orders.Include(o => o.Payments).FirstOrDefaultAsync(o => o.OrderCode == cb.OrderCode) ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        if (order.PaymentStatus == PaymentStatus.Paid) return;
        var payment = order.Payments.FirstOrDefault(p => p.Provider == cb.Provider.ToLower());
        if (payment == null) return;

        if (cb.Status == "success")
        {
            payment.Status = PaymentStatus.Paid;
            payment.TransactionId = cb.TransactionId;
            payment.PaidAt = DateTime.UtcNow;
            order.Status = OrderStatus.Confirmed;
            order.PaymentStatus = PaymentStatus.Paid;
            order.ConfirmedAt = DateTime.UtcNow;
            order.StatusLogs.Add(new OrderStatusLog { FromStatus = order.Status, ToStatus = OrderStatus.Confirmed, Note = "Thanh toán thành công" });
        }
        else payment.Status = PaymentStatus.Failed;
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<OrderListItemDto>> GetAllAsync(OrderQueryParams q)
    {
        var (items, total) = await _orders.GetPagedAsync(MapFilter(q));
        return new PagedResult<OrderListItemDto>(items.Select(MapListItem), total, q.Page, q.PageSize);
    }

    public async Task<OrderDetailDto> GetDetailAsync(Guid orderId)
    {
        var order = await _orders.GetDetailAsync(orderId) ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");
        return MapDetail(order);
    }

    public async Task<OrderDetailDto> UpdateStatusAsync(Guid orderId, AdminUpdateOrderStatusRequest req, Guid staffId)
    {
        var order = await _orders.GetDetailAsync(orderId) ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        
        ValidateTransition(order.Status, req.Status);
        
        if (req.Status == OrderStatus.Delivered) 
        {
            await _inventory.CommitAsync(order.Items.Select(i => (i.ProductId, i.Quantity)));
        }

        // Sử dụng Reload để tránh Concurrency
        await _db.Entry(order).ReloadAsync();

        var oldStatus = order.Status;
        order.Status = req.Status;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.OrderStatusLogs.AddAsync(new OrderStatusLog 
        { 
            OrderId = orderId,
            FromStatus = oldStatus, 
            ToStatus = req.Status, 
            Note = req.Note, 
            ChangedBy = staffId 
        });

        await _db.SaveChangesAsync();
        return await GetDetailAsync(orderId);
    }

    public async Task<OrderDetailDto> AssignShipmentAsync(Guid orderId, UpdateShipmentRequest req, Guid staffId)
    {
        var order = await _orders.GetDetailAsync(orderId) ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");
        
        // Sử dụng Reload để tránh Concurrency
        await _db.Entry(order).ReloadAsync();

        order.Shipments.Add(new Shipment { Carrier = req.Carrier, TrackingNumber = req.TrackingNumber, Status = ShipmentStatus.PickedUp, ShippedAt = DateTime.UtcNow, EstimatedDelivery = req.EstimatedDelivery });
        
        var oldStatus = order.Status;
        order.Status = OrderStatus.Shipping;

        await _db.OrderStatusLogs.AddAsync(new OrderStatusLog 
        { 
            OrderId = orderId,
            FromStatus = oldStatus, 
            ToStatus = OrderStatus.Shipping, 
            Note = $"{req.Carrier} - {req.TrackingNumber}", 
            ChangedBy = staffId 
        });
        
        await _db.SaveChangesAsync();
        return MapDetail(order);
    }

    public async Task<RefundDto> ProcessRefundAsync(Guid orderId, ProcessRefundRequest req, Guid adminId)
    {
        var order = await _orders.GetDetailAsync(orderId) ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");
        var refund = order.RefundRequest ?? throw new InvalidOperationException("Không có yêu cầu hoàn tiền.");
        refund.Status = req.Approved ? RefundStatus.Completed : RefundStatus.Rejected;
        refund.ProcessedAt = DateTime.UtcNow;
        if (req.Approved) { order.PaymentStatus = PaymentStatus.Refunded; order.Status = OrderStatus.Refunded; }
        await _orders.SaveChangesAsync();
        return MapRefund(refund);
    }

    private async Task CancelInternalAsync(Order order, string reason, Guid? cancelledBy = null)
    {
        // 1. Hoàn trả số lượng tồn kho (Trả lại từ Reserved về Available)
        await _inventory.ReleaseAsync(order.Items.Select(i => (i.ProductId, i.Quantity)));

        // 2. Hoàn trả lượt dùng Voucher nếu có
        if (order.VoucherId.HasValue)
        {
            await _vouchers.DecrementUsageAsync(order.VoucherId.Value, order.UserId);
        }

        // 3. Cập nhật trạng thái đơn hàng (Sử dụng Reload để tránh Concurrency)
        await _db.Entry(order).ReloadAsync(); 
        
        var oldStatus = order.Status;
        order.Status = OrderStatus.Cancelled;
        order.CancelReason = reason;
        order.CancelledAt = DateTime.UtcNow;

        // 4. Ghi log lịch sử trạng thái (Thêm trực tiếp vào DbSet để an toàn)
        await _db.OrderStatusLogs.AddAsync(new OrderStatusLog 
        { 
            OrderId = order.Id,
            FromStatus = oldStatus, 
            ToStatus = OrderStatus.Cancelled, 
            Note = reason, 
            ChangedBy = cancelledBy 
        });

        // 5. Lưu thay đổi
        await _db.SaveChangesAsync();
    }

    private async Task<(decimal Discount, Guid? VoucherId)> GetVoucherDiscountAsync(string? code, Guid userId, decimal subtotal)
    {
        if (string.IsNullOrWhiteSpace(code)) return (0, null);
        var v = await _vouchers.GetByCodeAsync(code) ?? throw new InvalidOperationException("Voucher không tồn tại.");
        if (!v.IsActive || DateTime.UtcNow < v.StartDate || DateTime.UtcNow > v.EndDate) throw new InvalidOperationException("Voucher hết hạn.");
        decimal discount = v.DiscountType == "percent" ? Math.Min(subtotal * v.DiscountValue / 100, v.MaxDiscountAmount ?? decimal.MaxValue) : Math.Min(v.DiscountValue, subtotal);
        return (discount, v.Id);
    }

    private static void ValidateTransition(OrderStatus from, OrderStatus to)
    {
        var allowed = new Dictionary<OrderStatus, HashSet<OrderStatus>> {
            [OrderStatus.Pending] = new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new() { OrderStatus.Processing, OrderStatus.Shipping, OrderStatus.Cancelled },
            [OrderStatus.Processing] = new() { OrderStatus.Shipping },
            [OrderStatus.Shipping] = new() { OrderStatus.Delivered },
            [OrderStatus.Delivered] = new() { OrderStatus.Refunded },
        };
        if (!allowed.ContainsKey(from) || !allowed[from].Contains(to)) throw new InvalidOperationException("Chuyển đổi trạng thái không hợp lệ.");
    }

    private static OrderFilter MapFilter(OrderQueryParams q) => new() { Keyword = q.Keyword, Status = q.Status, Page = q.Page, PageSize = q.PageSize };
    private static OrderListItemDto MapListItem(Order o) => new(o.Id, o.OrderCode, o.Status.ToString(), o.ShippingRecipientName, o.ShippingPhone, o.PaymentMethod.ToString(), o.PaymentStatus.ToString(), o.Items.Sum(i => i.Quantity), o.TotalAmount, o.CreatedAt, o.ConfirmedAt);
    private static OrderDetailDto MapDetail(Order o) => new(o.Id, o.OrderCode, o.Status.ToString(), o.CanCancel, new ShippingAddressDto(o.ShippingRecipientName, o.ShippingPhone, o.ShippingProvince, o.ShippingDistrict, o.ShippingWard, o.ShippingAddressLine), o.ShippingMethod.ToString(), o.ShippingFee, o.Subtotal, o.DiscountAmount, o.TotalAmount, o.PaymentMethod.ToString(), o.PaymentStatus.ToString(), o.Note, o.CancelReason, o.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.SnapshotTitle, i.SnapshotIsbn, i.SnapshotCoverUrl, i.SnapshotAuthorNames, i.Quantity, i.UnitPrice, i.DiscountAmount, i.LineTotal)), o.Payments.Select(p => new PaymentDto(p.Id, p.Provider, p.TransactionId, p.Amount, p.Status.ToString(), p.FailureReason, p.PaidAt, p.ExpiredAt, p.CreatedAt)), o.Shipments.Select(s => new ShipmentDto(s.Id, s.Carrier, s.TrackingNumber, s.Status.ToString(), s.ShippedAt, s.EstimatedDelivery, s.DeliveredAt, null)).FirstOrDefault(), o.StatusLogs.Select(l => new OrderStatusLogDto(l.FromStatus.ToString(), l.ToStatus.ToString(), l.Note, l.CreatedAt)), o.RefundRequest == null ? null : MapRefund(o.RefundRequest), o.CreatedAt, o.ConfirmedAt, o.CancelledAt);
    private static RefundDto MapRefund(RefundRequest r) => new(r.Id, r.Amount, r.Reason, r.Status.ToString(), r.AdminNote, r.TransactionId, r.ProcessedAt, r.CreatedAt);
}

public class OrderCodeGenerator : IOrderCodeGenerator
{
    private readonly AppDbContext _db;
    public OrderCodeGenerator(AppDbContext db) => _db = db;
    public async Task<string> GenerateAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"DH{today}";
        var last = await _db.Orders.Where(o => o.OrderCode.StartsWith(prefix)).OrderByDescending(o => o.OrderCode).Select(o => o.OrderCode).FirstOrDefaultAsync();
        var seq = last == null ? 1 : int.Parse(last[^4..]) + 1;
        return $"{prefix}{seq:D4}";
    }
}

public class ShippingFeeService : IShippingFeeService
{
    public Task<decimal> CalculateAsync(string province, ShippingMethod method, decimal weightGram)
    {
        decimal fee = method == ShippingMethod.Standard ? 25000 : (method == ShippingMethod.Express ? 45000 : 60000);
        if (province.Contains("HCM") || province.Contains("Hà Nội")) fee -= 5000;
        return Task.FromResult(Math.Max(0, fee));
    }
}

public class InventoryReserveService : IInventoryReserveService
{
    private readonly AppDbContext _db;
    public InventoryReserveService(AppDbContext db) => _db = db;
    public async Task<bool> CheckAvailabilityAsync(IEnumerable<(Guid ProductId, int Qty)> items)
    {
        foreach (var (productId, qty) in items)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
            if (inv == null || inv.QtyAvailable < qty) return false;
        }
        return true;
    }
    public async Task ReserveAsync(IEnumerable<(Guid ProductId, int Qty)> items)
    {
        foreach (var (productId, qty) in items)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
            if (inv != null) { inv.QtyAvailable -= qty; inv.QtyReserved += qty; }
        }
    }
    public async Task CommitAsync(IEnumerable<(Guid ProductId, int Qty)> items)
    {
        foreach (var (productId, qty) in items)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
            if (inv != null) { inv.QtyReserved -= qty; inv.QtySold += qty; }
        }
    }
    public async Task ReleaseAsync(IEnumerable<(Guid ProductId, int Qty)> items)
    {
        foreach (var (productId, qty) in items)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
            if (inv != null) { inv.QtyAvailable += qty; inv.QtyReserved -= qty; }
        }
    }
}