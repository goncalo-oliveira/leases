using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Faactory.Leases.Redis;

internal sealed class RedisLeaseStore( IConnectionMultiplexer redisConnection, IOptions<RedisLeaseStoreOptions> optionsAccessor ) : IDistributedLeaseStore
{
    private readonly IDatabase redis = redisConnection.GetDatabase();
    private readonly RedisLeaseStoreOptions options = optionsAccessor.Value;

    public async Task<IDistributedLeaseHandle?> TryAcquireAsync( string name, string owner, TimeSpan ttl, CancellationToken cancellationToken )
    {
        var acquired = await redis.StringSetAsync(
            GetRedisKey( name ),
            owner,
            ttl,
            when: When.NotExists
        )
        .ConfigureAwait( false );

        return acquired
            ? new RedisLeaseHandle( name, owner, ttl )
            : null;
    }

    public async Task<bool> RenewAsync( IDistributedLeaseHandle leaseHandle, CancellationToken cancellationToken )
    {
        var result = (int)await redis.ScriptEvaluateAsync(
            LuaScripts.RenewScript,
            keys: [GetRedisKey( leaseHandle.Name )],
            values:
            [
                leaseHandle.OwnerId,
                (long)leaseHandle.Ttl.TotalMilliseconds
            ] )
            .ConfigureAwait( false );

        return result == 1;
    }

    public async Task<bool> ReleaseAsync( IDistributedLeaseHandle leaseHandle, CancellationToken cancellationToken )
    {
        var result = (int)await redis.ScriptEvaluateAsync(
            LuaScripts.ReleaseScript,
            keys: [GetRedisKey( leaseHandle.Name ),],
            values: [leaseHandle.OwnerId]
        )
        .ConfigureAwait( false );

        return result == 1;
    }

    private RedisKey GetRedisKey( string leaseName ) => $"{options.KeyPrefix}:{leaseName}";

    private static class LuaScripts
    {
        public static readonly string RenewScript = @"
            if redis.call('GET', KEYS[1]) == ARGV[1] then
                return redis.call('PEXPIRE', KEYS[1], ARGV[2])
            else
                return 0
            end";

        public static readonly string ReleaseScript = @"
            if redis.call('GET', KEYS[1]) == ARGV[1] then
                return redis.call('DEL', KEYS[1])
            else
                return 0
            end";
    }
}
