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

### Lease example

```csharp
public sealed class MyService( IDistributedLeaseStore leaseStore )
    : LeasedService( leaseStore )
{
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
public sealed class MyService( IDistributedLeaseStore leaseStore, IDistributedTimerStore timerStore )
    : LeasedService( leaseStore )
{
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

--

### Using leases without LeasedService

```csharp
public sealed class MyService( IDistributedLeaseStore leaseStore )
{
    public async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        var leaseHandle = await leaseStore.TryAcquireAsync(
            "my-lease",
            Guid.NewGuid().ToString(), // unique owner ID
            TimeSpan.FromSeconds( 30 ),
            stoppingToken
        );

        if ( leaseHandle is null )
        {
            // failed to acquire lease
            return;
        }

        await using var lease = new DistributedLease( leaseStore, leaseHandle, TimeSpan.FromSeconds( 10 ) );

        // do work while lease is held
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

## NATS Implementation

Uses NATS JetStream KV storage for leases and timers.

Leases rely on bucket markers, which require NATS Server v2.11+.
