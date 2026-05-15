# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
