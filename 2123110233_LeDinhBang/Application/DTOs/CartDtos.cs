using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.DTOs.Cart;

// ── Responses ─────────────────────────────────────────────

public record CartDto(
    Guid Id,
    IEnumerable<CartItemDto> Items,
    decimal Subtotal,
    int TotalItems,
    DateTime UpdatedAt
);

public record CartItemDto(
    Guid Id,
    Guid ProductId,
    string Title,
    string? CoverUrl,
    string? AuthorNames,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool InStock,
    int StockAvailable
);

// ── Requests ──────────────────────────────────────────────

public class AddToCartRequest
{
    [Required] public Guid ProductId { get; set; }
    [Range(1, 99)] public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    [Range(1, 99)] public int Quantity { get; set; }
}
