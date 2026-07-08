using System.Threading.RateLimiting;

namespace SPTarkov.Core.Forge;

public class ForgeRateLimiter : IAsyncDisposable
{
    private readonly RateLimiter _burst; // 40r/10s
    private readonly RateLimiter _sustained; // 200r/60s

    public ForgeRateLimiter()
    {
        _burst = new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 35,
                Window = TimeSpan.FromSeconds(10),
                SegmentsPerWindow = 1,
                QueueLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }
        );

        _sustained = new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 160,
                Window = TimeSpan.FromSeconds(60),
                SegmentsPerWindow = 1,
                QueueLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }
        );
    }

    public async Task WaitAsync(CancellationToken ct = default)
    {
        // Must acquire both — whichever is tighter will naturally throttle
        using var sustainedLease = await _sustained.AcquireAsync(1, ct);
        if (!sustainedLease.IsAcquired)
        {
            Console.WriteLine(_sustained.GetStatistics());
        }

        using var burstLease = await _burst.AcquireAsync(1, ct);
        if (!burstLease.IsAcquired)
        {
            Console.WriteLine(_sustained.GetStatistics());
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _burst.DisposeAsync();
        await _sustained.DisposeAsync();
    }
}
