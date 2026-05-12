namespace Faactory.Leases;

/// <summary>
/// Interface for a distributed timer store, which allows to get or create timers that can be shared across multiple instances of an application.
/// </summary>
public interface IDistributedTimerStore
{
    /// <summary>
    /// Gets or creates a timer for the specified name and period.
    /// If a timer already exists for the name, it returns the remaining time until the timer expires.
    /// If no timer exists, it creates a new timer with the specified period and returns the full period.
    /// </summary>
    /// <param name="name">The unique name identifying the timer.</param>
    /// <param name="period">The period for the timer.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The remaining time until the timer expires, or the full period if a new timer was created.</returns>
    Task<TimeSpan> GetOrCreateTimerAsync( string name, TimeSpan period, CancellationToken cancellationToken = default );
}
