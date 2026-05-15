namespace YayaInk.Orleans.Unions;

/// <summary>
/// Marks a C# 15 <c>union</c> type so that the
/// <c>YayaInk.Orleans.Unions.Generators</c> source generator emits a
/// Microsoft Orleans <see cref="!:Orleans.Serialization.Codecs.IFieldCodec{T}"/> and
/// <see cref="!:Orleans.Serialization.Cloning.IDeepCopier{T}"/> for it, plus the
/// assembly-level <c>TypeManifestProvider</c> registration that
/// <c>services.AddSerializer(b =&gt; b.AddAssembly(...))</c> needs to discover them.
/// </summary>
/// <remarks>
/// <para>
/// Supported shapes (validated by the test suite):
/// </para>
/// <list type="bullet">
/// <item>Non-generic and generic unions.</item>
/// <item>Multiple cases (forward-compatible unknown-tag fallback).</item>
/// <item><c>record struct</c> and <c>record class</c> cases, including
/// inheritance-overlap cases.</item>
/// <item>Nested unions (a union as a case payload).</item>
/// <item>Default / null <c>IUnion.Value</c>.</item>
/// </list>
/// <para>
/// The target type must be the compiler-generated default form of a C# 15
/// <c>union</c>. Custom (handwritten) union layouts are out of scope. When the
/// generator cannot emit code, it reports diagnostics in the
/// <c>YayaInkOrleansUnions</c> category and skips the type.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateUnionSerializerAttribute : Attribute;
