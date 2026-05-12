namespace Faactory.Leases.NATS;

/// <summary>
/// Options for configuring the NatsLeaseStore
/// </summary>
public sealed class NatsLeaseStoreOptions
{
    /// <summary>
    /// The name of the NATS Key-Value store (bucket) to use for storing leases. This is required and must be unique within the NATS server.
    /// The default value is "leases", but you can change it to any name that suits your application. Just make sure that it does not conflict with other Key-Value stores used by your application or other applications sharing the same NATS server.
    /// </summary>
    public string BucketName { get; set; } = "leases";

    /// <summary>
    /// How long the bucket keeps markers when keys are removed by the TTL setting.
    /// </summary>
    public TimeSpan BucketMarkerTtl { get; set; } = TimeSpan.FromMinutes( 5 );

    /// <summary>
    /// The prefix to use for all lease keys in NATS. This helps to avoid key collisions and allows for better organization of keys in NATS.
    /// The default value is "leases", which means that all lease keys will be prefixed with "leases." followed by the lease name.
    /// You can change this prefix if you want to use a different naming convention or if you want to group leases under a different category in NATS.
    /// For example, if you set the prefix to "myapp.leases", then all lease keys will be prefixed with "myapp.leases." followed by the lease name.
    /// </summary>
    public string KeyPrefix { get; set; } = "leases";
}
