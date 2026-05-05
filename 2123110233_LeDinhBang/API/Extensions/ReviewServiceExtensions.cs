using BookStore.Application.Interfaces;
using BookStore.Application.Services;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Repositories;

namespace BookStore.API.Extensions;

public static class ReviewServiceExtensions
{
    public static IServiceCollection AddReviewModule(this IServiceCollection services)
    {
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewService,    ReviewService>();
        return services;
    }
}
