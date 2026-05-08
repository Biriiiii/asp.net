namespace BookStore.Application.DTOs.Dashboard;

public record DashboardStatsDto(
    long TotalUsers,
    long TotalOrders,
    decimal TotalRevenue,
    long TotalProducts,
    IEnumerable<MonthlyRevenueDto> RevenueChart,
    IEnumerable<TopProductDto> TopProducts,
    IEnumerable<RecentOrderDto> RecentOrders
);

public record MonthlyRevenueDto(string Month, decimal Revenue, int Quantity); // Added Quantity

public record TopProductDto(Guid ProductId, string Title, int SoldCount, decimal Revenue);

public record RecentOrderDto(Guid Id, string OrderCode, string CustomerName, decimal TotalAmount, string Status, DateTime CreatedAt);
