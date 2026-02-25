namespace Bangkok.Application.Dto.Users;

/// <summary>
/// Optional body for lock user endpoint. When LockoutEnd is omitted, default lock duration (15 minutes) is used.
/// </summary>
public class LockUserRequest
{
    /// <summary>
    /// When the lockout should end (UTC). Must be in the future. If omitted, lockout ends in 15 minutes.
    /// </summary>
    public DateTime? LockoutEnd { get; set; }
}
