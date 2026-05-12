using Faactory.Leases;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace tests;

public class LeaseTests
{
    private sealed class TestLeaseHandle( string ownerId ) : IDistributedLeaseHandle
    {
        public string Name => "test";
        public string OwnerId { get; } = ownerId;
        public TimeSpan Ttl => TimeSpan.FromSeconds( 30 );
    }

    internal sealed class TestService( IDistributedLeaseStore store )
        : LeasedService(store)
    {
        protected override string LeaseName => "test";
        protected override TimeSpan LeaseTtl => TimeSpan.FromMilliseconds( 200 );
        protected override TimeSpan RenewalInterval => TimeSpan.FromMilliseconds( 100 );

        protected override Task ExecuteAsync( CancellationToken stoppingToken )
        {
            return Task.CompletedTask;
        }

        public Task<DistributedLease?> TryAcquireAsync( CancellationToken cancellationToken )
            => TryAcquireLeaseAsync( cancellationToken );
    }

    [Fact]
    public async Task AcquireLease_Success_ReturnsLease()
    {
        var store = Substitute.For<IDistributedLeaseStore>();

        store.TryAcquireAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>()
        )
        .Returns( new TestLeaseHandle( "owner" ) );

        var service = new TestService( store );

        var lease = await service.TryAcquireAsync( CancellationToken.None );

        Assert.NotNull( lease );
    }

    [Fact]
    public async Task AcquireLease_Failure_ReturnsNull()
    {
        var store = Substitute.For<IDistributedLeaseStore>();

        store.TryAcquireAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>()
        )
        .ReturnsNull();

        var service = new TestService( store );

        var lease = await service.TryAcquireAsync( CancellationToken.None );

        Assert.Null( lease );
    }

    [Fact]
    public async Task Lease_StopsRenewing_When_RenewFails()
    {
        var store = Substitute.For<IDistributedLeaseStore>();

        store.TryAcquireAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>()
        )
        .Returns( new TestLeaseHandle( "owner" ) );

        store.RenewAsync(
            Arg.Any<IDistributedLeaseHandle>(),
            Arg.Any<CancellationToken>()
        )
        .Returns( true, false );

        var service = new TestService( store );

        var lease = await service.TryAcquireAsync( CancellationToken.None );

        await Task.Delay( 50 ); // let renewal loop run

        Assert.NotNull( lease );

        await lease.DisposeAsync();

        await store.Received().ReleaseAsync(
            Arg.Any<IDistributedLeaseHandle>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Dispose_Calls_Release()
    {
        var store = Substitute.For<IDistributedLeaseStore>();

        store.TryAcquireAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>()
        )
        .Returns( new TestLeaseHandle( "owner" ) );

        store.RenewAsync(
            Arg.Any<IDistributedLeaseHandle>(),
            Arg.Any<CancellationToken>()
        )
        .Returns( true );

        var service = new TestService( store );

        var lease = await service.TryAcquireAsync( CancellationToken.None );

        Assert.NotNull( lease );

        await lease.DisposeAsync();

        await store.Received( 1 ).ReleaseAsync(
            Arg.Any<IDistributedLeaseHandle>(),
            Arg.Any<CancellationToken>()
        );
    }
}
