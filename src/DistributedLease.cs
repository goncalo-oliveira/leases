namespace Faactory.Leases;

/// <summary>
/// Represents a distributed lease that can be acquired, renewed, and released by a service instance.
/// The lease is automatically renewed at a specified interval until it is disposed, at which point it is released.
/// If the lease cannot be renewed (e.g., if it has been acquired by another instance), the lease is considered lost.
/// </summary>
public sealed class DistributedLease : IAsyncDisposable
{
    private readonly CancellationTokenSource cts = new();
    private readonly Task renewalLoop;
    private readonly Func<CancellationToken, Task<bool>> renew;
    private readonly Func<CancellationToken, Task> release;

    /// <summary>
    /// Initializes a new instance of the DistributedLease class with the specified renewal and release functions, and the renewal interval.
    /// </summary>
    /// <param name="renew">A function that attempts to renew the lease. It should return true if the lease was successfully renewed, or false if the lease was lost.</param>
    /// <param name="release">A function that releases the lease when it is no longer needed.</param>
    /// <param name="renewInterval">The interval at which the lease should be renewed.</param>
    public DistributedLease(
        Func<CancellationToken, Task<bool>> renew,
        Func<CancellationToken, Task> release,
        TimeSpan renewInterval
    )
    {
        this.renew = renew;
        this.release = release;

        renewalLoop = Task.Run( () => RunRenewalLoopAsync( renewInterval, cts.Token ) );
    }

    /// <summary>
    /// A cancellation token that is triggered when the lease is lost or disposed.
    /// </summary>
    public CancellationToken CancellationToken => cts.Token;

    private async Task RunRenewalLoopAsync( TimeSpan renewInterval, CancellationToken cancellationToken )
    {
        while ( !cancellationToken.IsCancellationRequested )
        {
            await Task.Delay( renewInterval, cancellationToken );

            if ( !await renew( cancellationToken ) )
            {
                cts.Cancel(); // lost leadership
                return;
            }
        }
    }

    /// <summary>
    /// Disposes the lease asynchronously by canceling the renewal loop and releasing the lease.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        cts.Cancel();

        try 
        {
            await renewalLoop;
        }
        catch ( OperationCanceledException )
        { }

        await release( CancellationToken.None );

        cts.Dispose();
    }
}
