using Microsoft.Extensions.Logging;

namespace Dispatch.Infrastructure;

public interface IEmailNotificationService
{
    Task SendAsync(string payload);
}

public interface ISmsNotificationService
{
    Task SendAsync(string payload);
}

public interface IPushNotificationService
{
    Task SendAsync(string payload);
}

public sealed class InMemoryEmailService : IEmailNotificationService
{
    private readonly ILogger<InMemoryEmailService> _logger;

    public InMemoryEmailService(ILogger<InMemoryEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string payload)
    {
        _logger.LogInformation("Email notification sent: {Payload}", payload);
        return Task.CompletedTask;
    }
}

public sealed class InMemorySmsService : ISmsNotificationService
{
    private readonly ILogger<InMemorySmsService> _logger;

    public InMemorySmsService(ILogger<InMemorySmsService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string payload)
    {
        _logger.LogInformation("SMS notification sent: {Payload}", payload);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryPushService : IPushNotificationService
{
    private readonly ILogger<InMemoryPushService> _logger;

    public InMemoryPushService(ILogger<InMemoryPushService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string payload)
    {
        _logger.LogInformation("Push notification sent: {Payload}", payload);
        return Task.CompletedTask;
    }
}
