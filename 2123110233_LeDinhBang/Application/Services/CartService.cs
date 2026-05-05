using BookStore.Application.DTOs.Cart;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _carts;
    private readonly IFlashSaleRepository _flashSales; // Thêm repository flash sale
    private readonly AppDbContext    _db;      
    private const int MaxDistinctItems = 20;

    public CartService(ICartRepository carts, IFlashSaleRepository flashSales, AppDbContext db)
    {
        _carts = carts;
        _flashSales = flashSales;
        _db    = db;
    }

    public async Task<CartDto> GetCartAsync(Guid? userId, string? sessionId)
    {
        Cart? cart = userId.HasValue
            ? await _carts.GetWithItemsAsync(userId.Value)
            : await _carts.GetWithItemsBySessionAsync(sessionId ?? "");

        if (cart == null) return EmptyCart();
        
        // Lấy flash sale đang diễn ra
        var activeFS = await _flashSales.GetActiveAsync();
        return MapCart(cart, activeFS);
    }

    public async Task<CartDto> AddItemAsync(Guid? userId, string? sessionId, AddToCartRequest req)
{
    // 1. Khởi tạo và kiểm tra sản phẩm
    var product = await _db.Products
        .IgnoreQueryFilters()
        .Include(p => p.Images)
        .Include(p => p.Inventory)
        .Include(p => p.ProductAuthors).ThenInclude(pa => pa.Author)
        .FirstOrDefaultAsync(p => p.Id == req.ProductId)
        ?? throw new KeyNotFoundException("Sản phẩm không tồn tại.");

    var stock = product.Inventory?.QtyAvailable - product.Inventory?.QtyReserved ?? 0;
    if (stock <= 0)
        throw new InvalidOperationException("Sản phẩm đã hết hàng.");

    // 2. Tìm Giỏ hàng (Chỉ tìm Cart, thao tác trực tiếp qua DbContext cho an toàn)
    var cart = userId.HasValue
        ? await _db.Set<Cart>().FirstOrDefaultAsync(c => c.UserId == userId.Value)
        : await _db.Set<Cart>().FirstOrDefaultAsync(c => c.SessionId == (sessionId ?? ""));

    // 3. Đảm bảo Giỏ hàng có thật dưới Database để lấy CartId
    if (cart == null)
    {
        cart = new Cart
        {
            UserId    = userId,
            SessionId = userId.HasValue ? null : sessionId,
            ExpiresAt = userId.HasValue ? null : DateTime.UtcNow.AddDays(7),
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<Cart>().Add(cart);
        await _db.SaveChangesAsync(); // Lưu nhịp 1: Để DB sinh ra cart.Id thật
    }
    else
    {
        cart.UpdatedAt = DateTime.UtcNow;
        _db.Set<Cart>().Update(cart); 
    }

    // 4. Tìm xem sản phẩm đã có trong giỏ chưa (Truy vấn độc lập)
    var cartItem = await _db.Set<CartItem>()
        .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == req.ProductId);

    if (cartItem != null)
    {
        // 4A. Đã có trong giỏ -> Cộng dồn số lượng
        var newQty = cartItem.Quantity + req.Quantity;
        if (newQty > 99)
            throw new InvalidOperationException("Số lượng tối đa mỗi sản phẩm là 99.");
        if (newQty > stock)
            throw new InvalidOperationException($"Chỉ còn {stock} sản phẩm trong kho.");
            
        cartItem.Quantity = newQty;
        _db.Set<CartItem>().Update(cartItem); // Chỉ định rõ cho EF Core: ĐÂY LÀ LỆNH UPDATE
    }
    else
    {
        // 4B. Chưa có trong giỏ -> Thêm mới
        var currentItemsCount = await _db.Set<CartItem>().CountAsync(i => i.CartId == cart.Id);
        if (currentItemsCount >= MaxDistinctItems) 
            throw new InvalidOperationException($"Giỏ hàng tối đa {MaxDistinctItems} sản phẩm khác nhau.");
        if (req.Quantity > stock)
            throw new InvalidOperationException($"Chỉ còn {stock} sản phẩm trong kho.");

        // Kiểm tra xem sản phẩm có đang Flash Sale không
        var activeFS = await _flashSales.GetActiveAsync();
        var fsItem = activeFS?.Items.FirstOrDefault(i => i.ProductId == req.ProductId && i.IsAvailable);
        
        var newItem = new CartItem
        {
            CartId    = cart.Id, 
            ProductId = req.ProductId,
            Quantity  = req.Quantity,
            UnitPrice = fsItem != null ? fsItem.SalePrice : (product.SalePrice > 0 ? product.SalePrice : product.OriginalPrice)
        };
        _db.Set<CartItem>().Add(newItem);
    }

    // 5. Lưu xuống DB (Nhịp 2)
    try
    {
        await _db.SaveChangesAsync();
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
    {
        // Bẫy lỗi an toàn
        return await GetCartAsync(userId, sessionId); 
    }

    // 6. Lấy lại toàn bộ Giỏ hàng
    var updated = userId.HasValue
        ? await _carts.GetWithItemsAsync(userId.Value)
        : await _carts.GetWithItemsBySessionAsync(sessionId ?? "");

    if (updated == null) return EmptyCart();
    var finalFS = await _flashSales.GetActiveAsync();
    return MapCart(updated, finalFS); 
}

    public async Task<CartDto> UpdateItemAsync(Guid? userId, string? sessionId, Guid itemId, UpdateCartItemRequest req)
    {
        var cart = await GetCartOrThrowAsync(userId, sessionId);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException("Sản phẩm không có trong giỏ hàng.");

        var stock = await GetStockAsync(item.ProductId);
        if (req.Quantity > stock)
            throw new InvalidOperationException($"Chỉ còn {stock} sản phẩm trong kho.");

        item.Quantity  = req.Quantity;
        cart.UpdatedAt = DateTime.UtcNow;
        _carts.Update(cart);
        await _carts.SaveChangesAsync();
        
        var activeFS = await _flashSales.GetActiveAsync();
        return MapCart(cart, activeFS);
    }

    public async Task<CartDto> RemoveItemAsync(Guid? userId, string? sessionId, Guid itemId)
    {
        var cart = await GetCartOrThrowAsync(userId, sessionId);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException("Sản phẩm không có trong giỏ hàng.");

        cart.Items.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        _carts.Update(cart);
        await _carts.SaveChangesAsync();

        var activeFS = await _flashSales.GetActiveAsync();
        return MapCart(cart, activeFS);
    }

    public async Task ClearCartAsync(Guid? userId, string? sessionId)
    {
        var cart = await GetCartOrThrowAsync(userId, sessionId);
        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        _carts.Update(cart);
        await _carts.SaveChangesAsync();
    }

    public async Task MergeGuestCartAsync(string sessionId, Guid userId)
    {
        var guestCart = await _carts.GetWithItemsBySessionAsync(sessionId);
        if (guestCart == null || !guestCart.Items.Any()) return;

        var userCart = await _carts.GetWithItemsAsync(userId);
        if (userCart == null)
        {
            guestCart.UserId    = userId;
            guestCart.SessionId = null;
            guestCart.ExpiresAt = null;
            _carts.Update(guestCart);
        }
        else
        {
            foreach (var guestItem in guestCart.Items)
            {
                var existing = userCart.Items.FirstOrDefault(i => i.ProductId == guestItem.ProductId);
                if (existing != null)
                    existing.Quantity = Math.Min(99, existing.Quantity + guestItem.Quantity);
                else
                    userCart.Items.Add(new CartItem
                    {
                        CartId    = userCart.Id,
                        ProductId = guestItem.ProductId,
                        Quantity  = guestItem.Quantity,
                        UnitPrice = guestItem.UnitPrice
                    });
            }
            userCart.UpdatedAt = DateTime.UtcNow;
            _carts.Update(userCart);
        }

        await _carts.SaveChangesAsync();
    }

    // ── Helpers ───────────────────────────────────────────

    private async Task<Cart> GetCartOrThrowAsync(Guid? userId, string? sessionId)
    {
        Cart? cart = userId.HasValue
            ? await _carts.GetWithItemsAsync(userId.Value)
            : await _carts.GetWithItemsBySessionAsync(sessionId ?? "");
        return cart ?? throw new KeyNotFoundException("Giỏ hàng không tồn tại.");
    }

    private async Task<int> GetStockAsync(Guid productId)
    {
        var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
        return inv == null ? 0 : inv.QtyAvailable - inv.QtyReserved;
    }

    private static CartDto EmptyCart() =>
        new(Guid.Empty, Enumerable.Empty<CartItemDto>(), 0, 0, DateTime.UtcNow);

    private static CartDto MapCart(Cart cart, FlashSale? activeFS = null) =>
        new(cart.Id,
            cart.Items.Select(i => {
                // Ưu tiên lấy giá Flash Sale nếu có
                var fsItem = activeFS?.Items.FirstOrDefault(f => f.ProductId == i.ProductId && f.IsAvailable);
                var unitPrice = fsItem != null ? fsItem.SalePrice : i.UnitPrice;
                
                return new CartItemDto(
                    i.Id,
                    i.ProductId,
                    i.Product?.Title ?? "",
                    i.Product?.Images?.FirstOrDefault(img => img.IsPrimary)?.ImageUrl
                        ?? i.Product?.Images?.OrderBy(img => img.DisplayOrder).FirstOrDefault()?.ImageUrl,
                    i.Product?.ProductAuthors != null
                        ? string.Join(", ", i.Product.ProductAuthors.Select(pa => pa.Author?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)))
                        : null,
                    i.Quantity,
                    unitPrice,
                    unitPrice * i.Quantity,
                    i.Product?.Inventory != null
                        ? (i.Product.Inventory.QtyAvailable - i.Product.Inventory.QtyReserved) > 0
                        : false,
                    i.Product?.Inventory != null
                        ? Math.Max(0, i.Product.Inventory.QtyAvailable - i.Product.Inventory.QtyReserved)
                        : 0
                );
            }),
            cart.Items.Sum(i => {
                var fsItem = activeFS?.Items.FirstOrDefault(f => f.ProductId == i.ProductId && f.IsAvailable);
                return (fsItem != null ? fsItem.SalePrice : i.UnitPrice) * i.Quantity;
            }),
            cart.Items.Sum(i => i.Quantity),
            cart.UpdatedAt);
}
