using Microsoft.Extensions.Hosting;

namespace Faactory.Leases;

/// <summary>
/// Represents a long-running leased service that uses a distributed lease to ensure that only one instance of the service is active at a time.
/// </summary>
/// <param name="leaseStore">The lease store used to acquire, renew, and release leases.</param>
public abstract class LeasedService( IDistributedLeaseStore leaseStore ) : BackgroundService
{
    /// <summary>
    /// A unique identifier for the service instance, used for lease ownership.
    /// By default, it generates a new GUID in "n" format (32 digits) using version 7.
    /// You can override this property to provide a custom identifier if needed.
    /// </summary>
    protected virtual string OwnerId { get; } = Guid.CreateVersion7().ToString( "n" );

    /// <summary>
    /// The name of the lease, which is used to identify the lease in the lease store.
    /// </summary>
    protected abstract string LeaseName { get; }

    /// <summary>
    /// The time-to-live (TTL) for the lease, which determines how long the lease is valid before it expires.
    /// </summary>
    protected virtual TimeSpan LeaseTtl => TimeSpan.FromSeconds( 30 );

    /// <summary>
    /// The interval at which the service should attempt to renew the lease before it expires.
    /// </summary>
    protected virtual TimeSpan RenewalInterval => TimeSpan.FromSeconds( 10 );

    /// <summary>
    /// The interval at which the service should retry acquiring the lease if it fails to acquire it initially.
    /// </summary>
    protected virtual TimeSpan RetryInterval => RenewalInterval;

    /// <summary>
    /// Attempts to acquire the lease for the service.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A DistributedLease object if the lease is successfully acquired; otherwise, null.</returns>
    protected async Task<DistributedLease?> TryAcquireLeaseAsync( CancellationToken cancellationToken )
    {
        var acquired = await leaseStore.TryAcquireAsync(
            LeaseName,
            OwnerId,
            LeaseTtl,
            cancellationToken
        );

        if ( !acquired )
        {
            await JitterDelay.DelayAsync( RetryInterval, cancellationToken );

            return null;
        }

        return new DistributedLease(
            renew: ct => leaseStore.RenewAsync( LeaseName, OwnerId, LeaseTtl, ct ),
            release: ct => leaseStore.ReleaseAsync( LeaseName, OwnerId, ct ),
            renewInterval: RenewalInterval
        );
    }
}
