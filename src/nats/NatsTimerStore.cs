
using Microsoft.Extensions.Options;
using NATS.Client.KeyValueStore;
using NATS.Net;

namespace Faactory.Leases.NATS;

internal sealed class NatsTimerStore( NatsClient nats, IOptions<NatsTimerStoreOptions> optionsAccessor ) : IDistributedTimerStore
{
    private readonly INatsKVContext kvContext = nats.CreateKeyValueStoreContext();
    private readonly NatsTimerStoreOptions options = optionsAccessor.Value;
    private readonly SemaphoreSlim storeLock = new( 1, 1) ;
    private INatsKVStore? store;

    public async Task<TimeSpan> GetOrCreateTimerAsync( string key, TimeSpan period, CancellationToken cancellationToken )
    {
        var store = await GetStoreAsync( cancellationToken );

        var natsKey = GetNatsKey( key );

        var createResult = await store.TryCreateAsync(
            natsKey,
            1,
            ttl: period,
            cancellationToken: cancellationToken
        )
        .ConfigureAwait( false );

        if ( createResult.Success )
        {
            return period;
        }

        try
        {
            var entry = await store.GetEntryAsync<long>(
                natsKey,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait( false );

            if ( entry.Error is not null || entry.Value <= 0 )
            {
                return TimeSpan.Zero;
            }

            var remaining =  entry.Created + period - DateTimeOffset.UtcNow;

            return remaining > TimeSpan.Zero
                ? remaining
                : TimeSpan.Zero;
        }
        catch ( NatsKVKeyNotFoundException )
        {
            return TimeSpan.Zero;
        }
    }

    private string GetNatsKey( string key ) => $"{options.KeyPrefix}.{key}";

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
                    History = 1,
                    LimitMarkerTTL = options.BucketMarkerTtl
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
