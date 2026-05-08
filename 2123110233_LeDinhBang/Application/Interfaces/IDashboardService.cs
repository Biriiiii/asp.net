using BookStore.Application.DTOs.Dashboard;

namespace BookStore.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
}
