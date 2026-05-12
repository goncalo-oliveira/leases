
using Microsoft.Extensions.Options;
using NATS.Client.KeyValueStore;
using NATS.Net;

namespace Faactory.Leases.NATS;

internal sealed class NatsLeaseStore( NatsClient nats, IOptions<NatsLeaseStoreOptions> optionsAccessor ) : IDistributedLeaseStore
{
    private readonly INatsKVContext kvContext = nats.CreateKeyValueStoreContext();
    private readonly NatsLeaseStoreOptions options = optionsAccessor.Value;
    private readonly SemaphoreSlim storeLock = new( 1, 1) ;
    private INatsKVStore? store;

    public async Task<IDistributedLeaseHandle?> TryAcquireAsync( string name, string owner, TimeSpan ttl, CancellationToken cancellationToken )
    {
        var store = await GetStoreAsync( cancellationToken );

        var acquireResult = await store.TryCreateAsync(
            GetNatsKey( name ),
            owner,
            ttl: ttl,
            cancellationToken: cancellationToken
        )
        .ConfigureAwait( false );

        if ( !acquireResult.Success )
        {
            return null;
        }

        return new NatsLeaseHandle
        {
            Name = name,
            OwnerId = owner,
            Ttl = ttl,
            Revision = acquireResult.Value
        };
    }

    public async Task<bool> RenewAsync( IDistributedLeaseHandle leaseHandle, CancellationToken cancellationToken )
    {
        if ( store is null )
        {
            throw new InvalidOperationException( "Lease store has not been initialized." );
        }

        if ( leaseHandle is not NatsLeaseHandle natsLeaseHandle )
        {
            throw new ArgumentException( "Invalid lease handle type.", nameof( leaseHandle ) );
        }

        /*
        NATS KV conditional updates preserve the original entry TTL and therefore
        cannot be used to renew expiring leases.
        To refresh the TTL, we replace the lease entry using a CAS delete followed by a create operation.
        */

        if ( !await ReleaseAsync( leaseHandle, cancellationToken ) )
        {
            return false;
        }

        var acquired = await TryAcquireAsync( leaseHandle.Name, leaseHandle.OwnerId, leaseHandle.Ttl, cancellationToken );

        if ( acquired is null )
        {
            return false;
        }

        // Update the lease's revision to the new value returned by the update operation
        natsLeaseHandle.Revision = ((NatsLeaseHandle)acquired).Revision;

        return true;
    }

    public async Task<bool> ReleaseAsync( IDistributedLeaseHandle leaseHandle, CancellationToken cancellationToken )
    {
        if ( store is null )
        {
            throw new InvalidOperationException( "Lease store has not been initialized." );
        }

        if ( leaseHandle is not NatsLeaseHandle natsLeaseHandle )
        {
            throw new ArgumentException( "Invalid lease handle type.", nameof( leaseHandle ) );
        }

        var result = await store.TryDeleteAsync(
            GetNatsKey( natsLeaseHandle.Name ),
            new NatsKVDeleteOpts
            {
                Revision = natsLeaseHandle.Revision
            },
            cancellationToken: cancellationToken
        )
        .ConfigureAwait( false );

        return result.Success;
    }

    private string GetNatsKey( string leaseName ) => $"{options.KeyPrefix}.{leaseName}";

    private async Task<INatsKVStore> GetStoreAsync( CancellationToken cancellationToken )
    {
        if ( store is not null )
        {
            return store;
        }

        await storeLock.WaitAsync( cancellationToken )
            .ConfigureAwait( false );

        try
        {
            if ( store is not null )
            {
                return store;
            }

            store = await kvContext.CreateOrUpdateStoreAsync(
                new NatsKVConfig( options.BucketName )
                {
                    LimitMarkerTTL = options.BucketMarkerTtl,
                    History = 1,
                },
                cancellationToken
            ).ConfigureAwait( false );

            return store;
        }
        finally
        {
            storeLock.Release();
        }
    }
}
