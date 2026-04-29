namespace Faactory.Leases.Redis;

/// <summary>
/// Options for configuring the RedisTimerStore
/// </summary>
public sealed class RedisTimerStoreOptions
{
    /// <summary>
    /// The prefix to use for all timer keys in Redis. This helps to avoid key collisions and allows for better organization of keys in Redis.
    /// The default value is "timers", which means that all timer keys will be prefixed with "timers:" followed by the timer name.
    /// You can change this prefix if you want to use a different naming convention or if you want to group timers under a different category in Redis.
    /// For example, if you set the prefix to "myapp:timers", then all timer keys will be prefixed with "myapp:timers:" followed by the timer name.
    /// </summary>
    public string KeyPrefix { get; set; } = "timers";
}
