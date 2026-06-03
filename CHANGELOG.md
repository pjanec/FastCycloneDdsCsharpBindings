# Changelog

All notable changes to CycloneDDS.NET will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## unreleased

### Fixed
- **Code-generation stamp file placed outside the generated folder.** A missing path
  separator wrote the incremental stamp as a sibling of the generated directory
  (`...CycloneDdsGeneratedcodegen.stamp`) instead of inside it. When the folder was cleaned
  but the orphaned stamp survived, the next build skipped code generation yet compiled with no
  generated files — producing an assembly missing its `[assembly: DdsIdlMapping]` metadata.
  Downstream projects could then no longer emit the matching `#include` and `idlc` failed with
  *"Scoped name '…' cannot be resolved"*. The stamp now lives inside the generated directory,
  so `Clean` (and any wipe of the folder) removes it together and forces a correct regen.
- **Generated `.idl` not copied to output on a clean build.** The output step ran (via
  `AfterTargets`) before generation completed, so on a clean build the `.idl` never reached
  `bin`; a referencing project's `idlc` then failed with *"Can't open include file"*. The copy
  is now performed deterministically inside the generation target, immediately after the files
  are produced.
- **Inconsistent `#include` emission.** The IDL type name and its `#include` were resolved by
  two independent paths that could disagree, silently producing an un-includable IDL. They now
  share one resolution path, and the generator fails fast with an actionable message (naming the
  type, field, and likely cause) instead of letting `idlc` surface a cryptic error later.

### Changed
- **Code generation now runs only where it's needed.** Previously the build targets flowed to
  every project in the transitive reference closure and ran the (Roslyn-based) generator
  unconditionally — dozens of needless runs. Generation is now gated to projects that (a) have a
  **direct** `PackageReference` to CycloneDDS.NET and (b) actually declare DDS types or enums
  (a fast in-process source scan, no extra process launch). A project that authors DDS types but
  references the package only transitively can opt in with
  `<CycloneDdsCodeGenEnabled>true</CycloneDdsCodeGenEnabled>`.
- **Unified the MSBuild integration into a single source of truth.** The packaged
  (`buildTransitive`) targets and the in-repo targets were duplicated; they are now one file,
  with environment differences expressed as overridable properties.

### Added
- `build/CycloneDDS.NET.props` (imported only for direct package references) and the
  `CycloneDdsCodeGenEnabled` opt-in property.

## 0.2.2
- Partitions
- WaitSets
- Removed Newtonsoft.Json reference
- Incremental code generation. File delta tracking to limit unnecessary disk writes and Roslyn intellisense thrashing.
- Union fix (critical structural alignment bug causing misaligned DDS union payloads.)
- Added explicit translation fallback logic for type identity boundaries across foreign assembly contexts.
- Added support for:
   - sparse/non-contiguous enums via `@value` annotations,
   - enum wire sizing bounds (`@bit_bound`),
   - C# `[InlineArray]` memory patterns,
   - and explicit generation toggles (`CycloneDdsDisableCodeGen`).
- DdsMonitor tool (blazor based UI trafic monitor)
   - Multi-Participant Configuration
   - Dynamic Topic Discovery from .net assemblies
   - Topic explorer
   - Sample list & sample detail windows linked together
   - Data Grid Customization
   - Filtering and Sorting
   - Recording & replay
   - Sample authoring & sending
   - Sender tracking
   - Headless mode (CLI tool)
   - Traffic stats


### 4. CycloneDDS Code Generator Enhancements

* **Schema Extensions:** 

## 0.1.25

### Added
- Initial public release of CycloneDDS.NET
- Zero-allocation write path with custom marshaller
- Zero-copy read path using `ref struct` views
- Code-first schema DSL with attributes (`[DdsTopic]`, `[DdsKey]`, `[DdsStruct]`)
- Automatic IDL generation from C# types
- IDL import tool for converting existing IDL to C# DSL
- Async/await support with `WaitDataAsync`
- Client-side filtering with compiled predicates
- Sender tracking (Computer, PID, custom app ID)
- NuGet package with bundled native binaries and build tools
- Automatic C# code generation during build
- Support for keyed topics with O(1) instance lookup
- Full interoperability with other DDS implementations

### Changed

### Deprecated

### Removed

### Fixed

### Security

---

## Release Notes

### Version Numbering

This project uses [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) for automatic semantic versioning based on git tags and commit history.

### How to Read This Changelog

- **Added** - New features
- **Changed** - Changes in existing functionality
- **Deprecated** - Soon-to-be removed features
- **Removed** - Removed features
- **Fixed** - Bug fixes
- **Security** - Vulnerability fixes

[Unreleased]: https://github.com/pjanec/CycloneDds.NET/compare/HEAD...HEAD
