using BookStore.Application.DTOs.Dashboard;
using BookStore.Application.Interfaces;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly AuthDbContext _authDb;

    public DashboardService(AppDbContext db, AuthDbContext authDb)
    {
        _db = db;
        _authDb = authDb;
    }

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        // 1. Fetch raw data from DB
        var totalUsers = await _authDb.Users.CountAsync();
        var totalProducts = await _db.Products.CountAsync();
        
        var orders = await _db.Orders
            .Select(o => new { o.Id, o.TotalAmount, o.Status, o.CreatedAt, o.OrderCode, o.ShippingRecipientName })
            .ToListAsync();

        var orderItems = await _db.OrderItems
            .Select(i => new { i.OrderId, i.ProductId, i.SnapshotTitle, i.Quantity, i.UnitPrice })
            .ToListAsync();

        // 2. Process logic in memory (Client-side)
        var activeOrders = orders.Where(o => o.Status != Domain.Enums.OrderStatus.Cancelled).ToList();
        var activeOrderIds = activeOrders.Select(o => o.Id).ToHashSet();
        var activeItems = orderItems.Where(i => activeOrderIds.Contains(i.OrderId)).ToList();
        
        var totalOrders = orders.Count;
        var totalRevenue = activeOrders.Sum(o => o.TotalAmount);

        // Revenue & Quantity Chart
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
        var startDate = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);
        
        var revenueChart = Enumerable.Range(0, 6)
            .Select(i => {
                var monthDate = startDate.AddMonths(i);
                var monthStr = monthDate.ToString("MM/yyyy");
                
                var monthOrders = activeOrders
                    .Where(o => o.CreatedAt.Month == monthDate.Month && o.CreatedAt.Year == monthDate.Year)
                    .ToList();
                
                var monthOrderIds = monthOrders.Select(o => o.Id).ToHashSet();
                
                var revenue = monthOrders.Sum(o => o.TotalAmount);
                var quantity = activeItems.Where(item => monthOrderIds.Contains(item.OrderId)).Sum(item => item.Quantity);
                
                return new MonthlyRevenueDto(monthStr, revenue, quantity);
            }).ToList();

        // Top Selling Products
        var topProducts = activeItems
            .GroupBy(i => new { i.ProductId, i.SnapshotTitle })
            .Select(g => new TopProductDto(
                g.Key.ProductId,
                g.Key.SnapshotTitle,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.UnitPrice * i.Quantity)
            ))
            .OrderByDescending(x => x.SoldCount)
            .Take(5)
            .ToList();

        // Recent Orders
        var recentOrders = orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new RecentOrderDto(
                o.Id,
                o.OrderCode,
                o.ShippingRecipientName,
                o.TotalAmount,
                o.Status.ToString(),
                o.CreatedAt
            ))
            .ToList();

        return new DashboardStatsDto(
            totalUsers,
            totalOrders,
            totalRevenue,
            totalProducts,
            revenueChart,
            topProducts,
            recentOrders
        );
    }
}
