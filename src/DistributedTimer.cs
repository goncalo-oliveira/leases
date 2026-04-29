namespace Faactory.Leases;

/// <summary>
/// Represents a distributed timer that can be used to coordinate actions across multiple instances in a distributed system.
/// </summary>
/// <param name="timerStore">The distributed timer store.</param>
/// <param name="name">The name of the timer.</param>
/// <param name="period">The period of the timer.</param>
public sealed class DistributedTimer( IDistributedTimerStore timerStore, string name, TimeSpan period )
{
    /// <summary>
    /// Waits for the next tick of the timer.
    /// This method will block until the timer ticks or the cancellation token is cancelled.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A value task that completes with true if the timer ticked, or false if the operation was cancelled.</returns>
    public async ValueTask<bool> WaitForNextTickAsync( CancellationToken cancellationToken )
    {
        while ( !cancellationToken.IsCancellationRequested )
        {
            try
            {
                var remaining = await timerStore.GetOrCreateTimerAsync( name, period, cancellationToken )
                    .ConfigureAwait( false );

                await Task.Delay( remaining, cancellationToken )
                    .ConfigureAwait( false );

                return true;
            }
            catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested )
            {
                return false;
            }
            catch
            {
                // Ignore and retry
                await JitterDelay.DelayAsync( TimeSpan.FromMilliseconds( 500 ), cancellationToken );
            }
        }

        return false;
    }
}
