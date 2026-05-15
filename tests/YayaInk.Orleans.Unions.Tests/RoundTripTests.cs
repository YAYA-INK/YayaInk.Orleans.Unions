using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.Cloning;
using YayaInk.Orleans.Unions.Sample;

namespace YayaInk.Orleans.Unions.Tests;

// Validates non-generic and generic unions through Orleans Serializer<T>
// and DeepCopier<T> directly. The cluster path is exercised by
// EndToEndClusterTests; the message-composition path by MessageCompositionTests.
public class RoundTripTests
{
    private static Serializer BuildSerializer()
    {
        var services = new ServiceCollection();
        services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
        return services.BuildServiceProvider().GetRequiredService<Serializer>();
    }

    [Fact]
    public void IdUnion_UserId_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new IdUnion(new UserId(7));
        var copy = s.Deserialize<IdUnion>(s.SerializeToArray(original));
        Assert.IsType<UserId>(copy.Value);
        Assert.Equal(7, ((UserId)copy.Value!).Value);
    }

    [Fact]
    public void IdUnion_OrderId_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new IdUnion(new OrderId(1001));
        var copy = s.Deserialize<IdUnion>(s.SerializeToArray(original));
        Assert.IsType<OrderId>(copy.Value);
        Assert.Equal(1001, ((OrderId)copy.Value!).Value);
    }

    [Fact]
    public void IdUnion_DeepCopy_ReturnsEquivalent()
    {
        var services = new ServiceCollection();
        services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
        var sp = services.BuildServiceProvider();
        var copier = sp.GetRequiredService<DeepCopier<IdUnion>>();

        var original = new IdUnion(new UserId(42));
        var copy = copier.Copy(original);
        Assert.IsType<UserId>(copy.Value);
        Assert.Equal(42, ((UserId)copy.Value!).Value);
    }

    [Fact]
    public void Default_Union_PreservesNullValue()
    {
        var s = BuildSerializer();
        IdUnion original = default;
        var copy = s.Deserialize<IdUnion>(s.SerializeToArray(original));
        Assert.Null(copy.Value);
    }

    // ── Generic unions ────────────────────────────────

    [Fact]
    public void ResultInt_Ok_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Result<int>(new Ok<int>(42));
        var copy = s.Deserialize<Result<int>>(s.SerializeToArray(original));
        Assert.IsType<Ok<int>>(copy.Value);
        Assert.Equal(42, ((Ok<int>)copy.Value!).Value);
    }

    [Fact]
    public void ResultInt_Err_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Result<int>(new Err("boom"));
        var copy = s.Deserialize<Result<int>>(s.SerializeToArray(original));
        Assert.IsType<Err>(copy.Value);
        Assert.Equal("boom", ((Err)copy.Value!).Message);
    }

    [Fact]
    public void ResultString_Ok_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Result<string>(new Ok<string>("hello"));
        var copy = s.Deserialize<Result<string>>(s.SerializeToArray(original));
        Assert.IsType<Ok<string>>(copy.Value);
        Assert.Equal("hello", ((Ok<string>)copy.Value!).Value);
    }

    [Fact]
    public void OptionString_Some_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Option<string>(new Some<string>("world"));
        var copy = s.Deserialize<Option<string>>(s.SerializeToArray(original));
        Assert.IsType<Some<string>>(copy.Value);
        Assert.Equal("world", ((Some<string>)copy.Value!).Value);
    }

    [Fact]
    public void OptionString_None_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Option<string>(new None());
        var copy = s.Deserialize<Option<string>>(s.SerializeToArray(original));
        Assert.IsType<None>(copy.Value);
    }

    [Fact]
    public void ResultInt_DeepCopy_ReturnsEquivalent()
    {
        var services = new ServiceCollection();
        services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
        var sp = services.BuildServiceProvider();
        var copier = sp.GetRequiredService<DeepCopier<Result<int>>>();

        var original = new Result<int>(new Ok<int>(99));
        var copy = copier.Copy(original);
        Assert.IsType<Ok<int>>(copy.Value);
        Assert.Equal(99, ((Ok<int>)copy.Value!).Value);
    }

    [Fact]
    public void Default_ResultInt_PreservesNullValue()
    {
        var s = BuildSerializer();
        Result<int> original = default;
        var copy = s.Deserialize<Result<int>>(s.SerializeToArray(original));
        Assert.Null(copy.Value);
    }

    [Fact]
    public void Default_OptionString_PreservesNullValue()
    {
        var s = BuildSerializer();
        Option<string> original = default;
        var copy = s.Deserialize<Option<string>>(s.SerializeToArray(original));
        Assert.Null(copy.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void ResultInt_Ok_EdgeIntegers_RoundTrip(int v)
    {
        var s = BuildSerializer();
        var original = new Result<int>(new Ok<int>(v));
        var copy = s.Deserialize<Result<int>>(s.SerializeToArray(original));
        Assert.Equal(v, ((Ok<int>)copy.Value!).Value);
    }

    [Fact]
    public void ResultString_Ok_EmptyString_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Result<string>(new Ok<string>(string.Empty));
        var copy = s.Deserialize<Result<string>>(s.SerializeToArray(original));
        Assert.Equal(string.Empty, ((Ok<string>)copy.Value!).Value);
    }

    [Fact]
    public void ResultString_Ok_LongUnicodeString_RoundTrip()
    {
        var s = BuildSerializer();
        var payload = new string('字', 4096) + "🚀汉字abc";
        var original = new Result<string>(new Ok<string>(payload));
        var copy = s.Deserialize<Result<string>>(s.SerializeToArray(original));
        Assert.Equal(payload, ((Ok<string>)copy.Value!).Value);
    }

    [Fact]
    public void IdUnion_RoundTrip_PreservesRecordEquality()
    {
        var s = BuildSerializer();
        var original = new IdUnion(new UserId(7));
        var copy = s.Deserialize<IdUnion>(s.SerializeToArray(original));
        Assert.Equal((UserId)original.Value!, (UserId)copy.Value!);
    }

    [Fact]
    public void ResultInt_RoundTrip_PreservesRecordEquality()
    {
        var s = BuildSerializer();
        var original = new Result<int>(new Ok<int>(123));
        var copy = s.Deserialize<Result<int>>(s.SerializeToArray(original));
        Assert.Equal((Ok<int>)original.Value!, (Ok<int>)copy.Value!);
    }

    [Fact]
    public void IdUnion_Bytes_Are_Compatible_Across_Serializer_Instances()
    {
        var s1 = BuildSerializer();
        var s2 = BuildSerializer();
        var original = new IdUnion(new OrderId(2024));
        var bytes = s1.SerializeToArray(original);
        var copy = s2.Deserialize<IdUnion>(bytes);
        Assert.Equal(2024, ((OrderId)copy.Value!).Value);
    }

    [Fact]
    public void IdUnion_Repeated_RoundTrip_IsStable()
    {
        var s = BuildSerializer();
        var current = new IdUnion(new UserId(5));
        byte[]? lastBytes = null;
        for (var i = 0; i < 5; i++)
        {
            var bytes = s.SerializeToArray(current);
            if (lastBytes is not null)
            {
                Assert.Equal(lastBytes, bytes);
            }
            lastBytes = bytes;
            current = s.Deserialize<IdUnion>(bytes);
        }
        Assert.Equal(5, ((UserId)current.Value!).Value);
    }

    [Fact]
    public void List_Of_IdUnion_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new List<IdUnion>
        {
            new IdUnion(new UserId(1)),
            new IdUnion(new OrderId(2)),
            new IdUnion(new UserId(3)),
            default,
        };
        var copy = s.Deserialize<List<IdUnion>>(s.SerializeToArray(original));
        Assert.Equal(4, copy.Count);
        Assert.Equal(1, ((UserId)copy[0].Value!).Value);
        Assert.Equal(2, ((OrderId)copy[1].Value!).Value);
        Assert.Equal(3, ((UserId)copy[2].Value!).Value);
        Assert.Null(copy[3].Value);
    }

    [Fact]
    public void Array_Of_ResultInt_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Result<int>[]
        {
            new(new Ok<int>(10)),
            new(new Err("nope")),
            new(new Ok<int>(-7)),
        };
        var copy = s.Deserialize<Result<int>[]>(s.SerializeToArray(original));
        Assert.Equal(3, copy.Length);
        Assert.Equal(10, ((Ok<int>)copy[0].Value!).Value);
        Assert.Equal("nope", ((Err)copy[1].Value!).Message);
        Assert.Equal(-7, ((Ok<int>)copy[2].Value!).Value);
    }

    [Fact]
    public void Dictionary_With_OptionString_Values_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Dictionary<string, Option<string>>
        {
            ["a"] = new Option<string>(new Some<string>("alpha")),
            ["b"] = new Option<string>(new None()),
        };
        var copy = s.Deserialize<Dictionary<string, Option<string>>>(s.SerializeToArray(original));
        Assert.Equal(2, copy.Count);
        Assert.Equal("alpha", ((Some<string>)copy["a"].Value!).Value);
        Assert.IsType<None>(copy["b"].Value);
    }

    // Nested unions: a union as the payload of another union's case.

    [Fact]
    public void Option_Of_ResultInt_RoundTrip_OkInside()
    {
        var s = BuildSerializer();
        var original = new Option<Result<int>>(new Some<Result<int>>(new Result<int>(new Ok<int>(77))));
        var copy = s.Deserialize<Option<Result<int>>>(s.SerializeToArray(original));
        var some = Assert.IsType<Some<Result<int>>>(copy.Value);
        Assert.Equal(77, ((Ok<int>)some.Value.Value!).Value);
    }

    [Fact]
    public void Option_Of_ResultInt_RoundTrip_ErrInside()
    {
        var s = BuildSerializer();
        var original = new Option<Result<int>>(new Some<Result<int>>(new Result<int>(new Err("bad"))));
        var copy = s.Deserialize<Option<Result<int>>>(s.SerializeToArray(original));
        var some = Assert.IsType<Some<Result<int>>>(copy.Value);
        Assert.Equal("bad", ((Err)some.Value.Value!).Message);
    }

    [Fact]
    public void Option_Of_ResultInt_RoundTrip_NoneOuter()
    {
        var s = BuildSerializer();
        var original = new Option<Result<int>>(new None());
        var copy = s.Deserialize<Option<Result<int>>>(s.SerializeToArray(original));
        Assert.IsType<None>(copy.Value);
    }

    [Fact]
    public void IdUnion_DeepCopy_DoesNotShareState()
    {
        var services = new ServiceCollection();
        services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
        var sp = services.BuildServiceProvider();
        var copier = sp.GetRequiredService<DeepCopier<IdUnion>>();

        var original = new IdUnion(new UserId(11));
        var copy = copier.Copy(original);

        Assert.Equal(11, ((UserId)original.Value!).Value);
        Assert.Equal(11, ((UserId)copy.Value!).Value);
    }

    [Fact]
    public void OptionString_DeepCopy_RoundTrip()
    {
        var services = new ServiceCollection();
        services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
        var sp = services.BuildServiceProvider();
        var copier = sp.GetRequiredService<DeepCopier<Option<string>>>();

        var original = new Option<string>(new Some<string>("hello"));
        var copy = copier.Copy(original);
        Assert.Equal("hello", ((Some<string>)copy.Value!).Value);
    }

    [Fact]
    public void Default_IdUnion_DeepCopy_PreservesNull()
    {
        var services = new ServiceCollection();
        services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
        var sp = services.BuildServiceProvider();
        var copier = sp.GetRequiredService<DeepCopier<IdUnion>>();

        IdUnion original = default;
        var copy = copier.Copy(original);
        Assert.Null(copy.Value);
    }

    // Multi-case (≥3) union: covers tag=1/2/3 write/read branches.

    [Fact]
    public void Status_Pending_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Status(new Pending());
        var copy = s.Deserialize<Status>(s.SerializeToArray(original));
        Assert.IsType<Pending>(copy.Value);
    }

    [Fact]
    public void Status_Running_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Status(new Running(50));
        var copy = s.Deserialize<Status>(s.SerializeToArray(original));
        var v = Assert.IsType<Running>(copy.Value);
        Assert.Equal(50, v.Progress);
    }

    [Fact]
    public void Status_Done_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Status(new Done("ok"));
        var copy = s.Deserialize<Status>(s.SerializeToArray(original));
        var v = Assert.IsType<Done>(copy.Value);
        Assert.Equal("ok", v.Result);
    }

    [Fact]
    public void Status_AllVariants_InContainer_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new List<Status>
        {
            new Status(new Pending()),
            new Status(new Running(1)),
            new Status(new Done("end")),
            default,
        };
        var copy = s.Deserialize<List<Status>>(s.SerializeToArray(original));
        Assert.Equal(4, copy.Count);
        Assert.IsType<Pending>(copy[0].Value);
        Assert.Equal(1, ((Running)copy[1].Value!).Progress);
        Assert.Equal("end", ((Done)copy[2].Value!).Result);
        Assert.Null(copy[3].Value);
    }

    // Reference-type cases (record class).

    [Fact]
    public void RefUnion_RefA_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new RefUnion(new RefA("alpha"));
        var copy = s.Deserialize<RefUnion>(s.SerializeToArray(original));
        var v = Assert.IsType<RefA>(copy.Value);
        Assert.Equal("alpha", v.Name);
    }

    [Fact]
    public void RefUnion_RefB_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new RefUnion(new RefB(7));
        var copy = s.Deserialize<RefUnion>(s.SerializeToArray(original));
        var v = Assert.IsType<RefB>(copy.Value);
        Assert.Equal(7, v.Code);
    }

    [Fact]
    public void RefUnion_DeepCopy_ProducesDistinctInstance()
    {
        var services = new ServiceCollection();
        services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
        var sp = services.BuildServiceProvider();
        var copier = sp.GetRequiredService<DeepCopier<RefUnion>>();

        var inner = new RefA("orig");
        var original = new RefUnion(inner);
        var copy = copier.Copy(original);

        var copiedInner = Assert.IsType<RefA>(copy.Value);
        Assert.Equal("orig", copiedInner.Name);
        Assert.NotSame(inner, copiedInner);
    }

    [Fact]
    public void List_Of_RefUnion_With_Default_Element_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new List<RefUnion>
        {
            new RefUnion(new RefA("a")),
            default,
            new RefUnion(new RefB(2)),
        };
        var copy = s.Deserialize<List<RefUnion>>(s.SerializeToArray(original));
        Assert.Equal(3, copy.Count);
        Assert.Equal("a", ((RefA)copy[0].Value!).Name);
        Assert.Null(copy[1].Value);
        Assert.Equal(2, ((RefB)copy[2].Value!).Code);
    }

    // Inheritance overlap: derived case must match before base case.

    [Fact]
    public void Pet_Puppy_RoundTrip_PreservesDerivedType()
    {
        var s = BuildSerializer();
        var original = new Pet(new Puppy("Max"));
        var copy = s.Deserialize<Pet>(s.SerializeToArray(original));
        var v = Assert.IsType<Puppy>(copy.Value);
        Assert.Equal("Max", v.Name);
    }

    [Fact]
    public void Pet_Animal_RoundTrip_StaysAsAnimal()
    {
        var s = BuildSerializer();
        var original = new Pet(new Animal("Wild"));
        var copy = s.Deserialize<Pet>(s.SerializeToArray(original));
        Assert.IsType<Animal>(copy.Value);
        Assert.Equal("Wild", ((Animal)copy.Value!).Name);
    }

    [Fact]
    public void Pet_DeepCopy_PreservesDerivedType()
    {
        var services = new ServiceCollection();
        services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
        var sp = services.BuildServiceProvider();
        var copier = sp.GetRequiredService<DeepCopier<Pet>>();

        var original = new Pet(new Puppy("Lucky"));
        var copy = copier.Copy(original);
        var v = Assert.IsType<Puppy>(copy.Value);
        Assert.Equal("Lucky", v.Name);
    }

    // Multi-level nested union.

    [Fact]
    public void Triple_Nested_Union_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Option<Option<Result<int>>>(
            new Some<Option<Result<int>>>(
                new Option<Result<int>>(
                    new Some<Result<int>>(new Result<int>(new Ok<int>(123))))));

        var copy = s.Deserialize<Option<Option<Result<int>>>>(s.SerializeToArray(original));

        var l1 = Assert.IsType<Some<Option<Result<int>>>>(copy.Value);
        var l2 = Assert.IsType<Some<Result<int>>>(l1.Value.Value);
        var l3 = Assert.IsType<Ok<int>>(l2.Value.Value);
        Assert.Equal(123, l3.Value);
    }

    // Compiler's no-box `where T : struct` form: documents observed shape and
    // confirms the IUnion.Value codec path still round-trips correctly.

    [Fact]
    public void ResultV2_Compiler_TryGetValue_Status_DocumentsActualBehavior()
    {
        var t = typeof(ResultV2<int>);
        var tryGets = t.GetMethods(System.Reflection.BindingFlags.Public
                                   | System.Reflection.BindingFlags.Instance)
            .Where(m => m.Name == "TryGetValue")
            .ToList();
        Assert.True(tryGets.Count is 0 or 2,
            $"Expected TryGetValue overload count to be 0 (current preview) or 2 (future optimization), actual={tryGets.Count}");
    }

    [Fact]
    public void ResultV2_Ok_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new ResultV2<int>(new OkV2<int>(7));
        var copy = s.Deserialize<ResultV2<int>>(s.SerializeToArray(original));
        var v = Assert.IsType<OkV2<int>>(copy.Value);
        Assert.Equal(7, v.Value);
    }

    [Fact]
    public void ResultV2_Err_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new ResultV2<int>(new ErrV2("boom"));
        var copy = s.Deserialize<ResultV2<int>>(s.SerializeToArray(original));
        var v = Assert.IsType<ErrV2>(copy.Value);
        Assert.Equal("boom", v.Message);
    }

    [Fact]
    public void Default_ResultV2_PreservesNullValue()
    {
        var s = BuildSerializer();
        ResultV2<int> original = default;
        var copy = s.Deserialize<ResultV2<int>>(s.SerializeToArray(original));
        Assert.Null(copy.Value);
    }

    [Fact]
    public void ResultV2_DeepCopy_RoundTrip()
    {
        var services = new ServiceCollection();
        services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
        var sp = services.BuildServiceProvider();
        var copier = sp.GetRequiredService<DeepCopier<ResultV2<int>>>();

        var original = new ResultV2<int>(new OkV2<int>(15));
        var copy = copier.Copy(original);
        Assert.Equal(15, ((OkV2<int>)copy.Value!).Value);
    }

    // Forward-compatibility: writing tag=0 for null inner value lands in the
    // generated tag switch's default branch -> ConsumeUnknownField -> default
    // result. The round-trip tests above already cover that behavior; this
    // structural assertion locks in the generated code shape.

    [Fact]
    public void Generated_Codec_Source_Has_Forward_Compat_Default_Branch()
    {
        var generatedRoot = LocateGeneratedRoot();
        var files = Directory.GetFiles(generatedRoot, "*.UnionOrleansSerializer.g.cs");
        Assert.NotEmpty(files);
        foreach (var f in files)
        {
            var text = File.ReadAllText(f);
            Assert.Contains("ConsumeUnknownField", text);
            var defaultCount = System.Text.RegularExpressions.Regex.Matches(text, @"\bdefault:").Count;
            Assert.True(defaultCount >= 2, $"{Path.GetFileName(f)} default: count={defaultCount}, expected >= 2");
        }
    }

    [Fact]
    public void Generated_Codec_Source_Covers_All_Sample_Unions()
    {
        var generatedRoot = LocateGeneratedRoot();
        var names = Directory.GetFiles(generatedRoot, "*.UnionOrleansSerializer.g.cs")
            .Select(Path.GetFileName)
            .ToList();
        Assert.Contains(names, n => n!.Contains("IdUnion"));
        Assert.Contains(names, n => n!.Contains("Status"));
        Assert.Contains(names, n => n!.Contains("Pet"));
        Assert.Contains(names, n => n!.Contains("RefUnion"));
        Assert.Contains(names, n => n!.Contains("Result_") || n!.Contains("Result."));
        Assert.Contains(names, n => n!.Contains("ResultV2"));
        Assert.Contains(names, n => n!.Contains("Option"));
        Assert.Contains(names, n => n!.Contains("Either"));
        Assert.Contains(names, n => n!.Contains("Pair"));
        Assert.Contains(names, n => n!.Contains("Triple"));
        Assert.Contains(names, n => n!.Contains("ConstrainedEither"));
    }

    // ── Multi-generic-parameter unions ────────────────────────

    // Scenario 1: 2-arity union, each case uses one parameter independently.
    [Fact]
    public void Either_IntString_Left_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Either<int, string>(new Left<int>(7));
        var copy = s.Deserialize<Either<int, string>>(s.SerializeToArray(original));
        Assert.IsType<Left<int>>(copy.Value);
        Assert.Equal(7, ((Left<int>)copy.Value!).Value);
    }

    [Fact]
    public void Either_IntString_Right_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Either<int, string>(new Right<string>("hello"));
        var copy = s.Deserialize<Either<int, string>>(s.SerializeToArray(original));
        Assert.IsType<Right<string>>(copy.Value);
        Assert.Equal("hello", ((Right<string>)copy.Value!).Value);
    }

    // Scenario 6: value-type vs reference-type type arguments.
    [Fact]
    public void Either_RefRef_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Either<RefA, RefB>(new Right<RefB>(new RefB(7)));
        var copy = s.Deserialize<Either<RefA, RefB>>(s.SerializeToArray(original));
        Assert.IsType<Right<RefB>>(copy.Value);
        Assert.Equal(7, ((Right<RefB>)copy.Value!).Value.Code);
    }

    // Scenario 5: same open generic union closed multiple ways in one
    // serializer instance — manifest provider must resolve every closed form.
    [Fact]
    public void Either_MultipleClosedForms_Coexist()
    {
        var s = BuildSerializer();

        var a = new Either<int, string>(new Left<int>(1));
        var b = new Either<Guid, RefA>(new Right<RefA>(new RefA("zoe")));

        var copyA = s.Deserialize<Either<int, string>>(s.SerializeToArray(a));
        var copyB = s.Deserialize<Either<Guid, RefA>>(s.SerializeToArray(b));

        Assert.Equal(1, ((Left<int>)copyA.Value!).Value);
        Assert.Equal("zoe", ((Right<RefA>)copyB.Value!).Value.Name);
    }

    // Scenario 4: nested generic union inside another generic union.
    [Fact]
    public void Either_NestedEither_RoundTrip()
    {
        var s = BuildSerializer();
        var inner = new Either<string, double>(new Right<double>(3.14));
        var original = new Either<int, Either<string, double>>(
            new Right<Either<string, double>>(inner));
        var copy = s.Deserialize<Either<int, Either<string, double>>>(
            s.SerializeToArray(original));
        var outerRight = Assert.IsType<Right<Either<string, double>>>(copy.Value);
        var innerRight = Assert.IsType<Right<double>>(outerRight.Value.Value);
        Assert.Equal(3.14, innerRight.Value);
    }

    // Scenario 2: 2-arity union, a single case uses both parameters at once.
    [Fact]
    public void Pair_Both_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Pair<int, string>(new Both<int, string>(7, "hi"));
        var copy = s.Deserialize<Pair<int, string>>(s.SerializeToArray(original));
        var both = Assert.IsType<Both<int, string>>(copy.Value);
        Assert.Equal(7, both.First);
        Assert.Equal("hi", both.Second);
    }

    [Fact]
    public void Pair_Empty_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Pair<int, string>(new Empty());
        var copy = s.Deserialize<Pair<int, string>>(s.SerializeToArray(original));
        Assert.IsType<Empty>(copy.Value);
    }

    // Scenario 3: 3-arity union — each case targets a different parameter.
    [Fact]
    public void Triple_One_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Triple<int, string, Guid>(new One<int>(11));
        var copy = s.Deserialize<Triple<int, string, Guid>>(s.SerializeToArray(original));
        Assert.Equal(11, ((One<int>)copy.Value!).Value);
    }

    [Fact]
    public void Triple_Two_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Triple<int, string, Guid>(new Two<string>("mid"));
        var copy = s.Deserialize<Triple<int, string, Guid>>(s.SerializeToArray(original));
        Assert.Equal("mid", ((Two<string>)copy.Value!).Value);
    }

    [Fact]
    public void Triple_Three_RoundTrip()
    {
        var s = BuildSerializer();
        var id = Guid.NewGuid();
        var original = new Triple<int, string, Guid>(new Three<Guid>(id));
        var copy = s.Deserialize<Triple<int, string, Guid>>(s.SerializeToArray(original));
        Assert.Equal(id, ((Three<Guid>)copy.Value!).Value);
    }

    [Fact]
    public void Triple_DeepCopy_PreservesCase()
    {
        var services = new ServiceCollection();
        services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
        var copier = services.BuildServiceProvider()
            .GetRequiredService<DeepCopier<Triple<int, string, Guid>>>();

        var original = new Triple<int, string, Guid>(new Two<string>("clone"));
        var copy = copier.Copy(original);
        Assert.Equal("clone", ((Two<string>)copy.Value!).Value);
    }

    // Scenario 7: generic constraints on the union itself must flow through
    // to all generated codec / copier types without producing CS errors.
    [Fact]
    public void ConstrainedEither_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new ConstrainedEither<int, string>(new Right<string>("ok"));
        var copy = s.Deserialize<ConstrainedEither<int, string>>(s.SerializeToArray(original));
        Assert.Equal("ok", ((Right<string>)copy.Value!).Value);
    }

    // ── Null reference-type payload edges (multi-generic) ─────
    //
    // Distinct from `Default_Union_PreservesNullValue`: here `IUnion.Value`
    // is a real case instance, but a reference-type field *inside* the case
    // is null. The codec must serialize/deserialize that null without
    // collapsing the whole union to default and without throwing NRE.

    [Fact]
    public void Either_RightRef_NullPayloadField_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Either<int, RefA>(new Right<RefA>(null!));
        var copy = s.Deserialize<Either<int, RefA>>(s.SerializeToArray(original));
        var right = Assert.IsType<Right<RefA>>(copy.Value);
        Assert.Null(right.Value);
    }

    [Fact]
    public void Either_LeftRef_NullPayloadField_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Either<RefA, RefB>(new Left<RefA>(null!));
        var copy = s.Deserialize<Either<RefA, RefB>>(s.SerializeToArray(original));
        var left = Assert.IsType<Left<RefA>>(copy.Value);
        Assert.Null(left.Value);
    }

    [Fact]
    public void Pair_BothRef_BothFieldsNull_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Pair<RefA, RefB>(new Both<RefA, RefB>(null!, null!));
        var copy = s.Deserialize<Pair<RefA, RefB>>(s.SerializeToArray(original));
        var both = Assert.IsType<Both<RefA, RefB>>(copy.Value);
        Assert.Null(both.First);
        Assert.Null(both.Second);
    }

    [Fact]
    public void Pair_BothRef_PartialNull_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Pair<RefA, RefB>(
            new Both<RefA, RefB>(new RefA("only-left"), null!));
        var copy = s.Deserialize<Pair<RefA, RefB>>(s.SerializeToArray(original));
        var both = Assert.IsType<Both<RefA, RefB>>(copy.Value);
        Assert.NotNull(both.First);
        Assert.Equal("only-left", both.First!.Name);
        Assert.Null(both.Second);
    }

    [Fact]
    public void Triple_RefArm_NullPayloadField_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Triple<RefA, RefB, string>(new Two<RefB>(null!));
        var copy = s.Deserialize<Triple<RefA, RefB, string>>(s.SerializeToArray(original));
        var two = Assert.IsType<Two<RefB>>(copy.Value);
        Assert.Null(two.Value);
    }

    [Fact]
    public void Either_Nested_InnermostRefIsNull_RoundTrip()
    {
        var s = BuildSerializer();
        var inner = new Either<string, RefA>(new Right<RefA>(null!));
        var original = new Either<int, Either<string, RefA>>(
            new Right<Either<string, RefA>>(inner));
        var copy = s.Deserialize<Either<int, Either<string, RefA>>>(
            s.SerializeToArray(original));
        var outerRight = Assert.IsType<Right<Either<string, RefA>>>(copy.Value);
        var innerRight = Assert.IsType<Right<RefA>>(outerRight.Value.Value);
        Assert.Null(innerRight.Value);
    }

    [Fact]
    public void Pair_BothFieldsNull_DeepCopy_PreservesNulls()
    {
        var services = new ServiceCollection();
        services.AddSerializer(b => b.AddAssembly(typeof(IdUnion).Assembly));
        var copier = services.BuildServiceProvider()
            .GetRequiredService<DeepCopier<Pair<RefA, RefB>>>();

        var original = new Pair<RefA, RefB>(new Both<RefA, RefB>(null!, null!));
        var copy = copier.Copy(original);
        var both = Assert.IsType<Both<RefA, RefB>>(copy.Value);
        Assert.Null(both.First);
        Assert.Null(both.Second);
    }

    // ── Nullable type-argument unions ────────────────────────────────
    //
    // These cover `Either<int?, string>` / `Either<string?, RefA?>` etc.
    // The semantics under test are:
    //   - The CLOSED generic form `Either<Nullable<int>, string>` must be
    //     resolvable through the manifest provider (the generator emits
    //     an open generic codec; Orleans constructs the closed one).
    //   - A `Left<int?>(null)` payload must round-trip with HasValue=false,
    //     not collapse to the entire union being default.
    //   - For reference-type T, `Left<string?>(null)` must round-trip the
    //     null payload while keeping the Left arm selected.

    [Fact]
    public void Either_NullableValue_LeftWithValue_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Either<int?, string>(new Left<int?>(7));
        var copy = s.Deserialize<Either<int?, string>>(s.SerializeToArray(original));
        var left = Assert.IsType<Left<int?>>(copy.Value);
        Assert.True(left.Value.HasValue);
        Assert.Equal(7, left.Value!.Value);
    }

    [Fact]
    public void Either_NullableValue_LeftWithNull_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Either<int?, string>(new Left<int?>(null));
        var copy = s.Deserialize<Either<int?, string>>(s.SerializeToArray(original));
        var left = Assert.IsType<Left<int?>>(copy.Value);
        Assert.False(left.Value.HasValue);
    }

    [Fact]
    public void Either_NullableValue_RightStillSelectable_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Either<int?, string>(new Right<string>("hi"));
        var copy = s.Deserialize<Either<int?, string>>(s.SerializeToArray(original));
        var right = Assert.IsType<Right<string>>(copy.Value);
        Assert.Equal("hi", right.Value);
    }

    [Fact]
    public void Pair_NullableValueBoth_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Pair<int?, int?>(new Both<int?, int?>(null, 5));
        var copy = s.Deserialize<Pair<int?, int?>>(s.SerializeToArray(original));
        var both = Assert.IsType<Both<int?, int?>>(copy.Value);
        Assert.False(both.First.HasValue);
        Assert.True(both.Second.HasValue);
        Assert.Equal(5, both.Second!.Value);
    }

    [Fact]
    public void Triple_NullableValueArm_RoundTrip()
    {
        var s = BuildSerializer();
        var original = new Triple<int?, string, System.Guid>(new One<int?>(null));
        var copy = s.Deserialize<Triple<int?, string, System.Guid>>(s.SerializeToArray(original));
        var one = Assert.IsType<One<int?>>(copy.Value);
        Assert.False(one.Value.HasValue);
    }

    [Fact]
    public void Either_NullableValueNested_RoundTrip()
    {
        // Nullable T flowing through a NESTED closed generic — exercises
        // the manifest provider building Either<int, Either<int?, string>>.
        var s = BuildSerializer();
        var inner = new Either<int?, string>(new Left<int?>(null));
        var original = new Either<int, Either<int?, string>>(
            new Right<Either<int?, string>>(inner));
        var copy = s.Deserialize<Either<int, Either<int?, string>>>(
            s.SerializeToArray(original));
        var outer = Assert.IsType<Right<Either<int?, string>>>(copy.Value);
        var innerLeft = Assert.IsType<Left<int?>>(outer.Value.Value);
        Assert.False(innerLeft.Value.HasValue);
    }

    // ── Potential real issue: reference identity through union dispatch ──
    //
    // Orleans enables reference tracking by default for reference-typed
    // payloads. If a single RefA instance is referenced TWICE inside a
    // List<RefUnion>, the deserialized list should also share the instance
    // (ReferenceEquals == true). The risk: the generator's Serialize path
    // dispatches via ((IUnion)value).Value and writes the case's IFieldCodec
    // directly — if that bypasses the per-reference bookkeeping, the second
    // occurrence would deserialize as a *new* RefA instance.
    //
    // This test pins the actual behavior so any future generator change
    // that breaks identity preservation surfaces immediately.

    [Fact]
    public void RefUnion_SameInstanceUsedTwice_ReferenceIdentityPreserved()
    {
        var s = BuildSerializer();
        var shared = new RefA("shared");
        var list = new List<RefUnion>
        {
            new RefUnion(shared),
            new RefUnion(shared),
        };

        var copy = s.Deserialize<List<RefUnion>>(s.SerializeToArray(list));
        var a0 = Assert.IsType<RefA>(copy[0].Value);
        var a1 = Assert.IsType<RefA>(copy[1].Value);
        Assert.Equal("shared", a0.Name);
        Assert.Equal("shared", a1.Name);

        // Document & enforce reference identity behavior across the union path.
        Assert.Same(a0, a1);
    }

    [Fact]
    public void EitherIntRef_SameInstanceUsedTwice_ReferenceIdentityPreserved()
    {
        var s = BuildSerializer();
        var shared = new RefA("shared");
        var list = new List<Either<int, RefA>>
        {
            new Either<int, RefA>(new Right<RefA>(shared)),
            new Either<int, RefA>(new Right<RefA>(shared)),
        };

        var copy = s.Deserialize<List<Either<int, RefA>>>(s.SerializeToArray(list));
        var r0 = Assert.IsType<Right<RefA>>(copy[0].Value);
        var r1 = Assert.IsType<Right<RefA>>(copy[1].Value);
        Assert.NotNull(r0.Value);
        Assert.NotNull(r1.Value);
        Assert.Same(r0.Value, r1.Value);
    }

    private static string LocateGeneratedRoot()
    {
        // Test dll lives at tests/YayaInk.Orleans.Unions.Tests/bin/<Cfg>/net11.0/.
        // Walk upward to locate the sample project, then read its
        // EmitCompilerGeneratedFiles output.
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 12 && !string.IsNullOrEmpty(dir); i++)
        {
            var sibling = Path.Combine(dir, "samples", "YayaInk.Orleans.Unions.Sample");
            if (!Directory.Exists(sibling))
                sibling = Path.Combine(dir, "YayaInk.Orleans.Unions.Sample");

            if (Directory.Exists(sibling))
            {
                foreach (var cfg in new[] { "Debug", "Release" })
                {
                    var generated = Path.Combine(sibling, "obj", cfg, "net11.0", "generated",
                        "YayaInk.Orleans.Unions.Generators",
                        "YayaInk.Orleans.Unions.Generators.UnionSerializerGenerator");
                    if (Directory.Exists(generated)) return generated;
                }
            }
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new DirectoryNotFoundException(
            "Failed to locate YayaInk.Orleans.Unions.Sample generator output directory");
    }
}
