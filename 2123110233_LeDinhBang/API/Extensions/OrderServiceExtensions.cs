using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookStore.API.Extensions;

public static class OrderServiceExtensions
{
    public static IServiceCollection AddOrderModule(this IServiceCollection services)
    {
        // ── Repositories ──────────────────────────────────
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();

        // ── Services ──────────────────────────────────────
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderCodeGenerator, OrderCodeGenerator>();
        services.AddScoped<IShippingFeeService, ShippingFeeService>();
        services.AddScoped<IInventoryReserveService, InventoryReserveService>();

        // ── Payment Gateway (stub — đổi thành VNPayService khi production) ─
        services.AddScoped<IPaymentGatewayService, VnPayPaymentGatewayService>();

        // ── Background Job: tự hủy đơn PENDING hết hạn 15 phút ──
        services.AddHostedService<PendingOrderTimeoutJob>();

        return services;
    }
}

// ── Background Job ────────────────────────────────────────
public class PendingOrderTimeoutJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingOrderTimeoutJob> _logger;

    public PendingOrderTimeoutJob(
        IServiceScopeFactory scopeFactory,
        ILogger<PendingOrderTimeoutJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            try { await CancelExpiredOrdersAsync(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PendingOrderTimeoutJob error");
            }
        }
    }

    private async Task CancelExpiredOrdersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var inventory = scope.ServiceProvider.GetRequiredService<IInventoryReserveService>();
        var expiredBefore = DateTime.UtcNow.AddMinutes(-15);
        var expired = await orders.GetPendingExpiredAsync(expiredBefore);

        foreach (var order in expired)
        {
            order.Status = Domain.Enums.OrderStatus.Cancelled;
            order.CancelReason = "Tự động hủy do quá 15 phút chưa thanh toán.";
            order.CancelledAt = DateTime.UtcNow;

            await inventory.ReleaseAsync(order.Items.Select(i => (i.ProductId, i.Quantity)));
            orders.Update(order);
            await orders.SaveChangesAsync();

            _logger.LogInformation("Auto-cancelled expired order {Code}", order.OrderCode);
        }
    }
}