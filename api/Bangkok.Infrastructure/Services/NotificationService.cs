using Bangkok.Application.Dto.Notifications;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;

namespace Bangkok.Infrastructure.Services;

public class NotificationService : INotificationService
{
    public const string TypeTaskAssigned = "TaskAssigned";
    public const string TypeTaskDueDateChanged = "TaskDueDateChanged";
    public const string TypeTaskStatusChanged = "TaskStatusChanged";
    public const string TypeMention = "Mention";
    public const string TypeMemberAddedToProject = "MemberAddedToProject";

    private readonly INotificationRepository _repository;

    public NotificationService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetByUserIdAsync(userId, limit, cancellationToken).ConfigureAwait(false);
        return list.Select(Map).ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetUnreadCountByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<(bool Success, string? Error)> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var n = await _repository.GetByIdAsync(notificationId, cancellationToken).ConfigureAwait(false);
        if (n == null)
            return (false, "Notification not found.");
        if (n.UserId != userId)
            return (false, "You can only mark your own notifications as read.");
        await _repository.MarkReadAsync(notificationId, userId, cancellationToken).ConfigureAwait(false);
        return (true, null);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _repository.MarkAllReadAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task CreateAsync(Guid userId, string type, string title, string message, Guid? referenceId = null, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            ReferenceId = referenceId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.CreateAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    private static NotificationResponse Map(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        ReferenceId = n.ReferenceId,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}
