using Bangkok.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

/// <summary>
/// Serilog-based audit logging. Structured properties: AuditAction, UserId, Email, Ip, Timestamp.
/// No passwords or secrets.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogUserCreated(Guid userId, string? email, string? ip)
    {
        _logger.LogInformation("Audit: UserCreated. UserId: {UserId}, Email: {Email}, Ip: {Ip}, Timestamp: {Timestamp:O}",
            userId, email ?? "(none)", ip ?? "(none)", DateTime.UtcNow);
    }

    public void LogUserUpdated(Guid userId, string? email, string? ip)
    {
        _logger.LogInformation("Audit: UserUpdated. UserId: {UserId}, Email: {Email}, Ip: {Ip}, Timestamp: {Timestamp:O}",
            userId, email ?? "(none)", ip ?? "(none)", DateTime.UtcNow);
    }

    public void LogUserDeleted(Guid userId, string? email, string? ip)
    {
        _logger.LogInformation("Audit: UserDeleted. UserId: {UserId}, Email: {Email}, Ip: {Ip}, Timestamp: {Timestamp:O}",
            userId, email ?? "(none)", ip ?? "(none)", DateTime.UtcNow);
    }

    public void LogRoleChanged(Guid userId, string? email, string newRole, string? ip)
    {
        _logger.LogInformation("Audit: RoleChanged. UserId: {UserId}, Email: {Email}, NewRole: {NewRole}, Ip: {Ip}, Timestamp: {Timestamp:O}",
            userId, email ?? "(none)", newRole, ip ?? "(none)", DateTime.UtcNow);
    }

    public void LogLoginSuccess(Guid? userId, string? email, string? ip)
    {
        _logger.LogInformation("Audit: LoginSuccess. UserId: {UserId}, Email: {Email}, Ip: {Ip}, Timestamp: {Timestamp:O}",
            userId, email ?? "(none)", ip ?? "(none)", DateTime.UtcNow);
    }

    public void LogLoginFailure(string? email, string? ip)
    {
        _logger.LogWarning("Audit: LoginFailure. Email: {Email}, Ip: {Ip}, Timestamp: {Timestamp:O}",
            email ?? "(none)", ip ?? "(none)", DateTime.UtcNow);
    }

    public void LogAccountLockoutTriggered(string? email, string? ip)
    {
        _logger.LogWarning("Audit: AccountLockoutTriggered. Email: {Email}, Ip: {Ip}, Timestamp: {Timestamp:O}",
            email ?? "(none)", ip ?? "(none)", DateTime.UtcNow);
    }

    public void LogIpBlockTriggered(string? ip)
    {
        _logger.LogWarning("Audit: IpBlockTriggered. Ip: {Ip}, Timestamp: {Timestamp:O}",
            ip ?? "(none)", DateTime.UtcNow);
    }
}
