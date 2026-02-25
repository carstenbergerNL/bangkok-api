namespace Bangkok.Domain;

/// <summary>
/// Refresh token entity stored in SQL Server. Primary key is UNIQUEIDENTIFIER (Guid).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? RevokedReason { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
