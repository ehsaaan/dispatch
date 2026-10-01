namespace Dispatch.Domain;

public enum JobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum JobPriority
{
    Low,
    Normal,
    High,
    Critical
}

public enum JobType
{
    Email,
    Sms,
    Push,
    DataSync,
    ReportGeneration,
    Cleanup,
    Custom
}

public sealed class Job : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public JobType Type { get; private set; }
    public JobPriority Priority { get; private set; }
    public JobStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public TimeSpan? Duration { get; private set; }

    private Job() { }

    public static Job Create(Guid id, string name, string payload, JobType type, JobPriority priority = JobPriority.Normal, int maxRetries = 3, DateTime? scheduledAt = null)
    {
        return new Job
        {
            Id = id,
            Name = name,
            Payload = payload,
            Type = type,
            Priority = priority,
            Status = JobStatus.Pending,
            MaxRetries = maxRetries,
            ScheduledAt = scheduledAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Start()
    {
        if (Status != JobStatus.Pending)
            throw new InvalidOperationException("Job must be pending to start.");

        Status = JobStatus.Running;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != JobStatus.Running)
            throw new InvalidOperationException("Job must be running to complete.");

        Status = JobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Duration = CompletedAt.Value - StartedAt.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail(string? errorMessage)
    {
        if (Status != JobStatus.Running)
            throw new InvalidOperationException("Job must be running to fail.");

        Status = JobStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanRetry()
    {
        return RetryCount < MaxRetries && Status == JobStatus.Failed;
    }

    public void IncrementRetry()
    {
        RetryCount++;
        Status = JobStatus.Pending;
        StartedAt = null;
        CompletedAt = null;
        Duration = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == JobStatus.Completed || Status == JobStatus.Cancelled)
            throw new InvalidOperationException("Job cannot be cancelled.");

        Status = JobStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
