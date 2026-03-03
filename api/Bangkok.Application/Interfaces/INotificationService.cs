using Bangkok.Application.Dto.Notifications;

namespace Bangkok.Application.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Get notifications for the current user (newest first).
    /// </summary>
    Task<IReadOnlyList<NotificationResponse>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unread count for the current user.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a single notification as read. Only the owner can mark.
    /// </summary>
    Task<(bool Success, string? Error)> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark all notifications for the user as read.
    /// </summary>
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a notification (used by other services: task assign, mention, due date change, etc.).
    /// </summary>
    Task CreateAsync(Guid userId, string type, string title, string message, Guid? referenceId = null, CancellationToken cancellationToken = default);
}
