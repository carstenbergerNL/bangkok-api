namespace Bangkok.Api.Services;

/// <summary>
/// In-memory IP-based brute force protection. Single-instance only.
/// </summary>
public interface IIpBlockService
{
    /// <summary>
    /// Returns true if the IP is currently blocked (too many failed login attempts).
    /// </summary>
    bool IsBlocked(string ip);

    /// <summary>
    /// Records a failed login attempt. Call when login fails.
    /// Returns true if the IP was just blocked (threshold reached).
    /// </summary>
    bool RecordFailedAttempt(string ip);

    /// <summary>
    /// Clears failure count for the IP. Call when login succeeds.
    /// </summary>
    void ResetAttempts(string ip);
}
