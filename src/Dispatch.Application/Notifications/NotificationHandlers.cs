using Dispatch.Application.Common;
using Dispatch.Domain;

namespace Dispatch.Application.Notifications;

public sealed record SendNotificationRequest(
    string To,
    string Subject,
    string Body,
    string Channel) : IRequest<SendNotificationResponse>;

public sealed record SendNotificationResponse(Guid NotificationId);

public sealed record GetNotificationsRequest(
    string? Channel = null,
    bool? IsSent = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedNotificationResponse>;

public sealed record PagedNotificationResponse(IEnumerable<NotificationSummary> Items, int Page, int PageSize, int Total);

public sealed record NotificationSummary(
    Guid Id,
    string To,
    string Subject,
    string Body,
    string Channel,
    bool IsSent,
    string? Error,
    DateTime? SentAt);

public static class NotificationMappings
{
    public static NotificationSummary ToSummary(this Notification notification)
    {
        return new NotificationSummary(
            notification.Id,
            notification.Recipient,
            notification.Subject,
            notification.Body,
            notification.Channel,
            notification.IsSent,
            notification.Error,
            notification.SentAt);
    }
}

public sealed class SendNotificationHandler : IRequestHandler<SendNotificationRequest, SendNotificationResponse>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SendNotificationHandler(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SendNotificationResponse> Handle(SendNotificationRequest request)
    {
        var notification = Notification.Create(Guid.NewGuid(), request.To, request.Subject, request.Body, request.Channel);
        await _notificationRepository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
        return new SendNotificationResponse(notification.Id);
    }
}

public sealed class GetNotificationsHandler : IRequestHandler<GetNotificationsRequest, PagedNotificationResponse>
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<PagedNotificationResponse> Handle(GetNotificationsRequest request)
    {
        var notifications = await _notificationRepository.GetPagedAsync(request.Channel, request.IsSent, request.Page, request.PageSize);
        return new PagedNotificationResponse(notifications.Select(n => n.ToSummary()), request.Page, request.PageSize, notifications.Count());
    }
}
