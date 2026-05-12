namespace Faactory.Leases.Redis;

internal sealed record RedisLeaseHandle( string Name, string OwnerId, TimeSpan Ttl ) : IDistributedLeaseHandle;
