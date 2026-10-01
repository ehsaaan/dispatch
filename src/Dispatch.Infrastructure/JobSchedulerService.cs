using Dispatch.Application;
using Dispatch.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;

namespace Dispatch.Infrastructure;

public sealed class JobSchedulerService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobSchedulerService> _logger;
    private Task? _executingTask;
    private const int _checkIntervalSeconds = 10;

    public JobSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<JobSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JobSchedulerService started.");
        _executingTask = ExecuteAsync(cancellationToken);
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_checkIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending jobs.");
            }
        }
    }

    private async Task ProcessPendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
        var smsService = scope.ServiceProvider.GetRequiredService<ISmsNotificationService>();
        var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pendingJobs = await jobRepository.GetPendingAsync();
        var now = DateTime.UtcNow;

        foreach (var job in pendingJobs)
        {
            if (job.ScheduledAt.HasValue && job.ScheduledAt.Value > now)
                continue;

            await ExecuteJobAsync(job, emailService, smsService, pushService, unitOfWork, jobRepository, cancellationToken);
        }
    }

    private async Task ExecuteJobAsync(Job job, IEmailNotificationService emailService, ISmsNotificationService smsService, IPushNotificationService pushService, IUnitOfWork unitOfWork, IJobRepository jobRepository, CancellationToken cancellationToken)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                job.MaxRetries,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Job {JobId} ({Name}) failed, retry {RetryCount}/{MaxRetries} after {Delay}ms.",
                        job.Id,
                        job.Name,
                        retryCount,
                        job.MaxRetries,
                        timeSpan.TotalMilliseconds);
                });

        await retryPolicy.ExecuteAsync(async () =>
        {
            job.Start();

            switch (job.Type)
            {
                case JobType.Email:
                    await emailService.SendAsync(job.Payload);
                    break;
                case JobType.Sms:
                    await smsService.SendAsync(job.Payload);
                    break;
                case JobType.Push:
                    await pushService.SendAsync(job.Payload);
                    break;
                case JobType.DataSync:
                    _logger.LogDebug("Executing data sync job: {JobId}", job.Id);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    break;
                case JobType.ReportGeneration:
                    _logger.LogDebug("Generating report: {JobId}", job.Id);
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    break;
                case JobType.Cleanup:
                    _logger.LogDebug("Running cleanup: {JobId}", job.Id);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    break;
                case JobType.Custom:
                    _logger.LogDebug("Executing custom job: {JobId}", job.Id);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    break;
            }

            job.Complete();
            await jobRepository.UpdateAsync(job);
            await unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Job {JobId} ({Name}) completed in {Duration:F2}s",
                job.Id,
                job.Name,
                job.Duration!.Value.TotalSeconds);
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JobSchedulerService stopped.");
        if (_executingTask is null || _executingTask.IsCompleted)
            return Task.CompletedTask;
        return _executingTask;
    }
}
