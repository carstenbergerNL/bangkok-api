namespace Bangkok.Application.Interfaces;

/// <summary>
/// Simple audit logging (Serilog). No database audit tables.
/// Do not log passwords, hashes, salts, or recovery strings.
/// </summary>
public interface IAuditLogger
{
    void LogUserCreated(Guid userId, string? email, string? ip);
    void LogUserUpdated(Guid userId, string? email, string? ip);
    void LogUserDeleted(Guid userId, string? email, string? ip);
    void LogRoleChanged(Guid userId, string? email, string newRole, string? ip);
    void LogLoginSuccess(Guid? userId, string? email, string? ip);
    void LogLoginFailure(string? email, string? ip);
    void LogAccountLockoutTriggered(string? email, string? ip);
    void LogIpBlockTriggered(string? ip);
}
