namespace Faactory.Leases.Redis;

/// <summary>
/// Options for configuring the RedisLeaseStore
/// </summary>
public sealed class RedisLeaseStoreOptions
{
    /// <summary>
    /// The prefix to use for all lease keys in Redis. This helps to avoid key collisions and allows for better organization of keys in Redis.
    /// The default value is "leases", which means that all lease keys will be prefixed with "leases:" followed by the lease name.
    /// You can change this prefix if you want to use a different naming convention or if you want to group leases under a different category in Redis.
    /// For example, if you set the prefix to "myapp:leases", then all lease keys will be prefixed with "myapp:leases:" followed by the lease name.
    /// </summary>
    public string KeyPrefix { get; set; } = "leases";
}
