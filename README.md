# Distributed Coordination

Minimal abstractions for coordinating work across multiple instances.

Provides two independent primitives:

- **Distributed leases**: coordinate *who* runs
- **Distributed timers**: coordinate *when* work runs

They can be used separately or combined.

---

## Concepts

### Lease

A lease represents temporary ownership of a resource.

Used for:

- leader election
- singleton work
- mutual exclusion across instances

---

### Timer

A timer represents a shared periodic schedule.

Used for:

- periodic background tasks
- coordinated execution across nodes
- restart-safe scheduling without cron

---

## Registration

> [!IMPORTANT]
> Redis implementations require `IConnectionMultiplexer` to be registered.

### Redis leases

```csharp
services.AddRedisDistributedLeases( options =>
{
    options.KeyPrefix = "leases";
} );
```

### Redis timers

```csharp
services.AddRedisDistributedTimers( options =>
{
    options.KeyPrefix = "timers";
} );
```

---

## Usage

### Lease example

```csharp
public sealed class MyService( IDistributedLeaseStore leaseStore )
    : LeasedService( leaseStore )
{
    protected override string LeaseName => "my-service";

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        while ( !stoppingToken.IsCancellationRequested )
        {
            // do optional non-leader work here

            await using var lease = await TryAcquireLeaseAsync( stoppingToken );

            if ( lease is null )
            {
                continue;
            }

            // leader-only work
        }
    }
}
```

---

### Timer example

```csharp
public sealed class MyService( IDistributedTimerStore timerStore )
{
    public async Task ExecuteAsync( CancellationToken cancellationToken )
    {
        var timer = new DistributedTimer(
            timerStore,
            "my-timer",
            TimeSpan.FromMinutes( 10 )
        );

        while ( await timer.WaitForNextTickAsync( cancellationToken ) )
        {
            // periodic work
        }
    }
}
```

---

### Combined example

```csharp
public sealed class MyService(
    IDistributedLeaseStore leaseStore,
    IDistributedTimerStore timerStore
)
    : LeasedService( leaseStore )
{
    protected override string LeaseName => "my-service";

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        var timer = new DistributedTimer(
            timerStore,
            "my-timer",
            TimeSpan.FromMinutes( 10 )
        );

        while ( await timer.WaitForNextTickAsync( stoppingToken ) )
        {
            await using var lease = await TryAcquireLeaseAsync( stoppingToken );

            if ( lease is null )
            {
                continue;
            }

            // leader-only periodic work
        }
    }
}
```

---

## Redis Implementation

### Leases

- `SET NX PX` for acquisition
- Lua scripts for safe renew/release
- owner-checked operations

### Timers

- Redis TTL defines the timer window
- Lua script creates the timer if missing
- key expiration drives the next tick

Behavior:

- first run executes immediately
- subsequent runs occur every period
- timer state survives restarts while Redis persists the key
- all instances align on the same schedule
