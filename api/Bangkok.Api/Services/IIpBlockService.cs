namespace Bangkok.Api.Services;

/// <summary>
/// Result of a block check. Use RetryAfterSeconds for the Retry-After response header when IsBlocked is true.
/// </summary>
public readonly struct BlockCheckResult
{
    public bool IsBlocked { get; }
    public int? RetryAfterSeconds { get; }

    public static BlockCheckResult NotBlocked => new(false, null);
    public static BlockCheckResult Blocked(int retryAfterSeconds) => new(true, retryAfterSeconds);

    private BlockCheckResult(bool isBlocked, int? retryAfterSeconds)
    {
        IsBlocked = isBlocked;
        RetryAfterSeconds = retryAfterSeconds;
    }
}

/// <summary>
/// In-memory brute force protection: IP, Email, and IP+Email tracking with exponential IP lock escalation. Single-instance only.
/// </summary>
public interface IIpBlockService
{
    /// <summary>
    /// Checks if the client is blocked by IP, email, or IP+Email. Call before attempting login.
    /// </summary>
    /// <param name="ip">Client IP address.</param>
    /// <param name="email">Login email (optional; if null only IP is checked).</param>
    /// <returns>BlockCheckResult with IsBlocked and RetryAfterSeconds for the Retry-After header.</returns>
    BlockCheckResult CheckBlocked(string ip, string? email);

    /// <summary>
    /// Records a failed login attempt. Increments IP, email, and IP+Email counters and applies thresholds/escalation.
    /// </summary>
    void RecordFailedAttempt(string ip, string? email);

    /// <summary>
    /// Clears failure counters for this IP and email (and IP+Email). Call when login succeeds.
    /// </summary>
    void ResetAttempts(string ip, string? email);
}
