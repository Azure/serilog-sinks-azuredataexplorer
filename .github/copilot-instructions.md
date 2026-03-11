# Copilot Instructions — Serilog.Sinks.AzureDataExplorer

## Project Overview

This is a Serilog sink that ships structured logs to Azure Data Explorer (Kusto). It is a NuGet library owned by Microsoft.

## Architecture

- **Main library**: `src/Serilog.Sinks.AzureDataExplorer/` — multi-targeted (net9.0, net8.0, net6.0, netstandard2.0, net471, net462)
- **Tests**: `src/Serilog.Sinks.AzureDataExplorer.Tests/` — xUnit (net6.0, net8.0)
- **Samples**: `src/Serilog.Sinks.AzureDataExplorer.Samples/` — console demo app
- **Strong-named** with `adxSerilog.snk`
- Two ingestion modes: Batched (in-memory → Kusto) and Durable (file buffer → ship to Kusto)

## Central Package Management

This project uses **Central Package Management**. All package versions are in `src/Directory.Packages.props`. Individual `.csproj` files reference packages **without versions**.

Key rules:
- Never add `Version=` to `PackageReference` elements in `.csproj` files — that causes error NU1008
- All version changes go to `src/Directory.Packages.props`
- Some packages have TFM-conditional versions (e.g., `System.Text.Json` is `8.0.0` for net6.0 and `9.0.0` for net8.0) — preserve these conditions
- The test project overrides FluentAssertions via `PackageVersion Update=` in its `.csproj` — this is intentional

## Shared Build Properties

`src/Directory.Build.props` contains shared MSBuild properties including:
- `TargetFrameworks`
- `Version`, `AssemblyVersion`, `FileVersion`
- Package metadata (Authors, Description, License)

## Hardcoded Versions

`src/Serilog.Sinks.AzureDataExplorer/Extensions/AzureDataExplorerSinkOptionsExtensions.cs` has `ClientVersion = "2.0.0"` — this must be updated manually when releasing new versions.

## Build & Test

```sh
# From src/ directory
dotnet build
dotnet test Serilog.Sinks.AzureDataExplorer.Tests/Serilog.Sinks.AzureDataExplorer.Tests.csproj
```

### Test Tiers

- **Tier 1 — Unit tests**: Self-contained, no setup needed. Use `--filter "FullyQualifiedName!~E2E&FullyQualifiedName!~AppSettings"` to run only these.
- **Tier 2 — E2E tests**: Require a live ADX cluster. Fail with `KustoClientTimeoutException` or `AuthenticationFailedException` if prerequisites are missing.

E2E test prerequisites (all required):
1. `az login` (uses `AzureCliCredential`)
2. Three environment variables: `ingestionURI`, `databaseName`, `tenant`
3. Network access to the cluster (VPN required for internal/PPE clusters)

See `.github/prompts/update-versions.prompt.md` Step 5 for the full E2E setup guide.

## Dependency Upgrade Guidance

When upgrading packages, use the reusable prompt: `.github/prompts/update-versions.prompt.md`

Key constraints:
- TFM-conditional packages (System.*, Microsoft.Extensions.*) must keep version-to-TFM alignment: net6.0 → 8.0.x, net8.0 → 8.0.x or 9.0.x, net9.0 → 9.0.x
- Serilog ecosystem packages are TFM-agnostic — same version for all targets
- The Kusto SDK (`Microsoft.Azure.Kusto.Ingest`) pulls many transitive deps — check for conflicts after upgrading
- FluentAssertions 7.x has major breaking changes — stay on 6.x unless ready to fix tests
- net6.0 is EOL (Nov 2024) — suppress warnings with `SuppressTfmSupportBuildWarnings` or migrate to net8.0
