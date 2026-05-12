namespace Faactory.Leases;

/// <summary>
/// Represents a handle for a distributed lease
/// </summary>
public interface IDistributedLeaseHandle
{
    /// <summary>
    /// The name of the lease, which is used to identify the lease in the lease store.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The unique identifier for the owner of the lease, used for lease ownership and management.
    /// </summary>
    string OwnerId { get; }

    /// <summary>
    /// The time-to-live (TTL) for the lease, which determines how long the lease is valid before it expires.
    /// </summary>
    TimeSpan Ttl { get; }
}
