namespace Faactory.Leases;

/// <summary>
/// Defines the contract for a distributed lease store, responsible for managing leases in a distributed environment.
/// </summary>
public interface IDistributedLeaseStore
{
    /// <summary>
    /// Attempts to acquire a lease for a given key and owner with a specified time-to-live (TTL).
    /// </summary>
    /// <param name="key">The key identifying the lease.</param>
    /// <param name="owner">The owner attempting to acquire the lease.</param>
    /// <param name="ttl">The time-to-live for the lease.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True if the lease was successfully acquired; otherwise, false.</returns>
    Task<bool> TryAcquireAsync( string key, string owner, TimeSpan ttl, CancellationToken cancellationToken = default );

    /// <summary>
    /// Renews an existing lease for a given key and owner with a new time-to-live (TTL).
    /// </summary>
    /// <param name="key">The key identifying the lease.</param>
    /// <param name="owner">The owner attempting to renew the lease.</param>
    /// <param name="ttl">The new time-to-live for the lease.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True if the lease was successfully renewed; otherwise, false.</returns>
    Task<bool> RenewAsync( string key, string owner, TimeSpan ttl, CancellationToken cancellationToken = default );

    /// <summary>
    /// Releases an existing lease for a given key and owner.
    /// </summary>
    /// <param name="key">The key identifying the lease.</param>
    /// <param name="owner">The owner attempting to release the lease.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True if the lease was successfully released; otherwise, false.</returns>
    Task<bool> ReleaseAsync( string key, string owner, CancellationToken cancellationToken = default );
}
