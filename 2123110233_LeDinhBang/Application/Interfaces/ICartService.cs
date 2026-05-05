using BookStore.Application.DTOs.Cart;

namespace BookStore.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid? userId, string? sessionId);
    Task<CartDto> AddItemAsync(Guid? userId, string? sessionId, AddToCartRequest request);
    Task<CartDto> UpdateItemAsync(Guid? userId, string? sessionId, Guid itemId, UpdateCartItemRequest request);
    Task<CartDto> RemoveItemAsync(Guid? userId, string? sessionId, Guid itemId);
    Task ClearCartAsync(Guid? userId, string? sessionId);
    Task MergeGuestCartAsync(string sessionId, Guid userId);  // Gọi khi guest đăng nhập
}
