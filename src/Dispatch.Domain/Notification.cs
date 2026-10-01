namespace Dispatch.Domain;

public sealed class Notification : AggregateRoot
{
    public string Recipient { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Channel { get; private set; } = string.Empty;
    public bool IsSent { get; private set; }
    public bool IsFailed { get; private set; }
    public string? Error { get; private set; }
    public DateTime? SentAt { get; private set; }

    private Notification() { }

    public static Notification Create(Guid id, string recipient, string subject, string body, string channel)
    {
        return new Notification
        {
            Id = id,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Channel = channel,
            IsSent = false,
            IsFailed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsSent()
    {
        IsSent = true;
        SentAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string? errorMessage)
    {
        IsFailed = true;
        Error = errorMessage;
        SentAt = DateTime.UtcNow;
    }
}
