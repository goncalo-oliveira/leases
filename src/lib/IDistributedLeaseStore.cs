namespace Faactory.Leases;

/// <summary>
/// Defines the contract for a distributed lease store, responsible for managing leases in a distributed environment.
/// </summary>
public interface IDistributedLeaseStore
{
    /// <summary>
    /// Attempts to acquire a lease for a given name and owner with a specified time-to-live (TTL).
    /// </summary>
    /// <param name="name">The name identifying the lease.</param>
    /// <param name="owner">The owner attempting to acquire the lease.</param>
    /// <param name="ttl">The time-to-live for the lease.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A handle to the acquired lease if successful, null otherwise.</returns>
    Task<IDistributedLeaseHandle?> TryAcquireAsync( string name, string owner, TimeSpan ttl, CancellationToken cancellationToken = default );

    /// <summary>
    /// Renews an existing lease, extending its time-to-live (TTL).
    /// </summary>
    /// <param name="leaseHandle">The handle of the lease to renew.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True if the lease was successfully renewed; otherwise, false.</returns>
    Task<bool> RenewAsync( IDistributedLeaseHandle leaseHandle, CancellationToken cancellationToken = default );

    /// <summary>
    /// Releases an existing lease.
    /// </summary>
    /// <param name="leaseHandle">The handle of the lease to release.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True if the lease was successfully released; otherwise, false.</returns>
    Task<bool> ReleaseAsync( IDistributedLeaseHandle leaseHandle, CancellationToken cancellationToken = default );
}
