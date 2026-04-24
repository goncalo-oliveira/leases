# Distributed Leases

A minimal abstraction for coordinating work across multiple instances using **distributed leases**.

Supports scenarios like:
- leader election
- singleton background tasks
- coordination across nodes

---

## Concepts

- **Lease**: temporary ownership of a resource (with TTL)
- **Owner**: the instance holding the lease
- **Renewal**: keeps the lease alive
- **Expiration**: lease is lost if not renewed

---

## Usage

### 1. Register services

```csharp
services.AddRedisDistributedLeases( options =>
{
    options.KeyPrefix = "service-leases"; // optional: this is the default prefix
} );
```

> [!IMPORTANT]
> Requires `IConnectionMultiplexer` to be registered.

---

### 2. Create a service

```csharp
public sealed class MyService( IDistributedLeaseStore leaseStore )
    : LeasedService( leaseStore )
{
    protected override string LeaseName => "my-service";

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        while ( !stoppingToken.IsCancellationRequested )
        {
            // always-run work

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

## Redis Implementation

Uses:
- `SET NX PX` for acquisition
- Lua scripts for safe renew/release (owner-checked)

Keys are prefixed using `RedisLeaseStoreOptions.KeyPrefix`.

---

## Notes

- Lease loss is silent by default — design your leader work accordingly
- Renewal happens automatically while the lease is held
- The store abstraction allows future alternative backends (e.g. Postgres, etcd)

---

## Summary

- Simple API
- No storage coupling in services
- Safe distributed coordination via leases
