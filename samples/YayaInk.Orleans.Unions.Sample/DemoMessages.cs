using System.Collections.Generic;

namespace YayaInk.Orleans.Unions.Sample;

// ============================================================
// Message-composition scenarios. Each outer type below carries
// at least one union field; Orleans' own source generator emits
// codecs for the outer type and delegates to the union codecs
// produced by YayaInk.Orleans.Unions.Generators.
//
//   1. Direct field:                CommandEnvelope.Id
//   2. List<T> element:             BatchEnvelope.Ids
//   3. Dictionary value:            IndexedEnvelope.Map
//   4. Nested message:              OuterEnvelope.Inner.Id
//   5. Multiple union fields:       MultiUnionEnvelope
//   6. Generic union field:         ResultEnvelope (Result<int>)
//   7. Reference-type case union:   RefEnvelope
//   8. Nullable union field:        NullableEnvelope (IdUnion?)
// ============================================================

[global::Orleans.GenerateSerializer]
public sealed record CommandEnvelope(
    [property: global::Orleans.Id(0)] string CorrelationId,
    [property: global::Orleans.Id(1)] IdUnion Id,
    [property: global::Orleans.Id(2)] long Timestamp);

[global::Orleans.GenerateSerializer]
public sealed record BatchEnvelope(
    [property: global::Orleans.Id(0)] string CorrelationId,
    [property: global::Orleans.Id(1)] List<IdUnion> Ids);

[global::Orleans.GenerateSerializer]
public sealed record IndexedEnvelope(
    [property: global::Orleans.Id(0)] Dictionary<string, IdUnion> Map);

[global::Orleans.GenerateSerializer]
public sealed record InnerEnvelope(
    [property: global::Orleans.Id(0)] IdUnion Id,
    [property: global::Orleans.Id(1)] string Note);

[global::Orleans.GenerateSerializer]
public sealed record OuterEnvelope(
    [property: global::Orleans.Id(0)] string Topic,
    [property: global::Orleans.Id(1)] InnerEnvelope Inner);

[global::Orleans.GenerateSerializer]
public sealed record MultiUnionEnvelope(
    [property: global::Orleans.Id(0)] IdUnion Id,
    [property: global::Orleans.Id(1)] Status Status,
    [property: global::Orleans.Id(2)] string Tag);

[global::Orleans.GenerateSerializer]
public sealed record ResultEnvelope(
    [property: global::Orleans.Id(0)] string CorrelationId,
    [property: global::Orleans.Id(1)] Result<int> Result);

[global::Orleans.GenerateSerializer]
public sealed record RefEnvelope(
    [property: global::Orleans.Id(0)] RefUnion Payload);

// Union is a struct; nullable form is IdUnion?.
[global::Orleans.GenerateSerializer]
public sealed record NullableEnvelope(
    [property: global::Orleans.Id(0)] IdUnion? MaybeId);
