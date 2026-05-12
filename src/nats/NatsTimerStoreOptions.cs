namespace Faactory.Leases.NATS;

/// <summary>
/// Options for configuring the NatsTimerStore
/// </summary>
public sealed class NatsTimerStoreOptions
{
    /// <summary>
    /// The name of the NATS Key-Value store (bucket) to use for storing timers. This is required and must be unique within the NATS server.
    /// The default value is "timers", but you can change it to any name that suits your application. Just make sure that it does not conflict with other Key-Value stores used by your application or other applications sharing the same NATS server.
    /// </summary>
    public string BucketName { get; set; } = "timers";

    /// <summary>
    /// The prefix to use for all timer keys in NATS. This helps to avoid key collisions and allows for better organization of keys in NATS.
    /// The default value is "timers", which means that all timer keys will be prefixed with "timers." followed by the timer name.
    /// You can change this prefix if you want to use a different naming convention or if you want to group timers under a different category in NATS.
    /// For example, if you set the prefix to "myapp.timers", then all timer keys will be prefixed with "myapp.timers." followed by the timer name.
    /// </summary>
    public string KeyPrefix { get; set; } = "timers";
}
