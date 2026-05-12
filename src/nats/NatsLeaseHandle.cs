namespace Faactory.Leases.NATS;

internal sealed class NatsLeaseHandle : IDistributedLeaseHandle
{
    public required string Name { get; init; }
    public required string OwnerId { get; init; }
    public required TimeSpan Ttl { get; init; }
    public ulong Revision { get; internal set; }
}
