using Faactory.Leases;
using Faactory.Leases.Redis;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130

/// <summary>
/// Provides extension methods for registering services related to Redis distributed leases in the dependency injection container.
/// </summary>
public static class RedisDistributedLeasesServiceExtensions
{
    /// <summary>
    /// Adds the necessary services for using Redis distributed leases in a hosted service.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configure">An optional action to configure the RedisLeaseStoreOptions.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddRedisDistributedLeases( this IServiceCollection services, Action<RedisLeaseStoreOptions>? configure = null )
    {
        if ( configure != null )
        {
            services.AddOptions<RedisLeaseStoreOptions>()
                .Validate( options => !string.IsNullOrEmpty( options.KeyPrefix ), "KeyPrefix must be provided." )
                .ValidateOnStart()
                .Configure( configure );
        }

        services.AddSingleton<IDistributedLeaseStore, RedisLeaseStore>();

        return services;
    }
}
