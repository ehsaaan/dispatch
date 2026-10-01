using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dispatch.Application;
using Dispatch.Application.Common;
using Dispatch.Application.Jobs;
using Dispatch.Application.Notifications;
using Dispatch.Domain;
using Dispatch.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dispatch.Tests;

public class DispatchIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DispatchIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DispatchDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<DispatchDbContext>(options =>
                    options.UseSqlite("Data Source=dispatch-test.db"));

                using var scope = services.BuildServiceProvider().CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DispatchDbContext>();
                dbContext.Database.EnsureCreated();
            });
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        if (File.Exists("dispatch-test.db"))
            File.Delete("dispatch-test.db");
    }

    private void ClearDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DispatchDbContext>();
        dbContext.Jobs.RemoveRange(dbContext.Jobs);
        dbContext.Notifications.RemoveRange(dbContext.Notifications);
        dbContext.SaveChanges();
    }

    [Fact]
    public async Task ScheduleJob_ShouldCreatePendingJob()
    {
        ClearDatabase();
        var mediator = await GetMediator();

        var request = new ScheduleJobRequest(
            "Test Job",
            "{\"key\":\"value\"}",
            JobType.Email,
            JobPriority.Normal,
            3);

        var response = await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(request);
        response.JobId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPendingJobs_ShouldReturnScheduledJobs()
    {
        ClearDatabase();
        var mediator = await GetMediator();

        await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(
            new ScheduleJobRequest("Job 1", "{}", JobType.Email, JobPriority.High));
        await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(
            new ScheduleJobRequest("Job 2", "{}", JobType.Sms, JobPriority.Normal));
        await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(
            new ScheduleJobRequest("Job 3", "{}", JobType.Push, JobPriority.Low));

        var response = await mediator.Send<GetPendingJobsRequest, PagedJobResponse>(
            new GetPendingJobsRequest());

        response.Total.Should().BeGreaterThanOrEqualTo(3);
        response.Items.Should().HaveCountGreaterThanOrEqualTo(3);
        response.Items.Should().OnlyContain(j => j.Status == "Pending");
    }

    [Fact]
    public async Task GetJobById_ShouldReturnScheduledJob()
    {
        ClearDatabase();
        var mediator = await GetMediator();

        var createResponse = await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(
            new ScheduleJobRequest("Test Job", "{}", JobType.Email));

        var job = await mediator.Send<GetJobByIdRequest, JobDto?>(
            new GetJobByIdRequest(createResponse.JobId));

        job.Should().NotBeNull();
        job!.Name.Should().Be("Test Job");
        job.Type.Should().Be("Email");
    }

    [Fact]
    public async Task CancelJob_ShouldCancelPendingJob()
    {
        ClearDatabase();
        var mediator = await GetMediator();

        var createResponse = await mediator.Send<ScheduleJobRequest, ScheduleJobResponse>(
            new ScheduleJobRequest("Test Job", "{}", JobType.Email));

        var cancelResponse = await mediator.Send<CancelJobRequest, CancelJobResponse>(
            new CancelJobRequest(createResponse.JobId));

        cancelResponse.JobId.Should().Be(createResponse.JobId);

        var job = await mediator.Send<GetJobByIdRequest, JobDto?>(
            new GetJobByIdRequest(createResponse.JobId));

        job!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task SendNotification_ShouldCreateNotification()
    {
        ClearDatabase();
        var mediator = await GetMediator();

        var request = new SendNotificationRequest(
            "admin@example.com",
            "Test Alert",
            "This is a test notification",
            "Email");

        var response = await mediator.Send<SendNotificationRequest, SendNotificationResponse>(request);
        response.NotificationId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetNotifications_ShouldReturnSentNotifications()
    {
        ClearDatabase();
        var mediator = await GetMediator();

        await mediator.Send<SendNotificationRequest, SendNotificationResponse>(
            new SendNotificationRequest("admin@example.com", "Alert 1", "Body 1", "Email"));
        await mediator.Send<SendNotificationRequest, SendNotificationResponse>(
            new SendNotificationRequest("+1234567890", "SMS 1", "Body 2", "Sms"));

        var response = await mediator.Send<GetNotificationsRequest, PagedNotificationResponse>(
            new GetNotificationsRequest());

        response.Total.Should().BeGreaterThanOrEqualTo(2);
        response.Items.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    private async Task<Mediator> GetMediator()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<Mediator>();
    }
}
