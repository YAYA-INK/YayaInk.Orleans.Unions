# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-preview.4] - 2026-06-19

### Fixed
- Fixed generated codec/copier name collisions for nested union types with
  identical short names, such as `TrackResource.Pose` and
  `GripperResource.Pose`.
- Included containing type chains and containing generic arity in generated
  source hint names and Orleans manifest registrations.

### Added
- Added regression coverage for same-short-name nested unions and nested
  unions inside generic containing types.

## [0.1.0-preview.3] - 2026-05-16

### Fixed
- Fixed package metadata and analyzer packaging for the preview.3 NuGet
  release.

## [0.1.0-preview.2] - 2026-05-16

### Fixed
- Fixed generated union serialization reference tracking by marking the union
  value field on the writer side to match the reader side.

### Added
- Added regression coverage for complex nested union payloads, deep copies,
  and unions embedded as fields of `[GenerateSerializer]` records.

## [0.1.0-preview.1]

### Added
- Initial extraction of `UnionOrleansLab` into the standalone
  `YayaInk.Orleans.Unions` repository.
- `YayaInk.Orleans.Unions` runtime package with
  `GenerateUnionSerializerAttribute`.
- `YayaInk.Orleans.Unions.Generators` source generator package emitting
  Orleans `IFieldCodec<T>` / `IDeepCopier<T>` and
  `TypeManifestProvider` registrations.
- Sample project demonstrating union usage and Orleans round-trip.
- Test suites for serializer-level round-trip, cross-silo cluster behavior
  via `Microsoft.Orleans.TestingHost`, and message-composition scenarios.
