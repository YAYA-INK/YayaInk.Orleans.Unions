using System.Runtime.CompilerServices;
using YayaInk.Orleans.Unions;

namespace YayaInk.Orleans.Unions.Sample;

// ============================================================
// Demo union types — zero-boilerplate from the consumer's view:
// declare the union, attach [GenerateUnionSerializer], and the
// generator emits Orleans IFieldCodec / IDeepCopier for it.
//
// Covers non-generic and generic unions, ≥3 cases, record class
// cases, inheritance-overlap cases, and the compiler's no-box
// `where T : struct` form.
// ============================================================

[global::Orleans.GenerateSerializer]
public record struct Ok<T>([property: global::Orleans.Id(0)] T Value);

[global::Orleans.GenerateSerializer]
public record struct Err([property: global::Orleans.Id(0)] string Message);

[Union]
[GenerateUnionSerializer]
public union Result<T>(Ok<T>, Err);

[global::Orleans.GenerateSerializer]
public record struct Some<T>([property: global::Orleans.Id(0)] T Value);

[global::Orleans.GenerateSerializer]
public record struct None();

[Union]
[GenerateUnionSerializer]
public union Option<T>(Some<T>, None);

[global::Orleans.GenerateSerializer]
public record struct UserId([property: global::Orleans.Id(0)] int Value);

[global::Orleans.GenerateSerializer]
public record struct OrderId([property: global::Orleans.Id(0)] int Value);

[Union]
[GenerateUnionSerializer]
public union IdUnion(UserId, OrderId);

// Multi-case (≥3) union: exercises tag=3 write/read branches.

[global::Orleans.GenerateSerializer]
public record struct Pending();

[global::Orleans.GenerateSerializer]
public record struct Running([property: global::Orleans.Id(0)] int Progress);

[global::Orleans.GenerateSerializer]
public record struct Done([property: global::Orleans.Id(0)] string Result);

[Union]
[GenerateUnionSerializer]
public union Status(Pending, Running, Done);

// Reference-type cases (record class).

[global::Orleans.GenerateSerializer]
public record class RefA([property: global::Orleans.Id(0)] string Name);

[global::Orleans.GenerateSerializer]
public record class RefB([property: global::Orleans.Id(0)] int Code);

[Union]
[GenerateUnionSerializer]
public union RefUnion(RefA, RefB);

// Inheritance-overlap: derived case must come before base, the union's
// generated dispatch matches in declaration order.

[global::Orleans.GenerateSerializer]
public record class Animal([property: global::Orleans.Id(0)] string Name);

[global::Orleans.GenerateSerializer]
public record class Puppy(string Name) : Animal(Name);

[Union]
[GenerateUnionSerializer]
public union Pet(Puppy, Animal);

// Compiler's no-box form: `union ... where T : struct` emits typed field +
// tag + TryGetValue overloads. Our serializer still goes through IUnion.Value
// for correctness; this sample documents that both compiler shapes round-trip.

[global::Orleans.GenerateSerializer]
public record struct OkV2<T>([property: global::Orleans.Id(0)] T Value) where T : struct;

[global::Orleans.GenerateSerializer]
public record struct ErrV2([property: global::Orleans.Id(0)] string Message);

[Union]
[GenerateUnionSerializer]
public union ResultV2<T>(OkV2<T>, ErrV2) where T : struct;
