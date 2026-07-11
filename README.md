# YayaInk.Orleans.Unions

[![ci](https://github.com/YAYA-INK/YayaInk.Orleans.Unions/actions/workflows/ci.yml/badge.svg)](https://github.com/YAYA-INK/YayaInk.Orleans.Unions/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/YayaInk.Orleans.Unions.svg?label=YayaInk.Orleans.Unions)](https://www.nuget.org/packages/YayaInk.Orleans.Unions)
[![NuGet](https://img.shields.io/nuget/vpre/YayaInk.Orleans.Unions.Generators.svg?label=YayaInk.Orleans.Unions.Generators)](https://www.nuget.org/packages/YayaInk.Orleans.Unions.Generators)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

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
  <PackageReference Include="YayaInk.Orleans.Unions" Version="0.1.0-preview.5" />
  <PackageReference Include="YayaInk.Orleans.Unions.Generators" Version="0.1.0-preview.5"
                    PrivateAssets="all" />
</ItemGroup>
```

## Usage

```csharp
using YayaInk.Orleans.Unions;

[Union]
[GenerateUnionSerializer]
public union Result<T>(Ok<T>, Err);

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

## Performance notes

The `union` keyword always lowers to a struct wrapper containing a single
`object? Value` field, regardless of the case types or generic constraints.
That is the spec's design, not a temporary state — `where T : struct` does
**not** trigger a different layout, and there is no compiler switch that
makes `union Foo(...)` emit the unboxed `byte _tag` + per-case typed fields
shape on its own.

What that means for this package:

- The generator dispatches via `((IUnion)value).Value`, which is the only
  shape the compiler produces for `union` declarations.
- For value-type cases, the boxing happens inside the union's own
  constructor (`new MyUnion(structValue)`); the serializer does **not** add
  another layer of boxing on top of that.
- `CompilerShapeProbeTests` simply pins this lowering as a regression
  guard; it is not a "waiting for the no-box form to land" probe.

The C# 15 spec **does** define a non-boxing access pattern, but only for
**hand-written `[Union] struct`** types that opt in by implementing
`HasValue` plus one or more `bool TryGetValue(out T)` overloads (see the
"Non-boxing access pattern" section of the spec). The compiler then routes
pattern matching through those typed accessors instead of `Value`.

This package currently targets the `union` keyword path only. Adding
generator support for hand-written `[Union] struct`s with `TryGetValue`
overloads is a possible future enhancement and is independent of any
compiler change.

## Limitations

- The C# `union` keyword is a **preview** language feature; you must enable
  `LangVersion=preview` and `EnablePreviewFeatures=true` in your project.
- The package currently requires .NET SDK `11.0.100-preview.5.26302.115`.
  This SDK supplies `System.Runtime.CompilerServices.IUnion` and
  `UnionAttribute`; consumers must not define or reference a polyfill for
  these runtime types.
- Custom (handwritten) union layouts are out of scope; only the
  compiler-generated default form is supported.

## License

MIT © YAYA-INK
