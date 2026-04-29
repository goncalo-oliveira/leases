using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Faactory.Leases.Redis;

internal sealed class RedisTimerStore( IConnectionMultiplexer redisConnection, IOptions<RedisTimerStoreOptions> optionsAccessor ) : IDistributedTimerStore
{
    private readonly IDatabase redis = redisConnection.GetDatabase();
    private readonly RedisTimerStoreOptions options = optionsAccessor.Value;

    public async Task<TimeSpan> GetOrCreateTimerAsync( string key, TimeSpan period, CancellationToken cancellationToken )
    {
        var result = (long)await redis.ScriptEvaluateAsync(
            LuaScripts.GetTimerScript,
            keys: [GetRedisKey( key )],
            values:
            [
                "1", // Value doesn't matter, we only care about the TTL
                (long)period.TotalMilliseconds
            ] )
            .ConfigureAwait( false );

        return result <= 0
            ? TimeSpan.Zero
            : TimeSpan.FromMilliseconds( result );
    }

    private RedisKey GetRedisKey( string key ) => $"{options.KeyPrefix}:{key}";

    private static class LuaScripts
    {
        public static readonly string GetTimerScript =
        """
        local ttl = redis.call('PTTL', KEYS[1])

        if ttl <= 0 then
            redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[2])
            return 0
        end

        return ttl
        """;
    }
}
