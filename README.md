# YayaInk.Orleans.Unions

Zero-boilerplate [Microsoft Orleans](https://github.com/dotnet/orleans) serialization
support for **C# 15 union types** (the `union` declaration introduced as a preview
language feature).

Mark your union with `[GenerateUnionSerializer]` and the source generator emits a
real Orleans `IFieldCodec<T>` and `IDeepCopier<T>` for it, plus the
`TypeManifestProvider` registration that `services.AddSerializer(b => b.AddAssembly(...))`
needs to discover them.

> Status: preview (0.1.x). API and wire format may still change before 1.0.

## Packages

| Package | Purpose |
| --- | --- |
| `YayaInk.Orleans.Unions` | Runtime marker attribute (`GenerateUnionSerializerAttribute`). |
| `YayaInk.Orleans.Unions.Generators` | Roslyn source generator. Ships in `analyzers/dotnet/cs/`. |

Install both into the project that declares your union types:

```xml
<ItemGroup>
  <PackageReference Include="YayaInk.Orleans.Unions" Version="0.1.0-preview.1" />
  <PackageReference Include="YayaInk.Orleans.Unions.Generators" Version="0.1.0-preview.1"
                    PrivateAssets="all" />
</ItemGroup>
```

## Usage

```csharp
using YayaInk.Orleans.Unions;

[Union]
[GenerateUnionSerializer]
public partial union Result<T>(Ok<T>, Err);

[GenerateSerializer] public sealed record Ok<T>([property: Id(0)] T Value);
[GenerateSerializer] public sealed record Err([property: Id(0)] string Message);
```

Then register the assembly with Orleans as usual:

```csharp
services.AddSerializer(b => b.AddAssembly(typeof(Result<>).Assembly));
```

That's it — `Result<T>` will round-trip through Orleans codecs/copiers, work
across silos, and compose inside other `[GenerateSerializer]` types.

## Supported union shapes

Validated by the test suite:

- Non-generic and generic unions (any arity)
- Multiple cases (≥3) and forward-compatible unknown-tag fallback
- `record struct` and `record class` cases
- Inheritance-overlap cases (derived case stays derived after round-trip)
- Nested unions (a union as a case payload, including multi-level)
- Unions embedded in records, lists, dictionaries, and other Orleans messages
- `default` / null `IUnion.Value` preservation

## Limitations

- The C# `union` keyword is a **preview** language feature; you must enable
  `LangVersion=preview` and `EnablePreviewFeatures=true` in your project.
- This package does **not** ship a polyfill for
  `System.Runtime.CompilerServices.IUnion` / `UnionAttribute`. Until the BCL
  ships them, your project (or another package you already depend on) needs to
  provide these types. The `samples/` project demonstrates a minimal local
  polyfill.
- Custom (handwritten) union layouts are out of scope; only the
  compiler-generated default form is supported.

## License

MIT © YAYA-INK
