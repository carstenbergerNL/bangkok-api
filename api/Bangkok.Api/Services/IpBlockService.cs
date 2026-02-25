using System.Collections.Concurrent;

namespace Bangkok.Api.Services;

/// <summary>
/// In-memory brute force protection: IP (with exponential escalation), Email, and IP+Email tracking.
/// Single-instance only; thread-safe.
/// </summary>
public sealed class IpBlockService : IIpBlockService
{
    private const int IpThreshold = 10;
    private const int EmailThreshold = 5;
    private const int IpEmailThreshold = 5;
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan EscalationResetWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan BlockLevel1 = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan BlockLevel2 = TimeSpan.FromHours(2);
    private static readonly TimeSpan BlockLevel3 = TimeSpan.FromHours(24);
    private static readonly TimeSpan EmailOrComboBlockDuration = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<string, IpEntry> _ipEntries = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SimpleEntry> _emailEntries = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SimpleEntry> _ipEmailEntries = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<IpBlockService> _logger;

    public IpBlockService(ILogger<IpBlockService> logger)
    {
        _logger = logger;
    }

    public BlockCheckResult CheckBlocked(string ip, string? email)
    {
        var now = DateTime.UtcNow;
        int? retryAfter = null;

        if (!string.IsNullOrWhiteSpace(ip) && _ipEntries.TryGetValue(ip, out var ipEntry) && ipEntry.BlockedUntil is { } ipUntil && now < ipUntil)
        {
            var seconds = (int)Math.Ceiling((ipUntil - now).TotalSeconds);
            retryAfter = retryAfter.HasValue ? Math.Max(retryAfter.Value, seconds) : seconds;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailKey = NormalizeEmail(email);
            if (_emailEntries.TryGetValue(emailKey, out var emailEntry) && emailEntry.BlockedUntil is { } emailUntil)
            {
                if (now < emailUntil)
                {
                    var seconds = (int)Math.Ceiling((emailUntil - now).TotalSeconds);
                    retryAfter = retryAfter.HasValue ? Math.Max(retryAfter.Value, seconds) : seconds;
                }
                else
                    _emailEntries.TryRemove(emailKey, out _);
            }

            var comboKey = $"{ip ?? "unknown"}|{emailKey}";
            if (_ipEmailEntries.TryGetValue(comboKey, out var comboEntry) && comboEntry.BlockedUntil is { } comboUntil)
            {
                if (now < comboUntil)
                {
                    var seconds = (int)Math.Ceiling((comboUntil - now).TotalSeconds);
                    retryAfter = retryAfter.HasValue ? Math.Max(retryAfter.Value, seconds) : seconds;
                }
                else
                    _ipEmailEntries.TryRemove(comboKey, out _);
            }
        }

        if (retryAfter.HasValue)
            return BlockCheckResult.Blocked(retryAfter.Value);
        return BlockCheckResult.NotBlocked;
    }

    public void RecordFailedAttempt(string ip, string? email)
    {
        var now = DateTime.UtcNow;
        var ipKey = string.IsNullOrWhiteSpace(ip) ? null : ip;
        var emailKey = string.IsNullOrWhiteSpace(email) ? null : NormalizeEmail(email!);

        if (ipKey != null)
        {
            var (blocked, duration) = UpdateIpEntry(ipKey, now);
            if (blocked && duration.HasValue)
            {
                var level = GetIpEscalationLevel(ipKey);
                _logger.LogWarning(
                    "IP block triggered. IP: {ClientIp}, EscalationLevel: {EscalationLevel}, BlockDurationMinutes: {BlockDurationMinutes}, Timestamp: {Timestamp:O}",
                    ipKey, level, (int)duration.Value.TotalMinutes, now);
            }
        }

        if (emailKey != null)
        {
            var blocked = UpdateSimpleEntry(_emailEntries, emailKey, now, EmailThreshold, (_, _) => EmailOrComboBlockDuration);
            if (blocked)
                _logger.LogWarning("Email block triggered. Email: {Email}, Timestamp: {Timestamp:O}", emailKey, now);
        }

        if (ipKey != null && emailKey != null)
        {
            var comboKey = $"{ipKey}|{emailKey}";
            var blocked = UpdateSimpleEntry(_ipEmailEntries, comboKey, now, IpEmailThreshold, (_, _) => EmailOrComboBlockDuration);
            if (blocked)
                _logger.LogWarning("IP+Email block triggered. IP: {ClientIp}, Email: {Email}, Timestamp: {Timestamp:O}", ipKey, emailKey, now);
        }
    }

    public void ResetAttempts(string ip, string? email)
    {
        var now = DateTime.UtcNow;
        var ipKey = string.IsNullOrWhiteSpace(ip) ? null : ip;
        var emailKey = string.IsNullOrWhiteSpace(email) ? null : NormalizeEmail(email!);

        if (ipKey != null && _ipEntries.TryRemove(ipKey, out _))
            _logger.LogInformation("Successful login; IP failure count reset. IP: {ClientIp}, Timestamp: {Timestamp:O}", ipKey, now);
        if (emailKey != null && _emailEntries.TryRemove(emailKey, out _))
            _logger.LogInformation("Successful login; email failure count reset. Email: {Email}, Timestamp: {Timestamp:O}", emailKey, now);
        if (ipKey != null && emailKey != null)
        {
            var comboKey = $"{ipKey}|{emailKey}";
            if (_ipEmailEntries.TryRemove(comboKey, out _))
                _logger.LogInformation("Successful login; IP+email failure count reset. IP: {ClientIp}, Email: {Email}, Timestamp: {Timestamp:O}", ipKey, emailKey, now);
        }
    }

    private (bool Blocked, TimeSpan? Duration) UpdateIpEntry(string ip, DateTime now)
    {
        var entry = _ipEntries.AddOrUpdate(
            ip,
            _ => new IpEntry { FailedCount = 1, FirstAttemptAt = now },
            (_, existing) =>
            {
                if (now - existing.FirstAttemptAt > FailureWindow)
                    return new IpEntry { FailedCount = 1, FirstAttemptAt = now, EscalationLevel = existing.EscalationLevel, LastBlockTimestamp = existing.LastBlockTimestamp };

                var nextCount = existing.FailedCount + 1;
                if (nextCount < IpThreshold)
                    return new IpEntry { FailedCount = nextCount, FirstAttemptAt = existing.FirstAttemptAt, BlockedUntil = existing.BlockedUntil, EscalationLevel = existing.EscalationLevel, LastBlockTimestamp = existing.LastBlockTimestamp };

                var level = existing.EscalationLevel;
                if (existing.LastBlockTimestamp.HasValue && (now - existing.LastBlockTimestamp.Value) > EscalationResetWindow)
                    level = 1;
                else
                    level = Math.Min(3, level + 1);

                var duration = level switch
                {
                    1 => BlockLevel1,
                    2 => BlockLevel2,
                    _ => BlockLevel3
                };
                var blockedUntil = now.Add(duration);

                return new IpEntry
                {
                    FailedCount = nextCount,
                    FirstAttemptAt = existing.FirstAttemptAt,
                    BlockedUntil = blockedUntil,
                    EscalationLevel = level,
                    LastBlockTimestamp = now
                };
            });

        if (!entry.BlockedUntil.HasValue || entry.BlockedUntil.Value <= now)
            return (false, null);
        var d = entry.BlockedUntil.Value - now;
        return (true, d);
    }

    private int GetIpEscalationLevel(string ip)
    {
        return _ipEntries.TryGetValue(ip, out var e) ? e.EscalationLevel : 0;
    }

    private static bool UpdateSimpleEntry(
        ConcurrentDictionary<string, SimpleEntry> dict,
        string key,
        DateTime now,
        int threshold,
        Func<string, SimpleEntry, TimeSpan> getDuration)
    {
        var entry = dict.AddOrUpdate(
            key,
            _ => new SimpleEntry { FailedCount = 1, FirstAttemptAt = now },
            (_, existing) =>
            {
                if (now - existing.FirstAttemptAt > FailureWindow)
                    return new SimpleEntry { FailedCount = 1, FirstAttemptAt = now };

                var nextCount = existing.FailedCount + 1;
                DateTime? blockedUntil = nextCount >= threshold ? now.Add(getDuration(key, existing)) : (DateTime?)null;
                return new SimpleEntry
                {
                    FailedCount = nextCount,
                    FirstAttemptAt = existing.FirstAttemptAt,
                    BlockedUntil = blockedUntil
                };
            });

        return entry.BlockedUntil.HasValue && entry.BlockedUntil.Value > now;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private sealed class IpEntry
    {
        public int FailedCount { get; set; }
        public DateTime FirstAttemptAt { get; set; }
        public DateTime? BlockedUntil { get; set; }
        public int EscalationLevel { get; set; }
        public DateTime? LastBlockTimestamp { get; set; }
    }

    private sealed class SimpleEntry
    {
        public int FailedCount { get; set; }
        public DateTime FirstAttemptAt { get; set; }
        public DateTime? BlockedUntil { get; set; }
    }
}
