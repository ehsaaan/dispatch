using Dispatch.Application.Common;
using Dispatch.Application.Jobs;
using Dispatch.Application.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Dispatch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<Mediator>();
        services.AddScoped<IRequestHandler<ScheduleJobRequest, ScheduleJobResponse>, ScheduleJobHandler>();
        services.AddScoped<IRequestHandler<GetPendingJobsRequest, PagedJobResponse>, GetPendingJobsHandler>();
        services.AddScoped<IRequestHandler<GetJobByIdRequest, JobDto?>, GetJobByIdHandler>();
        services.AddScoped<IRequestHandler<CancelJobRequest, CancelJobResponse>, CancelJobHandler>();
        services.AddScoped<IRequestHandler<SendNotificationRequest, SendNotificationResponse>, SendNotificationHandler>();
        services.AddScoped<IRequestHandler<GetNotificationsRequest, PagedNotificationResponse>, GetNotificationsHandler>();
        return services;
    }
}
