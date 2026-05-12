using Faactory.Leases;
using Faactory.Leases.NATS;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130

/// <summary>
/// Provides extension methods for registering services related to NATS distributed leases in the dependency injection container.
/// </summary>
public static class NatsDistributedLeasesServiceExtensions
{
    /// <summary>
    /// Adds the necessary services for using NATS distributed leases in a hosted service.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configure">An optional action to configure the NatsLeaseStoreOptions.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddNatsDistributedLeases( this IServiceCollection services, Action<NatsLeaseStoreOptions>? configure = null )
    {
        var optionsBuilder = services.AddOptions<NatsLeaseStoreOptions>()
            .Validate( options => !string.IsNullOrEmpty( options.BucketName ), "BucketName must be provided." )
            .Validate( options => options.BucketMarkerTtl > TimeSpan.Zero, "BucketMarkerTtl must be greater than zero." )
            .Validate( options => !string.IsNullOrEmpty( options.KeyPrefix ), "KeyPrefix must be provided." )
            .ValidateOnStart();

        if ( configure != null )
        {
            optionsBuilder.Configure( configure );
        }

        services.AddSingleton<IDistributedLeaseStore, NatsLeaseStore>();

        return services;
    }

    /// <summary>
    /// Adds the necessary services for using NATS distributed timers.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configure">An optional action to configure the NatsTimerStoreOptions.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddNatsDistributedTimers( this IServiceCollection services, Action<NatsTimerStoreOptions>? configure = null )
    {
        var optionsBuilder = services.AddOptions<NatsTimerStoreOptions>()
            .Validate( options => !string.IsNullOrEmpty( options.BucketName ), "BucketName must be provided." )
            .Validate( options => !string.IsNullOrEmpty( options.KeyPrefix ), "KeyPrefix must be provided." )
            .ValidateOnStart();

        if ( configure != null )
        {
            optionsBuilder.Configure( configure );
        }

        services.AddSingleton<IDistributedTimerStore, NatsTimerStore>();

        return services;
    }
}
