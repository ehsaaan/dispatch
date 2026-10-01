using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Dispatch.Application;
using Dispatch.Infrastructure.Persistence;

namespace Dispatch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DispatchDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IJobRepository, EfJobRepository>();
        services.AddScoped<INotificationRepository, EfNotificationRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IEmailNotificationService, InMemoryEmailService>();
        services.AddScoped<ISmsNotificationService, InMemorySmsService>();
        services.AddScoped<IPushNotificationService, InMemoryPushService>();

        return services;
    }
}
