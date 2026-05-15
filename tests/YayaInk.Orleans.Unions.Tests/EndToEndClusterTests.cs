using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Orleans.Serialization;
using Orleans.TestingHost;
using YayaInk.Orleans.Unions.Sample;

namespace YayaInk.Orleans.Unions.Tests;

// ============================================================
// End-to-end (TestingHost) verification.
//
// RoundTripTests only exercises Serializer<T> in-process; this suite
// stands up a real in-proc TestCluster (1 silo + 1 client) and routes
// unions through the full Client -> Silo -> Client serialization path,
// covering manifest provider resolution and cross-silo dispatch.
//
// Covered union shapes:
//   IdUnion              non-generic struct cases
//   Result<int>          generic union (Ok/Err)
//   Status               multi-case (>=3) union, exercises tag=3 branch
//   RefUnion             record class cases
//   Pet (Puppy, Animal)  inheritance overlap: derived stays derived
//   default(IdUnion)     uninitialized union
// ============================================================

public interface IUnionEchoGrain : IGrainWithStringKey
{
    Task<IdUnion> EchoIdUnionAsync(IdUnion value);
    Task<Result<int>> EchoResultIntAsync(Result<int> value);
    Task<Status> EchoStatusAsync(Status value);
    Task<RefUnion> EchoRefUnionAsync(RefUnion value);
    Task<Pet> EchoPetAsync(Pet value);

    Task<CommandEnvelope> EchoCommandAsync(CommandEnvelope value);
    Task<BatchEnvelope> EchoBatchAsync(BatchEnvelope value);
    Task<OuterEnvelope> EchoOuterAsync(OuterEnvelope value);
    Task<MultiUnionEnvelope> EchoMultiAsync(MultiUnionEnvelope value);
}

public sealed class UnionEchoGrain : Grain, IUnionEchoGrain
{
    public Task<IdUnion> EchoIdUnionAsync(IdUnion value) => Task.FromResult(value);
    public Task<Result<int>> EchoResultIntAsync(Result<int> value) => Task.FromResult(value);
    public Task<Status> EchoStatusAsync(Status value) => Task.FromResult(value);
    public Task<RefUnion> EchoRefUnionAsync(RefUnion value) => Task.FromResult(value);
    public Task<Pet> EchoPetAsync(Pet value) => Task.FromResult(value);

    public Task<CommandEnvelope> EchoCommandAsync(CommandEnvelope value) => Task.FromResult(value);
    public Task<BatchEnvelope> EchoBatchAsync(BatchEnvelope value) => Task.FromResult(value);
    public Task<OuterEnvelope> EchoOuterAsync(OuterEnvelope value) => Task.FromResult(value);
    public Task<MultiUnionEnvelope> EchoMultiAsync(MultiUnionEnvelope value) => Task.FromResult(value);
}

file sealed class UnionSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
    }
}

file sealed class UnionClientConfigurator : IClientBuilderConfigurator
{
    public void Configure(Microsoft.Extensions.Configuration.IConfiguration configuration, IClientBuilder clientBuilder)
    {
        clientBuilder.Services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
    }
}

public sealed class EndToEndClusterFixture : IAsyncLifetime
{
    public TestCluster Cluster { get; private set; } = default!;

    public Task InitializeAsync()
    {
        var builder = new TestClusterBuilder(initialSilosCount: 1);
        builder.AddSiloBuilderConfigurator<UnionSiloConfigurator>();
        builder.AddClientBuilderConfigurator<UnionClientConfigurator>();
        Cluster = builder.Build();
        return Cluster.DeployAsync();
    }

    public async Task DisposeAsync()
    {
        if (Cluster is not null)
        {
            await Cluster.StopAllSilosAsync();
            Cluster.Dispose();
        }
    }
}

public class EndToEndClusterTests : IClassFixture<EndToEndClusterFixture>
{
    private readonly EndToEndClusterFixture _fixture;

    public EndToEndClusterTests(EndToEndClusterFixture fixture) => _fixture = fixture;

    private IUnionEchoGrain Grain() =>
        _fixture.Cluster.GrainFactory.GetGrain<IUnionEchoGrain>("echo");

    [Fact]
    public async Task IdUnion_UserId_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoIdUnionAsync(new IdUnion(new UserId(7)));
        Assert.IsType<UserId>(result.Value);
        Assert.Equal(7, ((UserId)result.Value!).Value);
    }

    [Fact]
    public async Task IdUnion_OrderId_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoIdUnionAsync(new IdUnion(new OrderId(1001)));
        Assert.IsType<OrderId>(result.Value);
        Assert.Equal(1001, ((OrderId)result.Value!).Value);
    }

    [Fact]
    public async Task IdUnion_Default_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoIdUnionAsync(default);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task ResultInt_Ok_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoResultIntAsync(new Result<int>(new Ok<int>(42)));
        Assert.IsType<Ok<int>>(result.Value);
        Assert.Equal(42, ((Ok<int>)result.Value!).Value);
    }

    [Fact]
    public async Task ResultInt_Err_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoResultIntAsync(new Result<int>(new Err("boom")));
        Assert.IsType<Err>(result.Value);
        Assert.Equal("boom", ((Err)result.Value!).Message);
    }

    [Fact]
    public async Task Status_Pending_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoStatusAsync(new Status(new Pending()));
        Assert.IsType<Pending>(result.Value);
    }

    [Fact]
    public async Task Status_Running_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoStatusAsync(new Status(new Running(55)));
        Assert.IsType<Running>(result.Value);
        Assert.Equal(55, ((Running)result.Value!).Progress);
    }

    [Fact]
    public async Task Status_Done_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoStatusAsync(new Status(new Done("ok")));
        Assert.IsType<Done>(result.Value);
        Assert.Equal("ok", ((Done)result.Value!).Result);
    }

    [Fact]
    public async Task RefUnion_RefA_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoRefUnionAsync(new RefUnion(new RefA("alice")));
        var a = Assert.IsType<RefA>(result.Value);
        Assert.Equal("alice", a.Name);
    }

    [Fact]
    public async Task RefUnion_RefB_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoRefUnionAsync(new RefUnion(new RefB(7)));
        var b = Assert.IsType<RefB>(result.Value);
        Assert.Equal(7, b.Code);
    }

    [Fact]
    public async Task Pet_Puppy_PreservesSubclassThroughCluster()
    {
        var result = await Grain().EchoPetAsync(new Pet(new Puppy("rex")));
        var puppy = Assert.IsType<Puppy>(result.Value);
        Assert.Equal("rex", puppy.Name);
    }

    [Fact]
    public async Task Pet_Animal_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoPetAsync(new Pet(new Animal("kitty")));
        Assert.IsType<Animal>(result.Value);
        Assert.Equal("kitty", ((Animal)result.Value!).Name);
    }

    [Fact]
    public async Task CommandEnvelope_RoundTripsThroughCluster()
    {
        var msg = new CommandEnvelope("corr-1", new IdUnion(new UserId(7)), 1700000000L);
        var result = await Grain().EchoCommandAsync(msg);
        Assert.Equal("corr-1", result.CorrelationId);
        Assert.Equal(1700000000L, result.Timestamp);
        Assert.Equal(7, ((UserId)result.Id.Value!).Value);
    }

    [Fact]
    public async Task BatchEnvelope_WithMixedUnionElements_RoundTripsThroughCluster()
    {
        var msg = new BatchEnvelope("batch-1", new List<IdUnion>
        {
            new IdUnion(new UserId(1)),
            new IdUnion(new OrderId(2)),
            default,
        });
        var result = await Grain().EchoBatchAsync(msg);
        Assert.Equal(3, result.Ids.Count);
        Assert.Equal(1, ((UserId)result.Ids[0].Value!).Value);
        Assert.Equal(2, ((OrderId)result.Ids[1].Value!).Value);
        Assert.Null(result.Ids[2].Value);
    }

    [Fact]
    public async Task OuterEnvelope_NestedMessage_RoundTripsThroughCluster()
    {
        var msg = new OuterEnvelope("topic-A",
            new InnerEnvelope(new IdUnion(new OrderId(99)), "note"));
        var result = await Grain().EchoOuterAsync(msg);
        Assert.Equal("topic-A", result.Topic);
        Assert.Equal("note", result.Inner.Note);
        Assert.Equal(99, ((OrderId)result.Inner.Id.Value!).Value);
    }

    [Fact]
    public async Task MultiUnionEnvelope_TwoUnionsCoexist_RoundTripsThroughCluster()
    {
        var msg = new MultiUnionEnvelope(
            new IdUnion(new UserId(5)),
            new Status(new Done("ok")),
            "tag-x");
        var result = await Grain().EchoMultiAsync(msg);
        Assert.Equal(5, ((UserId)result.Id.Value!).Value);
        var done = Assert.IsType<Done>(result.Status.Value);
        Assert.Equal("ok", done.Result);
        Assert.Equal("tag-x", result.Tag);
    }
}
