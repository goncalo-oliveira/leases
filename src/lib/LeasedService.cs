using Microsoft.Extensions.Logging;

namespace Faactory.Leases;

/// <summary>
/// Convenience base class for services that use distributed leases to coordinate execution across instances.
/// </summary>
/// <param name="loggerFactory">The logger factory instance.</param>
/// <param name="leaseStore">The lease store used to acquire, renew, and release leases.</param>
public abstract class LeasedService( ILoggerFactory loggerFactory, IDistributedLeaseStore leaseStore )
    : LeaseAwareService( leaseStore )
{
    private readonly ILogger logger = loggerFactory.CreateLogger<LeasedService>();

    /// <summary>
    /// Executes the logic for acquiring the lease and running the service.
    /// This method is called by the background service infrastructure and should not be overridden by derived classes.
    /// Instead, derived classes should implement the ExecuteLeaderAsync method to define the actual work to be performed while holding the lease.
    /// </summary>
    /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected sealed override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        while ( !stoppingToken.IsCancellationRequested )
        {
            try
            {
                await using var lease = await TryAcquireLeaseAsync( stoppingToken );

                if ( lease is null )
                {
                    continue;
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    stoppingToken,
                    lease.CancellationToken
                );

                await ExecuteLeaderAsync( cts.Token );
            }
            catch ( OperationCanceledException )
            {
                // shutdown requested, exit loop
                break;
            }
            catch ( Exception ex )
            {
                logger.LogError( ex, "Failed to execute leased service for {LeaseName}.", LeaseName );
            }
        }
    }

    /// <summary>
    /// Executes the logic for the service when it holds the lease.
    /// This method is called by the instance that successfully acquires the lease.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected abstract Task ExecuteLeaderAsync( CancellationToken cancellationToken );
}
