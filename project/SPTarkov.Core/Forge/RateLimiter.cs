using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.Forge;

public class ForgeRateLimiter : IAsyncDisposable
{
    private readonly ILogger<ForgeRateLimiter> _logger;
    private readonly RateLimiter _burst; // 40r/10s
    private readonly RateLimiter _sustained; // 200r/60s

    public ForgeRateLimiter(ILogger<ForgeRateLimiter> logger)
    {
        _logger = logger;

        _burst = new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 35,
                Window = TimeSpan.FromSeconds(10),
                SegmentsPerWindow = 1,
                QueueLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }
        );

        _sustained = new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 160,
                Window = TimeSpan.FromSeconds(60),
                SegmentsPerWindow = 1,
                QueueLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }
        );
    }

    public async Task WaitAsync(CancellationToken ct = default)
    {
        // Acquire both and whichever is tighter will throttle.
        await AcquireAsync(_sustained, "sustained", ct);
        await AcquireAsync(_burst, "burst", ct);
    }

    private async Task AcquireAsync(RateLimiter limiter, string name, CancellationToken ct)
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

    public async ValueTask DisposeAsync()
    {
        await _burst.DisposeAsync();
        await _sustained.DisposeAsync();
    }
}
