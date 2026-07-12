# Release Guide

This document provides step-by-step instructions for releasing CycloneDDS.NET to NuGet.org.

## Prerequisites

Before you begin, ensure you have:

1. **Maintainer Access** to the GitHub repository
2. **NuGet API Key** with push permissions for the CycloneDDS.NET package
3. **Development Environment** with:
   - **.NET 10 SDK** — the code uses C# 13 language features, so building requires it; the produced binaries still target `net8.0`. (The .NET 8 runtime is also handy for running the `net8.0` tests.)
   - **CMake 3.16+**
   - **Windows:** PowerShell and Visual Studio 2022 (*C++ Desktop Development* workload) for native compilation.
   - **Linux:** a C toolchain and `patchelf` — `sudo apt-get install -y cmake build-essential patchelf`.
   - Git command-line tools

## Building & Testing a Multi-Platform Package Locally

The published `CycloneDDS.NET` and `CycloneDDS.NET.DdsMonitor` packages ship native
assets for **both** `win-x64` and `linux-x64`. The pack step includes whatever
native artifacts are present under `artifacts/native/<rid>/` (Windows-only entries
are `Exists`-guarded), so a **cross-platform** package is produced by making *both*
platforms' natives available on one pack host.

A single machine can only compile one platform's native libraries, so bring the
other platform's from CI — the workflow publishes a `native-linux-x64` artifact
(and you can build `win-x64` locally) — or from WSL/Docker. Because the packages
bundle the Windows apphost `.exe` launchers for the build-time tools, **the final
cross-platform pack must run on Windows**. Packing on Linux is fully supported but
yields a `linux-x64`-only package (handy for validating the Linux side).

### A. Cross-platform package on a Windows box

```powershell
# 1. Build the Windows native, and bring in the Linux native from a green CI run on main
.\build\native-win.ps1
gh run download --repo pjanec/CycloneDds.NET -n native-linux-x64 -D artifacts\native\linux-x64

# 2. Build, test and pack -> artifacts\nuget\  (both packages, both platforms' natives)
.\build\pack.ps1
```

### B. Linux-only package on a Linux box

```bash
build/native-linux.sh Release
dotnet build CycloneDDS.NET.Core.slnf -c Release
dotnet pack src/CycloneDDS.Runtime/CycloneDDS.Runtime.csproj     -c Release --no-build -o artifacts/nuget
dotnet pack tools/DdsMonitor/DdsMonitor.Blazor/DdsMonitor.csproj -c Release --no-build -o artifacts/nuget
```

### Smoke-testing the packages

`examples/PackageSmokeTest` consumes `CycloneDDS.NET` as a real NuGet **package**
from the local feed (`artifacts/nuget`, set in its `nuget.config`). It declares a
`[DdsTopic]` (so the packaged code generator + `idlc` run) and does a
publish/subscribe round-trip. Run it against the exact packed version:

```bash
# Linux / bash
VER=$(ls artifacts/nuget/CycloneDDS.NET.[0-9]*.nupkg | head -1 | sed -E 's|.*/CycloneDDS\.NET\.(.+)\.nupkg|\1|')
dotnet run --project examples/PackageSmokeTest -c Release -p:SmokePkgVersion=$VER
# Expect:  [smoke] PASS: round-trip Id=42 Value=3.14159 Label=hello-cyclonedds
```

```powershell
# Windows / PowerShell
$pkg = Get-ChildItem artifacts\nuget\CycloneDDS.NET.[0-9]*.nupkg | Select-Object -First 1
$ver = $pkg.Name -replace '^CycloneDDS\.NET\.(.+)\.nupkg$','$1'
dotnet run --project examples\PackageSmokeTest -c Release -p:SmokePkgVersion=$ver
```

Smoke-test the DdsMonitor global tool:

```bash
dotnet tool install --global --add-source artifacts/nuget --version <VER> CycloneDDS.NET.DdsMonitor
ddsmonitor --NoBrowser true    # logs "Application started" and serves http://127.0.0.1:<port>/
dotnet tool uninstall --global CycloneDDS.NET.DdsMonitor
```

Run the smoke test on **both** a Windows and a Linux box before publishing (copy a
cross-platform package across, or do a per-platform local build on each). Both were
validated during development — Linux locally + in CI, Windows in the CI `build` job.

## Version Management

This project uses **Nerdbank.GitVersioning (NBGV)** for automatic semantic versioning.

### Understanding NBGV Versioning

- **Automatic Version Calculation:** NBGV determines the version based on:
  - Git tags (e.g., `v1.0.0`)
  - Git commit height since the last tag
  - Branch name
  
- **Version Format:** `{Major}.{Minor}.{Patch}-{Prerelease}+{GitCommitId}`
  - Example: `1.0.5-alpha.3+g7e041a2787`

### Version Configuration

Version settings are stored in `version.json`:
```json
{
  "version": "1.0",
  "publicReleaseRefSpec": [
    "^refs/heads/main$",
    "^refs/tags/v\\d+\\.\\d+"
  ]
}
```

## Release Types

### 1. Pre-Release (Alpha/Beta)

Pre-releases are automatically created by CI for every commit to `main`.

**Process:**
1. Merge your PR to `main`
2. CI automatically builds and publishes (if configured)
3. Package version: `1.0.x-alpha+{commitId}`

**Manual Pre-Release:**
```powershell
# Build the package
.\build\pack.ps1

# Packages will be in artifacts/nuget/
# Example: CycloneDDS.NET.1.0.5-alpha-g7e041a2787.nupkg
```

### 2. Stable Release

Stable releases are created by tagging a commit.

**Process:**

#### Step 1: Prepare the Release

1. **Update CHANGELOG.md:**
   ```markdown
   ## [1.0.0] - 2026-02-14
   
   ### Added
   - Feature descriptions
   
   ### Fixed
   - Bug fixes
   ```

2. **Commit the changelog:**
   ```bash
   git add CHANGELOG.md
   git commit -m "Prepare release v1.0.0"
   git push origin main
   ```

#### Step 2: Create a Git Tag

1. **Tag the release commit:**
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **Verify the tag:**
   ```bash
   git describe --tags
   # Should output: v1.0.0
   ```

#### Step 3: Build and Verify

The CI pipeline should automatically build the tagged version.

**To build locally:**
```powershell
# Clean previous builds
Remove-Item -Recurse -Force artifacts/ -ErrorAction SilentlyContinue

# Build the package
.\build\pack.ps1

# Verify the version
# Check artifacts/nuget/ for CycloneDDS.NET.1.0.0.nupkg (no prerelease suffix)
```

#### Step 4: Inspect the Package

Before publishing, inspect the package contents:

```powershell
# Option 1: Use NuGet Package Explorer (GUI)
# Download from: https://github.com/NuGetPackageExplorer/NuGetPackageExplorer

# Option 2: Extract and inspect manually
Expand-Archive artifacts/nuget/CycloneDDS.NET.1.0.0.nupkg -DestinationPath temp_inspect

# Verify contents:
# - icon.png is present
# - README.md is present
# - ThirdPartyNotices.txt is present
# - Native binaries are in runtimes/ folder
# - Build tools are in build/ or buildTransitive/ folder
```

**Key Things to Check:**
- [ ] Package version is correct (no `-alpha` suffix for stable)
- [ ] `icon.png` is at package root
- [ ] `README.md` is at package root
- [ ] `ThirdPartyNotices.txt` is at package root
- [ ] Native libraries: `runtimes/win-x64/native/ddsc.dll`
- [ ] Build tools: `tools/` or `build/` folder with code generators
- [ ] Symbol package (`.snupkg`) is generated

## Publishing to NuGet.org

### Option A: Automatic Publishing (CI/CD)

If CI is configured to auto-publish on tags:

1. **Wait for CI to complete** after pushing the tag
2. **Verify on NuGet.org** that the new version appears
3. **Create a GitHub Release** with release notes

### Option B: Manual Publishing

#### Step 1: Set Up NuGet API Key

**One-time setup:**
```powershell
# Get your API key from: https://www.nuget.org/account/apikeys
# Create a key with "Push new packages and package versions" permission

# Store the key (safer than using it directly in commands)
$apiKey = "your-api-key-here"

# Or use the dotnet tool to store it:
dotnet nuget add source https://api.nuget.org/v3/index.json `
    --name nuget.org `
    --api-key $apiKey
```

**Security Best Practice:** Store API keys as GitHub Secrets for CI, never commit them to the repository.

#### Step 2: Push the Package

```powershell
# Navigate to the package directory
cd artifacts/nuget

# Push the main package
dotnet nuget push CycloneDDS.NET.1.0.0.nupkg `
    --source https://api.nuget.org/v3/index.json `
    --api-key $apiKey

# Push the symbol package
dotnet nuget push CycloneDDS.NET.1.0.0.snupkg `
    --source https://api.nuget.org/v3/index.json `
    --api-key $apiKey
```

#### Step 3: Verify the Release

1. **Check NuGet.org:** https://www.nuget.org/packages/CycloneDDS.NET/
   - Verify the new version appears
   - Check that the icon displays correctly
   - Verify the README is visible on the package page

2. **Test Installation:**
   ```bash
   mkdir test-install
   cd test-install
   dotnet new console
   dotnet add package CycloneDDS.NET --version 1.0.0
   dotnet build
   ```

3. **Verify Package Contents:**
   - Native libraries are copied to output
   - Code generation works during build

## Post-Release Tasks

### 1. Create GitHub Release

1. Go to: https://github.com/pjanec/CycloneDds.NET/releases/new
2. Select the tag: `v1.0.0`
3. Release title: `CycloneDDS.NET v1.0.0`
4. Description: Copy relevant sections from `CHANGELOG.md`
5. Attach build artifacts (optional):
   - `CycloneDDS.NET.1.0.0.nupkg`
   - `CycloneDDS.NET.1.0.0.snupkg`
6. Click "Publish release"

### 2. Update Documentation

- Update README badges if necessary
- Announce the release (social media, mailing lists, etc.)
- Close any related milestone on GitHub

### 3. Prepare for Next Release

Update `CHANGELOG.md` with a new `[Unreleased]` section:
```markdown
## [Unreleased]

### Added

### Changed

### Fixed
```

## Troubleshooting

### Version Not Incrementing

**Problem:** NBGV is not detecting the new tag.

**Solution:**
```bash
# Verify tags are correct
git tag -l

# Ensure you've pushed the tag
git push origin v1.0.0

# Check NBGV version calculation
nbgv get-version
```

### Package Already Exists

**Problem:** `409 Conflict - The package version already exists.`

**Solution:**
- NuGet.org does **not allow overwriting** published packages
- You must increment the version (even for bug fixes)
- Hotfix: Create a new patch version (e.g., `v1.0.1`)

### Missing Files in Package

**Problem:** `icon.png` or `README.md` not in the package.

**Solution:**
1. Verify `Directory.Build.props` has the `<None Include=...>` elements
2. Check file paths are correct (use `$(MSBuildThisFileDirectory)`)
3. Clean and rebuild:
   ```powershell
   Remove-Item -Recurse artifacts/
   .\build\pack.ps1
   ```

### Symbol Package Errors

**Problem:** Symbol package fails to upload.

**Solution:**
- Ensure PDB files are embedded or included
- Check `<IncludeSymbols>true</IncludeSymbols>` in `Directory.Build.props`
- Symbol packages are optional; main package can be published without them

## Reference Links

- **NuGet.org Account:** https://www.nuget.org/account
- **API Key Management:** https://www.nuget.org/account/apikeys
- **NBGV Documentation:** https://github.com/dotnet/Nerdbank.GitVersioning/blob/main/doc/nbgv-cli.md
- **NuGet Package Explorer:** https://github.com/NuGetPackageExplorer/NuGetPackageExplorer

## Emergency Rollback

If a release has critical issues:

1. **Unlist (deprecate) the package version** on NuGet.org:
   - Go to the package page -> Manage Package
   - Select the version and click "Unlist"
   - This hides it from search but existing dependencies still work

2. **Publish a hotfix version** immediately:
   - Create a fix branch
   - Tag a new version (e.g., `v1.0.1`)
   - Follow the release process

3. **Communicate the issue:**
   - Update the GitHub release with a warning
   - Post an issue describing the problem

---

**Last Updated:** February 2026  
**Maintained By:** CycloneDDS.NET Authors
