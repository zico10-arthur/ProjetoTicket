using System.Collections.Concurrent;

namespace Api.Middlewares;

/// <summary>
/// Spec 120: Thread-safe sliding window counter for rate limiting.
/// Tracks timestamps of events and enforces a maximum count within a time interval.
/// </summary>
public class SlidingWindow
{
    private readonly ConcurrentQueue<DateTime> _timestamps = new();

    /// <summary>
    /// Attempts to record a new event.
    /// </summary>
    /// <param name="limit">Maximum number of events allowed within the interval.</param>
    /// <param name="interval">The time window duration.</param>
    /// <returns>true if the event is within the limit and was recorded; false if the limit has been exceeded.</returns>
    public bool TryIncrement(int limit, TimeSpan interval)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - interval;

        // Remove expired timestamps (lock-free cleanup)
        while (_timestamps.TryPeek(out var oldest) && oldest < cutoff)
            _timestamps.TryDequeue(out _);

        if (_timestamps.Count >= limit)
            return false;

        _timestamps.Enqueue(now);
        return true;
    }
}
