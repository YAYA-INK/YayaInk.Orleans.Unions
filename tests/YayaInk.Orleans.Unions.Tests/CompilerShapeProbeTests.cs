using System.Linq;
using System.Reflection;
using YayaInk.Orleans.Unions.Sample;

namespace YayaInk.Orleans.Unions.Tests;

/// <summary>
/// Pins the compiler-emitted shape of <c>union</c> declarations so a future
/// Roslyn change cannot silently invalidate the generator's dispatch model.
///
/// <para>
/// Per the C# 15 spec, the <c>union</c> keyword always lowers to a struct
/// wrapper with a single <c>object? Value</c> field, regardless of the case
/// types or generic constraints. The non-boxing access pattern (<c>HasValue</c>
/// + <c>bool TryGetValue(out T)</c> overloads) is an opt-in pattern for
/// <em>hand-written</em> <c>[Union] struct</c> types — it is not something
/// the compiler ever auto-generates for a <c>union</c> declaration, not even
/// for <c>union ... where T : struct</c>.
/// </para>
///
/// <para>
/// These tests therefore exist as stability guards: if Roslyn ever changes
/// the lowering of <c>union</c> declarations, the generator's
/// <c>((IUnion)value).Value</c> dispatch needs to be revisited.
/// </para>
/// </summary>
public class CompilerShapeProbeTests
{
    [Theory]
    [InlineData(typeof(IdUnion))]
    [InlineData(typeof(Status))]
    [InlineData(typeof(RefUnion))]
    [InlineData(typeof(Pet))]
    [InlineData(typeof(ResultV2<int>))]
    [InlineData(typeof(Result<int>))]
    [InlineData(typeof(Option<int>))]
    [InlineData(typeof(Either<int, string>))]
    [InlineData(typeof(Pair<int, string>))]
    [InlineData(typeof(Triple<int, string, double>))]
    [InlineData(typeof(ConstrainedEither<int, string>))]
    public void UnionKeyword_LowersToSingleObjectValueField(System.Type unionType)
    {
        // Every `union`-declared type has exactly one instance field — the
        // auto-property backing field for `object Value`.
        var instanceFields = unionType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.Single(instanceFields);
        Assert.Equal(typeof(object), instanceFields[0].FieldType);

        // The non-boxing access pattern is opt-in for hand-written [Union]
        // structs only; the `union` keyword never emits TryGetValue.
        var tryGet = unionType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == "TryGetValue")
            .ToArray();

        Assert.Empty(tryGet);
    }

    [Fact]
    public void UnionKeyword_DoesNotEmitTagOrTryGetValue_EvenForAllStructCases()
    {
        // ResultV2<T> where T : struct is the canonical "all value-type cases"
        // example. Per the spec the `union` keyword still lowers to the
        // single-object-Value form here. If this assertion ever flips it means
        // Roslyn changed the lowering of `union` declarations themselves, in
        // which case UnionSerializerGenerator's dispatch needs to be revisited.
        var t = typeof(ResultV2<int>);

        var hasTag = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                      .Any(f => f.FieldType == typeof(byte));
        var hasTryGetValue = t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                              .Any(m => m.Name == "TryGetValue");

        Assert.False(hasTag,
            "`union` keyword started emitting a tag field — revisit UnionSerializerGenerator's dispatch model.");
        Assert.False(hasTryGetValue,
            "`union` keyword started emitting TryGetValue overloads — revisit UnionSerializerGenerator's dispatch model.");
    }
}
