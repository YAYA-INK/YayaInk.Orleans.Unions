using System.Linq;
using System.Reflection;
using YayaInk.Orleans.Unions.Sample;

namespace YayaInk.Orleans.Unions.Tests;

/// <summary>
/// Detects which compiler-emitted shape the current Roslyn version uses for
/// <c>union</c> types. Today (VS 2026 18.7.0-insiders / .NET 11 preview) every
/// union — including <c>ResultV2&lt;T&gt; where T : struct</c> — is emitted in
/// the boxed single-<c>object Value</c> form. The no-box form described in the
/// C# 15 spec (a <c>byte _tag</c> + per-case typed fields + overloaded
/// <c>bool TryGetValue(out T)</c> methods) has not shipped yet.
///
/// <para>
/// These tests pin that observation. If the no-box form ever lands, the asserts
/// below will start failing — that is the signal to extend
/// <c>UnionSerializerGenerator</c> with a <c>TryGetValue</c>-based fast path so
/// value-case dispatch can avoid the boxing performed inside the union itself.
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
    public void Union_StillUsesBoxedSingleObjectFormToday(System.Type unionType)
    {
        // Every compiler-emitted union today has exactly one instance field —
        // the auto-property backing field for `object Value`.
        var instanceFields = unionType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.Single(instanceFields);
        Assert.Equal(typeof(object), instanceFields[0].FieldType);

        // And there is no `TryGetValue(out T)` fast-path overload yet.
        var tryGet = unionType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == "TryGetValue")
            .ToArray();

        Assert.Empty(tryGet);
    }

    [Fact]
    public void NoBoxFormHasNotLanded_ForAllStructCaseUnion()
    {
        // ResultV2<T> where T : struct is the spec's canonical "should be no-box"
        // example. The day this assertion flips, revisit UnionSerializerGenerator
        // and emit a TryGetValue<T>(out T) dispatch chain for such unions.
        var t = typeof(ResultV2<int>);

        var hasTag = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                      .Any(f => f.FieldType == typeof(byte));
        var hasTryGetValue = t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                              .Any(m => m.Name == "TryGetValue");

        Assert.False(hasTag,
            "Compiler started emitting a tag field — extend UnionSerializerGenerator with a TryGetValue fast path.");
        Assert.False(hasTryGetValue,
            "Compiler started emitting TryGetValue overloads — extend UnionSerializerGenerator with a TryGetValue fast path.");
    }
}
