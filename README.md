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
- coordinated execution across instances

---

### Timer

A timer represents a shared periodic schedule.

Used for:

- periodic background tasks
- coordinated execution across instances
- restart-safe scheduling without cron

Behavior:

- first run executes immediately
- subsequent runs occur every period
- timer state survives restarts while the backend persists the timer key
- all instances align on the same schedule

---

## Registration

Depending on the underlying implementation, different dependencies are required.

### Redis

To use Redis, install both Redis and the distributed coordination packages:

```bash
dotnet add package StackExchange.Redis
dotnet add package Faactory.Leases.Redis
```

> [!IMPORTANT]
> Redis implementations require `IConnectionMultiplexer` to be registered.

#### Leases

```csharp
services.AddRedisDistributedLeases( options =>
{
    options.KeyPrefix = "leases";
} );
```

#### Timers

```csharp
services.AddRedisDistributedTimers( options =>
{
    options.KeyPrefix = "timers";
} );
```

---

### NATS

To use NATS, install both NATS and the distributed coordination packages:

```bash
dotnet add package NATS.Client
dotnet add package Faactory.Leases.NATS
```

> [!IMPORTANT]
> NATS implementations require `NatsClient` to be registered.

#### Leases

```csharp
services.AddNatsDistributedLeases( options =>
{
    options.BucketName = "leases";
    options.BucketMarkerTtl = TimeSpan.FromMinutes( 5 );
    options.KeyPrefix = "leases";
} );
```

#### Timers

```csharp
services.AddNatsDistributedTimers( options =>
{
    options.BucketName = "timers";
    options.KeyPrefix = "timers";
} );
```

---

## Usage

### Acquiring a lease

```csharp
public sealed class MyService( IDistributedLeaseStore leaseStore )
{
    public async Task ExecuteAsync( CancellationToken cancellationToken )
    {
        var leaseHandle = await leaseStore.TryAcquireAsync(
            "my-lease",
            Guid.NewGuid().ToString(),
            TimeSpan.FromSeconds( 30 ),
            cancellationToken
        );

        if ( leaseHandle is null )
        {
            // failed to acquire lease
            return;
        }

        await using var lease = new DistributedLease(
            leaseStore,
            leaseHandle,
            TimeSpan.FromSeconds( 10 )
        );

        // do work while lease is held
    }
}
```

---

### Periodic execution

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

### Combining leases and timers

```csharp
public sealed class MyService(
    IDistributedLeaseStore leaseStore,
    IDistributedTimerStore timerStore
)
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
            // non-leader work can be done here (if any)

            var leaseHandle = await leaseStore.TryAcquireAsync(
                "my-lease",
                Guid.NewGuid().ToString(),
                TimeSpan.FromSeconds( 30 ),
                cancellationToken
            );

            if ( leaseHandle is null )
            {
                continue;
            }

            await using var lease = new DistributedLease(
                leaseStore,
                leaseHandle,
                TimeSpan.FromSeconds( 10 )
            );

            // leader-only periodic work
        }
    }
}
```

---

## Convenience APIs

The library also provides optional convenience base classes for hosted services.

### LeasedService

Convenience base class for leased services with opiniated defaults. Handles lease acquisition and renewal, allowing you to focus on the leader-only work.

```csharp
public sealed class MyService( ILoggerFactory loggerFactory, IDistributedLeaseStore leaseStore )
    : LeasedService( loggerFactory, leaseStore )
{
    protected override async Task ExecuteLeaderAsync( CancellationToken cancellationToken )
    {
        // leader-only work
    }
}
```

---

### PeriodicLeasedService

Convenience base class for periodic leased services with opiniated defaults. Handles lease acquisition, renewal, and periodic execution, allowing you to focus on the leader-only work.

```csharp
public sealed class MyService(
    ILoggerFactory loggerFactory,
    IDistributedLeaseStore leaseStore,
    IDistributedTimerStore timerStore
)
    : PeriodicLeasedService( loggerFactory, leaseStore, timerStore )
{
    protected override TimeSpan ExecutionInterval
        => TimeSpan.FromMinutes( 10 );

    protected override Task RunAsync( CancellationToken cancellationToken )
    {
        // leader-only periodic work

        return Task.CompletedTask;
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

---

## NATS Implementation

Uses NATS JetStream KV storage for leases and timers.

Leases rely on bucket markers, which require NATS Server v2.11+.
