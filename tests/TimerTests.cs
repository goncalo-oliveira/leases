using System.Diagnostics;
using Faactory.Leases;

namespace tests;

public class TimerTests
{
    [Fact]
    public async Task WaitForNextTickAsync_WhenRemainingIsZero_ReturnsImmediately()
    {
        var store = new FakeTimerStore();

        store.SetNext( TimeSpan.Zero );

        var timer = new DistributedTimer( store, "test", TimeSpan.FromSeconds( 10 ) );

        var sw = Stopwatch.StartNew();

        var result = await timer.WaitForNextTickAsync( CancellationToken.None );

        sw.Stop();

        Assert.True( result );
        Assert.True( sw.Elapsed < TimeSpan.FromMilliseconds( 50 ) );
    }

    [Fact]
    public async Task WaitForNextTickAsync_WaitsForRemainingTime()
    {
        var store = new FakeTimerStore();
        store.SetNext( TimeSpan.FromMilliseconds( 200 ) );

        var timer = new DistributedTimer( store, "test", TimeSpan.FromSeconds( 10 ) );

        var sw = Stopwatch.StartNew();

        var result = await timer.WaitForNextTickAsync( CancellationToken.None );

        sw.Stop();

        Assert.True( result );
        Assert.True( sw.Elapsed >= TimeSpan.FromMilliseconds( 180 ) );
    }

    [Fact]
    public async Task WaitForNextTickAsync_WhenCancelled_ReturnsFalse()
    {
        var store = new FakeTimerStore();
        store.SetNext( TimeSpan.FromSeconds( 5 ) );

        var timer = new DistributedTimer( store, "test", TimeSpan.FromSeconds( 10 ) );

        using var cts = new CancellationTokenSource( 100 );

        var result = await timer.WaitForNextTickAsync( cts.Token );

        Assert.False( result );
    }

    [Fact]
    public async Task WaitForNextTickAsync_RetriesOnFailure()
    {
        var store = new FailingStore();
        var timer = new DistributedTimer( store, "test", TimeSpan.FromSeconds( 10 ) );

        var result = await timer.WaitForNextTickAsync( CancellationToken.None );

        Assert.True( result );
    }

    internal sealed class FakeTimerStore : IDistributedTimerStore
    {
        private TimeSpan next;

        public void SetNext( TimeSpan value ) => next = value;

        public Task<TimeSpan> GetOrCreateTimerAsync( string key, TimeSpan period, CancellationToken cancellationToken )
            => Task.FromResult( next );
    }    

    internal sealed class FailingStore : IDistributedTimerStore
    {
        private int calls;

        public Task<TimeSpan> GetOrCreateTimerAsync( string key, TimeSpan period, CancellationToken cancellationToken )
        {
            if ( Interlocked.Increment( ref calls ) == 1 )
            {
                throw new Exception( "boom" );
            }

            return Task.FromResult( TimeSpan.Zero );
        }
    }
}
