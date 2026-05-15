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

    // Multi-generic-parameter unions.
    Task<Either<int, string>> EchoEitherIntStringAsync(Either<int, string> value);
    Task<Either<int, Either<string, double>>> EchoEitherNestedAsync(Either<int, Either<string, double>> value);
    Task<Pair<int, string>> EchoPairIntStringAsync(Pair<int, string> value);
    Task<Triple<int, string, System.Guid>> EchoTripleAsync(Triple<int, string, System.Guid> value);
    Task<ConstrainedEither<int, string>> EchoConstrainedEitherAsync(ConstrainedEither<int, string> value);

    // Reference-type case payloads: exercises null-field edges over the wire.
    Task<Either<int, RefA>> EchoEitherIntRefAsync(Either<int, RefA> value);
    Task<Either<RefA, RefB>> EchoEitherRefRefAsync(Either<RefA, RefB> value);
    Task<Pair<RefA, RefB>> EchoPairRefRefAsync(Pair<RefA, RefB> value);
    Task<Triple<RefA, RefB, string>> EchoTripleRefAsync(Triple<RefA, RefB, string> value);
    Task<Either<int, Either<string, RefA>>> EchoEitherNestedRefAsync(Either<int, Either<string, RefA>> value);
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

    public Task<Either<int, string>> EchoEitherIntStringAsync(Either<int, string> value) => Task.FromResult(value);
    public Task<Either<int, Either<string, double>>> EchoEitherNestedAsync(Either<int, Either<string, double>> value) => Task.FromResult(value);
    public Task<Pair<int, string>> EchoPairIntStringAsync(Pair<int, string> value) => Task.FromResult(value);
    public Task<Triple<int, string, System.Guid>> EchoTripleAsync(Triple<int, string, System.Guid> value) => Task.FromResult(value);
    public Task<ConstrainedEither<int, string>> EchoConstrainedEitherAsync(ConstrainedEither<int, string> value) => Task.FromResult(value);

    public Task<Either<int, RefA>> EchoEitherIntRefAsync(Either<int, RefA> value) => Task.FromResult(value);
    public Task<Either<RefA, RefB>> EchoEitherRefRefAsync(Either<RefA, RefB> value) => Task.FromResult(value);
    public Task<Pair<RefA, RefB>> EchoPairRefRefAsync(Pair<RefA, RefB> value) => Task.FromResult(value);
    public Task<Triple<RefA, RefB, string>> EchoTripleRefAsync(Triple<RefA, RefB, string> value) => Task.FromResult(value);
    public Task<Either<int, Either<string, RefA>>> EchoEitherNestedRefAsync(Either<int, Either<string, RefA>> value) => Task.FromResult(value);
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

    // ── Multi-generic-parameter unions (cross-grain) ─────────────────

    [Fact]
    public async Task EitherIntString_Left_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoEitherIntStringAsync(
            new Either<int, string>(new Left<int>(7)));
        Assert.Equal(7, ((Left<int>)result.Value!).Value);
    }

    [Fact]
    public async Task EitherIntString_Right_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoEitherIntStringAsync(
            new Either<int, string>(new Right<string>("hi")));
        Assert.Equal("hi", ((Right<string>)result.Value!).Value);
    }

    [Fact]
    public async Task EitherNested_RoundTripsThroughCluster()
    {
        var inner = new Either<string, double>(new Right<double>(2.5));
        var msg = new Either<int, Either<string, double>>(
            new Right<Either<string, double>>(inner));
        var result = await Grain().EchoEitherNestedAsync(msg);
        var outerRight = Assert.IsType<Right<Either<string, double>>>(result.Value);
        var innerRight = Assert.IsType<Right<double>>(outerRight.Value.Value);
        Assert.Equal(2.5, innerRight.Value);
    }

    [Fact]
    public async Task Pair_Both_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoPairIntStringAsync(
            new Pair<int, string>(new Both<int, string>(42, "hello")));
        var both = Assert.IsType<Both<int, string>>(result.Value);
        Assert.Equal(42, both.First);
        Assert.Equal("hello", both.Second);
    }

    [Fact]
    public async Task Pair_Empty_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoPairIntStringAsync(
            new Pair<int, string>(new Empty()));
        Assert.IsType<Empty>(result.Value);
    }

    [Fact]
    public async Task Triple_AllArmsRoundTripThroughCluster()
    {
        var grain = Grain();
        var id = System.Guid.NewGuid();

        var r1 = await grain.EchoTripleAsync(new Triple<int, string, System.Guid>(new One<int>(1)));
        Assert.Equal(1, ((One<int>)r1.Value!).Value);

        var r2 = await grain.EchoTripleAsync(new Triple<int, string, System.Guid>(new Two<string>("two")));
        Assert.Equal("two", ((Two<string>)r2.Value!).Value);

        var r3 = await grain.EchoTripleAsync(new Triple<int, string, System.Guid>(new Three<System.Guid>(id)));
        Assert.Equal(id, ((Three<System.Guid>)r3.Value!).Value);
    }

    [Fact]
    public async Task ConstrainedEither_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoConstrainedEitherAsync(
            new ConstrainedEither<int, string>(new Left<int>(99)));
        Assert.Equal(99, ((Left<int>)result.Value!).Value);
    }

    // ── Null reference-type payload edges over the wire ──────────────

    [Fact]
    public async Task EitherIntRef_NullPayload_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoEitherIntRefAsync(
            new Either<int, RefA>(new Right<RefA>(null!)));
        var right = Assert.IsType<Right<RefA>>(result.Value);
        Assert.Null(right.Value);
    }

    [Fact]
    public async Task EitherRefRef_LeftNullPayload_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoEitherRefRefAsync(
            new Either<RefA, RefB>(new Left<RefA>(null!)));
        var left = Assert.IsType<Left<RefA>>(result.Value);
        Assert.Null(left.Value);
    }

    [Fact]
    public async Task PairRefRef_BothFieldsNull_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoPairRefRefAsync(
            new Pair<RefA, RefB>(new Both<RefA, RefB>(null!, null!)));
        var both = Assert.IsType<Both<RefA, RefB>>(result.Value);
        Assert.Null(both.First);
        Assert.Null(both.Second);
    }

    [Fact]
    public async Task PairRefRef_PartialNull_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoPairRefRefAsync(
            new Pair<RefA, RefB>(new Both<RefA, RefB>(new RefA("only-left"), null!)));
        var both = Assert.IsType<Both<RefA, RefB>>(result.Value);
        Assert.NotNull(both.First);
        Assert.Equal("only-left", both.First!.Name);
        Assert.Null(both.Second);
    }

    [Fact]
    public async Task TripleRef_NullPayload_RoundTripsThroughCluster()
    {
        var result = await Grain().EchoTripleRefAsync(
            new Triple<RefA, RefB, string>(new Two<RefB>(null!)));
        var two = Assert.IsType<Two<RefB>>(result.Value);
        Assert.Null(two.Value);
    }

    [Fact]
    public async Task EitherNestedRef_InnermostNull_RoundTripsThroughCluster()
    {
        var inner = new Either<string, RefA>(new Right<RefA>(null!));
        var msg = new Either<int, Either<string, RefA>>(
            new Right<Either<string, RefA>>(inner));
        var result = await Grain().EchoEitherNestedRefAsync(msg);
        var outerRight = Assert.IsType<Right<Either<string, RefA>>>(result.Value);
        var innerRight = Assert.IsType<Right<RefA>>(outerRight.Value.Value);
        Assert.Null(innerRight.Value);
    }
}
