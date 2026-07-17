using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.Forge;

public class ForgeRateLimiter : IAsyncDisposable
{
    private static readonly TimeSpan BurstWindow = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SustainedWindow = TimeSpan.FromSeconds(60);

    private readonly ILogger<ForgeRateLimiter> _logger;
    private readonly RateLimiter _burst; // 40r/10s
    private readonly RateLimiter _sustained; // 200r/60s

    private readonly Lock _stateLock = new();
    private DateTimeOffset? _serverLimitedUntil;
    private DateTimeOffset? _clientLimitedUntil;
    private int _clientWaiters;

    /// <summary>Raised when the rate limit state changes: a wait begins, is extended, or clears.</summary>
    public event Action? RateLimitChanged;

    public ForgeRateLimiter(ILogger<ForgeRateLimiter> logger)
    {
        _logger = logger;

        _burst = new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 35,
                Window = BurstWindow,
                SegmentsPerWindow = 1,
                QueueLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }
        );

        _sustained = new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 160,
                Window = SustainedWindow,
                SegmentsPerWindow = 1,
                QueueLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }
        );
    }

    /// <summary>Estimated time left on the current rate limit wait, or <c>null</c> when requests can flow.</summary>
    public TimeSpan? TimeRemaining
    {
        get
        {
            DateTimeOffset? until;
            lock (_stateLock)
            {
                until = Later(_serverLimitedUntil, _clientLimitedUntil);
            }

            var remaining = until - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : null;
        }
    }

    public async Task WaitAsync(CancellationToken ct = default)
    {
        // Acquire both and whichever is tighter will throttle.
        await AcquireAsync(_sustained, "sustained", SustainedWindow, ct);
        await AcquireAsync(_burst, "burst", BurstWindow, ct);
    }

    /// <summary>Records a server 429 backoff; <see cref="TimeRemaining"/> then covers the <c>Retry-After</c> delay.</summary>
    public void ReportServerRateLimit(TimeSpan retryAfter)
    {
        var until = DateTimeOffset.UtcNow + retryAfter;
        lock (_stateLock)
        {
            _serverLimitedUntil = Later(_serverLimitedUntil, until);
        }

        RateLimitChanged?.Invoke();
    }

    /// <summary>Clears the server 429 backoff once a request gets through again.</summary>
    public void ClearServerRateLimit()
    {
        lock (_stateLock)
        {
            if (_serverLimitedUntil is null)
            {
                return;
            }

            _serverLimitedUntil = null;
        }

        RateLimitChanged?.Invoke();
    }

    private async Task AcquireAsync(RateLimiter limiter, string name, TimeSpan window, CancellationToken ct)
    {
        using (var immediate = limiter.AttemptAcquire())
        {
            if (immediate.IsAcquired)
            {
                return;
            }
        }

        BeginClientWait(window);
        try
        {
            using var lease = await limiter.AcquireAsync(1, ct);
            if (!lease.IsAcquired)
            {
                var stats = limiter.GetStatistics();
                _logger.LogWarning(
                    "Failed to acquire Forge {Name} rate limit lease (available: {Available}, queued: {Queued})",
                    name,
                    stats?.CurrentAvailablePermits,
                    stats?.CurrentQueuedCount
                );
                throw new InvalidOperationException($"Failed to acquire Forge {name} rate limit lease");
            }
        }
        finally
        {
            EndClientWait();
        }
    }

    private void BeginClientWait(TimeSpan window)
    {
        var until = DateTimeOffset.UtcNow + window;
        lock (_stateLock)
        {
            _clientWaiters++;
            _clientLimitedUntil = Later(_clientLimitedUntil, until);
        }

        RateLimitChanged?.Invoke();
    }

    private void EndClientWait()
    {
        lock (_stateLock)
        {
            _clientWaiters--;
            if (_clientWaiters > 0 || _clientLimitedUntil is null)
            {
                return;
            }

            _clientLimitedUntil = null;
        }

        RateLimitChanged?.Invoke();
    }

    private static DateTimeOffset? Later(DateTimeOffset? a, DateTimeOffset? b)
    {
        return a > b ? a : b ?? a;
    }

    public async ValueTask DisposeAsync()
    {
        await _burst.DisposeAsync();
        await _sustained.DisposeAsync();
    }
}
