using Dispatch.Application;
using Dispatch.Application.Jobs;
using Dispatch.Application.Notifications;
using Dispatch.Domain;
using Dispatch.Infrastructure;
using Dispatch.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("Default") ?? "Data Source=dispatch.db");
builder.Services.AddHostedService<JobSchedulerService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DispatchDbContext>();
    dbContext.Database.EnsureCreated();
}

// Health check
app.MapGet("/", () => Results.Json(new { service = "Dispatch", status = "ok" }));

// Schedule a job
app.MapPost("/jobs", async (ScheduleJobRequest request, Mediator mediator) =>
{
    var response = await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(request);
    return Results.Created($"/jobs/{response.JobId}", response);
});

// Get pending jobs
app.MapGet("/jobs", async (
    [AsParameters] GetPendingJobsRequest request,
    Mediator mediator) =>
{
    var response = await mediator.Send<GetPendingJobsRequest, PagedJobResponse>(request);
    return Results.Ok(response);
});

// Get job by ID
app.MapGet("/jobs/{id}", async (Guid id, Mediator mediator) =>
{
    var job = await mediator.Send<GetJobByIdRequest, JobDto?>(new GetJobByIdRequest(id));
    if (job is null) return Results.NotFound();
    return Results.Ok(job);
});

// Cancel a job
app.MapPost("/jobs/{id}/cancel", async (Guid id, Mediator mediator) =>
{
    var response = await mediator.Send<CancelJobRequest, CancelJobResponse>(new CancelJobRequest(id));
    return Results.Ok(response);
});

// Send a notification
app.MapPost("/notifications", async (SendNotificationRequest request, Mediator mediator) =>
{
    var response = await mediator.Send<SendNotificationRequest, SendNotificationResponse>(request);
    return Results.Created($"/notifications/{response.NotificationId}", response);
});

// Get notifications
app.MapGet("/notifications", async (
    [AsParameters] GetNotificationsRequest request,
    Mediator mediator) =>
{
    var response = await mediator.Send<GetNotificationsRequest, PagedNotificationResponse>(request);
    return Results.Ok(response);
});

// Seed demo data
app.MapPost("/demo/seed", async (Mediator mediator) =>
{
    await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(
        new ScheduleJobRequest("Send Welcome Email", "user@example.com", JobType.Email, JobPriority.High));
    await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(
        new ScheduleJobRequest("Send SMS Alert", "+1234567890", JobType.Sms, JobPriority.Critical));
    await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(
        new ScheduleJobRequest("Send Push Notification", "device-token-123", JobType.Push, JobPriority.Normal));
    await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(
        new ScheduleJobRequest("Data Sync", "{}", JobType.DataSync));
    await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(
        new ScheduleJobRequest("Generate Report", "monthly-report", JobType.ReportGeneration));
    await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(
        new ScheduleJobRequest("Cleanup Old Records", "{}", JobType.Cleanup));

    await mediator.Send<SendNotificationRequest, SendNotificationResponse>(
        new SendNotificationRequest("admin@example.com", "System Alert", "Server load above 80%", "Email"));
    await mediator.Send<SendNotificationRequest, SendNotificationResponse>(
        new SendNotificationRequest("+1234567890", "SMS Alert", "Database backup completed", "Sms"));

    return Results.Ok(new { seeded = 8 });
});

app.Run();

public partial class Program { }
