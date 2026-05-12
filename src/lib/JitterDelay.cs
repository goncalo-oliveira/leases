namespace Faactory.Leases;

internal static class JitterDelay
{
    public static Task DelayAsync( TimeSpan delay, CancellationToken cancellationToken )
    {
        var jitter = TimeSpan.FromMilliseconds( Random.Shared.Next( 0, 500 ) );

        return Task.Delay( delay + jitter, cancellationToken );
    }
}
