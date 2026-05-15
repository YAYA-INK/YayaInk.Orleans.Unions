using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using YayaInk.Orleans.Unions.Sample;

namespace YayaInk.Orleans.Unions.Tests;

// Validates that unions embedded inside larger Orleans messages round-trip
// correctly through the in-process serializer. This is the bridge case
// between RoundTripTests (union by itself) and EndToEndClusterTests (real
// silo dispatch): if outer codecs cannot delegate to our union codecs,
// these tests fail first.
public class MessageCompositionTests
{
    private static Serializer BuildSerializer()
    {
        var services = new ServiceCollection();
        services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
        return services.BuildServiceProvider().GetRequiredService<Serializer>();
    }

    private static T RoundTrip<T>(T value)
    {
        var s = BuildSerializer();
        return s.Deserialize<T>(s.SerializeToArray(value));
    }

    [Fact]
    public void CommandEnvelope_WithUserId_RoundTrips()
    {
        var msg = new CommandEnvelope("corr-1", new IdUnion(new UserId(7)), 1700000000L);
        var copy = RoundTrip(msg);
        Assert.Equal("corr-1", copy.CorrelationId);
        Assert.Equal(1700000000L, copy.Timestamp);
        Assert.IsType<UserId>(copy.Id.Value);
        Assert.Equal(7, ((UserId)copy.Id.Value!).Value);
    }

    [Fact]
    public void CommandEnvelope_WithDefaultUnion_RoundTrips()
    {
        var msg = new CommandEnvelope("corr-2", default, 0L);
        var copy = RoundTrip(msg);
        Assert.Null(copy.Id.Value);
    }

    [Fact]
    public void BatchEnvelope_WithMixedUnionElements_RoundTrips()
    {
        var msg = new BatchEnvelope("batch-1", new List<IdUnion>
        {
            new IdUnion(new UserId(1)),
            new IdUnion(new OrderId(2)),
            default,
            new IdUnion(new UserId(3)),
        });
        var copy = RoundTrip(msg);
        Assert.Equal(4, copy.Ids.Count);
        Assert.IsType<UserId>(copy.Ids[0].Value);
        Assert.IsType<OrderId>(copy.Ids[1].Value);
        Assert.Null(copy.Ids[2].Value);
        Assert.Equal(3, ((UserId)copy.Ids[3].Value!).Value);
    }

    [Fact]
    public void IndexedEnvelope_WithDictionaryOfUnions_RoundTrips()
    {
        var msg = new IndexedEnvelope(new Dictionary<string, IdUnion>
        {
            ["u"] = new IdUnion(new UserId(11)),
            ["o"] = new IdUnion(new OrderId(22)),
        });
        var copy = RoundTrip(msg);
        Assert.Equal(2, copy.Map.Count);
        Assert.Equal(11, ((UserId)copy.Map["u"].Value!).Value);
        Assert.Equal(22, ((OrderId)copy.Map["o"].Value!).Value);
    }

    [Fact]
    public void OuterEnvelope_NestedMessageWithUnion_RoundTrips()
    {
        var msg = new OuterEnvelope("topic-A",
            new InnerEnvelope(new IdUnion(new OrderId(99)), "note"));
        var copy = RoundTrip(msg);
        Assert.Equal("topic-A", copy.Topic);
        Assert.Equal("note", copy.Inner.Note);
        Assert.Equal(99, ((OrderId)copy.Inner.Id.Value!).Value);
    }

    [Fact]
    public void MultiUnionEnvelope_TwoUnionsCoexist_RoundTrips()
    {
        var msg = new MultiUnionEnvelope(
            new IdUnion(new UserId(5)),
            new Status(new Done("ok")),
            "tag-x");
        var copy = RoundTrip(msg);
        Assert.Equal(5, ((UserId)copy.Id.Value!).Value);
        var done = Assert.IsType<Done>(copy.Status.Value);
        Assert.Equal("ok", done.Result);
        Assert.Equal("tag-x", copy.Tag);
    }

    [Fact]
    public void ResultEnvelope_GenericUnionField_RoundTrips()
    {
        var msg = new ResultEnvelope("r-1", new Result<int>(new Err("nope")));
        var copy = RoundTrip(msg);
        Assert.Equal("r-1", copy.CorrelationId);
        Assert.Equal("nope", ((Err)copy.Result.Value!).Message);
    }

    [Fact]
    public void RefEnvelope_RefCaseUnionField_RoundTrips()
    {
        var msg = new RefEnvelope(new RefUnion(new RefA("alice")));
        var copy = RoundTrip(msg);
        var a = Assert.IsType<RefA>(copy.Payload.Value);
        Assert.Equal("alice", a.Name);
    }

    [Fact]
    public void NullableEnvelope_WithValue_RoundTrips()
    {
        var msg = new NullableEnvelope(new IdUnion(new UserId(13)));
        var copy = RoundTrip(msg);
        Assert.True(copy.MaybeId.HasValue);
        Assert.Equal(13, ((UserId)copy.MaybeId!.Value.Value!).Value);
    }

    [Fact]
    public void NullableEnvelope_WithNull_RoundTrips()
    {
        var msg = new NullableEnvelope(null);
        var copy = RoundTrip(msg);
        Assert.False(copy.MaybeId.HasValue);
    }
}
