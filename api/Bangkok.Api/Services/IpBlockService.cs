using System.Collections.Concurrent;

namespace Bangkok.Api.Services;

/// <summary>
/// In-memory IP block tracking: 10 failed attempts within 5 minutes blocks the IP for 30 minutes.
/// Single-instance only; not distributed.
/// </summary>
public sealed class IpBlockService : IIpBlockService
{
    private const int FailedAttemptThreshold = 10;
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<string, IpBlockEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<IpBlockService> _logger;

    public IpBlockService(ILogger<IpBlockService> logger)
    {
        _logger = logger;
    }

    public bool IsBlocked(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;

        if (!_entries.TryGetValue(ip, out var entry))
            return false;

        if (entry.BlockedUntil is not { } until)
            return false;

        if (DateTime.UtcNow < until)
            return true;

        // Block expired; remove so we don't keep stale entries
        _entries.TryRemove(ip, out _);
        return false;
    }

    public bool RecordFailedAttempt(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;

        var now = DateTime.UtcNow;

        var entry = _entries.AddOrUpdate(
            ip,
            _ => new IpBlockEntry { FailedCount = 1, FirstAttemptAt = now },
            (_, existing) =>
            {
                // If outside the 5-minute window, reset the window
                if (now - existing.FirstAttemptAt > FailureWindow)
                    return new IpBlockEntry { FailedCount = 1, FirstAttemptAt = now };

                var nextCount = existing.FailedCount + 1;
                return new IpBlockEntry
                {
                    FailedCount = nextCount,
                    FirstAttemptAt = existing.FirstAttemptAt,
                    BlockedUntil = nextCount >= FailedAttemptThreshold ? now + BlockDuration : null
                };
            });

        if (entry.BlockedUntil.HasValue)
        {
            _logger.LogWarning(
                "IP block triggered. IP: {ClientIp}, FailedAttempts: {FailedCount}, BlockedUntil: {BlockedUntil:O}",
                ip, entry.FailedCount, entry.BlockedUntil.Value);
            return true;
        }

        return false;
    }

    public void ResetAttempts(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return;

        if (_entries.TryRemove(ip, out _))
            _logger.LogInformation("Successful login; IP failure count reset. IP: {ClientIp}, Timestamp: {Timestamp:O}", ip, DateTime.UtcNow);
    }

    private sealed class IpBlockEntry
    {
        public int FailedCount { get; set; }
        public DateTime FirstAttemptAt { get; set; }
        public DateTime? BlockedUntil { get; set; }
    }
}
