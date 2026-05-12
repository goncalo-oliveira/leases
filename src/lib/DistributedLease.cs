namespace Faactory.Leases;

/// <summary>
/// Represents an acquired distributed lease that can be renewed or released.
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
    /// Creates a new instance of the DistributedLease class with the specified lease store, lease handle, and optional renewal interval.
    /// </summary>
    /// <param name="leaseStore">The lease store to use for renewing and releasing the lease.</param>
    /// <param name="leaseHandle">The handle of the lease to manage.</param>
    /// <param name="renewInterval">The interval at which the lease should be renewed. If not specified, the lease will expire after the initial TTL.</param>
    public DistributedLease( IDistributedLeaseStore leaseStore, IDistributedLeaseHandle leaseHandle, TimeSpan? renewInterval = null )
    {
        renew = ct => renewInterval is not null && renewInterval.Value > TimeSpan.Zero
            ? leaseStore.RenewAsync( leaseHandle, ct )
            : Task.FromResult( false );
        release = ct => leaseStore.ReleaseAsync( leaseHandle, ct );

        renewalLoop = Task.Run( () => RunRenewalLoopAsync( renewInterval ?? leaseHandle.Ttl, cts.Token ) );
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

            try
            {
                if ( !await renew( cancellationToken ) )
                {
                    cts.Cancel(); // lost leadership
                    return;
                }
            }
            catch ( OperationCanceledException )
            {
                // cancellation requested, exit the loop
            }
            catch ( Exception )
            {
                cts.Cancel();
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
