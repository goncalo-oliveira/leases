using Microsoft.Extensions.Logging;

namespace Faactory.Leases;

/// <summary>
/// Convenience base class for services that execute periodic work while holding a distributed lease.
/// The service attempts to acquire the lease before each execution interval and runs only on the instance that currently holds the lease.
/// </summary>
/// <param name="loggerFactory">The logger factory instance.</param>
/// <param name="leaseStore">The lease store used to acquire, renew, and release leases.</param>
/// <param name="timerStore">The timer store used to manage timers for periodic execution.</param>
public abstract class PeriodicLeasedService( ILoggerFactory loggerFactory, IDistributedLeaseStore leaseStore, IDistributedTimerStore timerStore )
    : LeaseAwareService( leaseStore )
{
    private readonly ILogger logger = loggerFactory.CreateLogger<PeriodicLeasedService>();

    /// <summary>
    /// Gets the interval at which the periodic task should be executed.
    /// The instance that holds the lease will execute the task at this interval.
    /// </summary>
    protected abstract TimeSpan RunInterval { get; }

    /// <summary>
    /// Executes the logic for acquiring the lease and running the periodic task.
    /// This method is called by the background service infrastructure and should not be overridden by derived classes.
    /// Instead, derived classes should implement the RunAsync method to define the actual work to be performed while holding the lease.
    /// </summary>
    /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected sealed override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        var timer = new DistributedTimer(
            timerStore,
            $"{LeaseName}.timer",
            RunInterval,
            loggerFactory
        );

        while ( await timer.WaitForNextTickAsync( stoppingToken ) )
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

                await RunAsync( cts.Token );
            }
            catch ( OperationCanceledException ) when ( stoppingToken.IsCancellationRequested )
            {
                // Graceful shutdown
                break;
            }
            catch ( Exception ex )
            {
                logger.LogError( ex, "Failed to execute periodic task for {LeaseName}.", LeaseName );
            }
        }
    }

    /// <summary>
    /// Executes the periodic task. This method is called by the instance that holds the lease.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected abstract Task RunAsync( CancellationToken cancellationToken );
}
