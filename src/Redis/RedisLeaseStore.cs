using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Faactory.Leases.Redis;

internal sealed class RedisLeaseStore( IConnectionMultiplexer redisConnection, IOptions<RedisLeaseStoreOptions> optionsAccessor ) : IDistributedLeaseStore
{
    private readonly IDatabase redis = redisConnection.GetDatabase();
    private readonly RedisLeaseStoreOptions options = optionsAccessor.Value;

    public async Task<bool> TryAcquireAsync( string key, string owner, TimeSpan ttl, CancellationToken cancellationToken )
    {
        return await redis.StringSetAsync(
            GetRedisKey( key ),
            owner,
            ttl,
            when: When.NotExists
        )
        .ConfigureAwait( false );
    }

    public async Task<bool> RenewAsync( string key, string owner, TimeSpan ttl, CancellationToken cancellationToken )
    {
        var result = (int)await redis.ScriptEvaluateAsync(
            LuaScripts.RenewScript,
            keys: [GetRedisKey( key )],
            values:
            [
                owner,
                (long)ttl.TotalMilliseconds
            ] )
            .ConfigureAwait( false );

        return result == 1;
    }

    public async Task<bool> ReleaseAsync( string key, string owner, CancellationToken cancellationToken )
    {
        var result = (int)await redis.ScriptEvaluateAsync(
            LuaScripts.ReleaseScript,
            keys: [GetRedisKey( key ),],
            values: [owner]
        )
        .ConfigureAwait( false );

        return result == 1;
    }

    private RedisKey GetRedisKey( string key ) => $"{options.KeyPrefix}:{key}";

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
