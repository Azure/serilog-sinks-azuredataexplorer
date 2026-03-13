---
description: Agentic skill — autonomously upgrade NuGet dependencies in Serilog.Sinks.AzureDataExplorer, verify the build and tests, then raise a draft PR for human review.
agent: agent
---

# Skill: Upgrade NuGet Dependencies — Serilog.Sinks.AzureDataExplorer

## How to invoke this skill

**VS Code Copilot (agent mode)**: open Copilot Chat, switch to agent mode, type `#update-versions` and send. Copilot will execute all phases autonomously.

**GitHub Copilot CLI** (recommended for fully autonomous execution):
```sh
# Install (one-time):
npm install -g @github/copilot

# From the repository root, start the CLI:
copilot

# Use plan mode first to review the approach:
/plan Follow the upgrade skill at @.github/prompts/update-versions.prompt.md — upgrade all outdated NuGet packages, verify build and tests, then open a draft PR

# Review the plan, then tell it to proceed:
Implement this plan
```

### CLI tips for this skill
- **Plan mode first** (`Shift+Tab` or `/plan`): lets you review the upgrade plan before any files are touched. Approve only after verifying the constraints are respected.
- **Autopilot**: once the plan is approved, the CLI can work autonomously through all phases without prompting at each step.
- **Tool permissions**: pre-approve shell access for dotnet/git commands:
  ```sh
  copilot --allow-tool 'shell(dotnet:*)' --allow-tool 'shell(git:*)' --allow-tool 'shell(gh:*)' --deny-tool 'shell(git push --force)'
  ```
- **Model selection**: use `/model` to pick Opus 4.5 for this task — it handles multi-step constraint reasoning better than Sonnet for TFM alignment rules.
- **If context gets large**: the CLI auto-compacts, but you can run `/compact` after Phase 1 discovery if the `--outdated` output is very long.

---

## Safety principles (read before executing anything)

This skill modifies source files and pushes a branch. Follow these rules at all times:

1. **Never push directly to `main` or `master`** — always create a new branch first (Phase 0).
2. **Never modify `.csproj` files** with `Version=` attributes — causes NU1008.
3. **Never collapse TFM-conditional `<PackageVersion>` entries** into a single unconditional entry.
4. **Never upgrade `FluentAssertions` past 6.x** without explicit instruction.
5. **Only edit `src/Directory.Packages.props`** for version changes — no other files unless instructed.
6. **Stop and report** if any new build error or test regression appears — do not push a broken state.
7. **Open a draft PR**, not a ready-for-review PR — humans must review before merge.
8. **Do not run post-install scripts** from NuGet packages — only declarative version bumps in XML.

---

## Project context (read this first — especially important when running from CLI)

This is **Serilog.Sinks.AzureDataExplorer** — a NuGet library that ships structured logs to Azure Data Explorer (Kusto), owned by Microsoft.

### Repository layout
```
repo root/
  src/
    Directory.Packages.props        ← ALL package versions live here (Central Package Management)
    Directory.Build.props           ← shared TargetFrameworks, AssemblyVersion, FileVersion, Version
    Serilog.Sinks.AzureDataExplorer/
      Serilog.Sinks.AzureDataExplorer.csproj   ← main library (no Version= in PackageReferences)
      Extensions/
        AzureDataExplorerSinkOptionsExtensions.cs  ← contains hardcoded ClientVersion string
    Serilog.Sinks.AzureDataExplorer.Tests/
      Serilog.Sinks.AzureDataExplorer.Tests.csproj
    Serilog.Sinks.AzureDataExplorer.Samples/
      Serilog.Sinks.AzureDataExplorer.Samples.csproj
```

### Target frameworks
- **Main library**: net9.0, net8.0, net6.0, netstandard2.0, net471, net462
- **Tests + Samples**: net8.0, net6.0

### Key architectural facts
- **Central Package Management (CPM)**: `src/Directory.Packages.props` is the single source of truth for all package versions. Individual `.csproj` files reference packages *without* `Version=` attributes — adding one causes build error NU1008.
- **TFM-conditional versions**: Some packages (e.g. `System.Text.Json`) have separate `<PackageVersion>` entries per TFM using `Condition` attributes. These must stay separate — do not collapse them.
- **FluentAssertions override**: The test `.csproj` has `<PackageVersion Update="FluentAssertions" Version="6.9.0" />` — intentional, leave it.
- **Hardcoded version string**: `ClientVersion` in `AzureDataExplorerSinkOptionsExtensions.cs` — only update when doing a library release, not for dependency-only upgrades.
- **Shared build props**: `Directory.Build.props` holds `<Version>`, `<AssemblyVersion>`, `<FileVersion>` — do not edit for a dependency-only upgrade.

---

## Phase 0 — Pre-flight checks (do this before anything else)

From the **repository root**:

```sh
# Confirm you are in the right repository
cat src/Directory.Build.props | grep -E "PackageProjectUrl|Version>"

# Confirm working tree is clean — do not start if there are uncommitted changes
git status

# Confirm you are on main (or the expected base branch)
git branch --show-current

# Create the feature branch NOW — before any file edits
git checkout -b chore/upgrade-nuget-deps-$(date +%Y-%m)
```

If `git status` shows uncommitted changes, **stop** and report what was found. Do not proceed.

---

## Phase 1 — Discover what needs upgrading

Run from the **repository root**:

```sh
dotnet list src/Serilog.Sinks.AzureDataExplorer/Serilog.Sinks.AzureDataExplorer.csproj package --outdated
dotnet list src/Serilog.Sinks.AzureDataExplorer.Samples/Serilog.Sinks.AzureDataExplorer.Samples.csproj package --outdated
dotnet list src/Serilog.Sinks.AzureDataExplorer.Tests/Serilog.Sinks.AzureDataExplorer.Tests.csproj package --outdated
```

Also run a security scan to identify packages with known CVEs:

```sh
dotnet list src/Serilog.Sinks.AzureDataExplorer/Serilog.Sinks.AzureDataExplorer.csproj package --vulnerable --include-transitive
dotnet list src/Serilog.Sinks.AzureDataExplorer.Tests/Serilog.Sinks.AzureDataExplorer.Tests.csproj package --vulnerable --include-transitive
```

From the output, build a list of: package name, current version, latest version, upgrade type (patch / minor / major), and any CVE severity.

> **Security rule**: Packages with High or Critical CVEs must be upgraded (or the CVE explicitly accepted with justification in the PR). Do not defer a security fix without documenting the reason.

---

## Phase 2 — Plan before touching any file

Before making any edits, produce an upgrade plan that lists:
- Every package you will bump and to what version
- Every package you will intentionally defer and why (see constraints below)
- Any breaking-change risk (major bumps)
- Any CVEs addressed by this upgrade

Apply the constraints in the section below to decide what to bump and what to skip. If a constraint blocks an upgrade, note it in the plan — do not skip silently.

---

## Phase 3 — Apply changes

### The only file you edit for versions: `src/Directory.Packages.props`

This project uses Central Package Management. **Never add or change `Version=` in any `.csproj` file** — that causes error NU1008 and will break the build.

Exception: the test project overrides `FluentAssertions` via `<PackageVersion Update="FluentAssertions" ... />` in its own `.csproj` — this is intentional, leave it.

**For simple bumps** (one unconditional `<PackageVersion>` entry): change `Version` in `Directory.Packages.props` only.

**For TFM-conditional entries** (e.g. `System.Text.Json`): there are multiple `<PackageVersion>` entries with `Condition` attributes — update EACH one separately, keeping the conditions intact. Never collapse them into a single unconditional entry.

After every batch of edits, run:
```sh
cd src && dotnet restore && dotnet build
```
Fix any errors before proceeding to the next batch.

---

## Phase 4 — Verify: build

From the **`src/` directory** (`cd src` first):

```sh
dotnet clean
dotnet restore
dotnet build
```

All 6 target frameworks must build. Pre-existing warnings NETSDK1138 (net6.0 EOL) and NU1603 are acceptable — do not treat them as failures. Any **new** error or **new** warning is a blocker.

---

## Phase 5 — Verify: tests

Run Tier 1 unit tests (no credentials needed) from the **`src/` directory**:

```sh
dotnet test Serilog.Sinks.AzureDataExplorer.Tests/Serilog.Sinks.AzureDataExplorer.Tests.csproj --filter "FullyQualifiedName!~E2E&FullyQualifiedName!~AppSettings"
```

**Pass condition**: all tests that were passing before your changes still pass. If a test that was already failing before your changes still fails, note it in the PR but do not block the PR on it. CI will run E2E tests after merge.

If any previously-passing test now fails:
1. Check whether the failure is caused by a breaking API change in the upgraded package.
2. If yes, fix the test or revert only that package upgrade and document the deferral.
3. If the cause is unclear, revert the change and note it as a blocked upgrade in the PR.
4. **Never push a state where previously-passing tests now fail.**

---

## Phase 6 — Commit and open a draft PR

From the **repository root**:

```sh
# Stage only the files you intentionally changed
git add src/Directory.Packages.props
# Add any other files changed (e.g. Directory.Build.props if version was bumped)
git diff --name-only --cached   # verify what you are about to commit
git commit -m "chore: upgrade NuGet dependencies $(date +%B\ %Y)"
git push --set-upstream origin chore/upgrade-nuget-deps-$(date +%Y-%m)
```

Then open a **draft** PR with the GitHub CLI:

```sh
gh pr create \
  --draft \
  --title "chore: upgrade NuGet dependencies $(date +%B\ %Y)" \
  --body "$(cat <<'EOF'
## Changes

### Packages bumped
| Package | Old | New | Type |
|---------|-----|-----|------|
<!-- fill from your Phase 2 upgrade plan -->

### Intentionally deferred
| Package | Latest | Reason |
|---------|--------|--------|
<!-- list any packages skipped and why -->

### Security
- CVEs addressed: <!-- list any -->
- CVEs still present (deferred): <!-- list with justification -->

## Verification
- Build: all 6 target frameworks clean (0 new errors, 0 new warnings)
- Tier 1 unit tests: all previously-passing tests still pass
- E2E tests: skipped locally — CI will validate on this PR

## Notes
- This is a dependency-only upgrade. `ClientVersion` in `AzureDataExplorerSinkOptionsExtensions.cs` was NOT changed.
- Reviewers: please verify deferred packages and any CVE deferrals before merging.
EOF
)" \
  --base main
```

Fill in the tables from your Phase 2 plan before running `gh pr create`. The PR must be **draft** — do not promote to ready-for-review.

---

## Constraints you must follow

### Central Package Management rules
- Edit `src/Directory.Packages.props` only — never `.csproj` files (except the intentional FluentAssertions override already there)
- Never add `Version=` to a `<PackageReference>` — this causes NU1008

### TFM version alignment (System.* and Microsoft.Extensions.*)

| TFM | Allowed version range |
|-----|----------------------|
| net462 / net471 | 8.0.x |
| netstandard2.0 | 8.0.x |
| net6.0 (EOL) | 8.0.x — do NOT go to 9.x |
| net8.0 (LTS) | 8.0.x or 9.0.x |
| net9.0 (STS) | 9.0.x |

Serilog ecosystem packages (`Serilog`, `Serilog.Sinks.*`, `Serilog.Formatting.*`) are TFM-agnostic — use the same version for all targets.

### Known breaking changes — apply these rules before bumping

| Package | Rule |
|---------|------|
| `FluentAssertions` 7.x | **Do not upgrade.** Major breaking changes in test assertions. Stay on 6.x unless explicitly asked. |
| `xunit.runner.visualstudio` 3.x | Requires xunit 3.x — do not upgrade while on xunit 2.x. |
| `Microsoft.NET.Test.Sdk` 18.x | Dropped net6.0 support — do not upgrade while tests target net6.0. |
| `Serilog.Settings.Configuration` 10.x | Requires `Microsoft.Extensions.Configuration` ≥ 10.x — blocked while net6.0 TFM pins at 8.0.x. |
| `Serilog` 4.x | If already on 4.x, bumping within 4.x is safe. |
| `Microsoft.Azure.Kusto.Ingest` 14.x | Public API unchanged, safe to bump. |
| `System.Text.Json` 9.x | Only for net8.0 / net9.0 entries — do not apply to net6.0 entry. |

---

## Reference: Breaking changes

### Serilog 3.x → 4.x (only relevant if upgrading from 3.x)
- `IBatchedLogEventSink` moved from `Serilog.Sinks.PeriodicBatching` to `Serilog` core
- `PeriodicBatchingSink` wrapper removed — use `IBatchedLogEventSink` directly with `BatchingOptions`
- `Serilog.Sinks.PeriodicBatching` NuGet package becomes obsolete

### Serilog.Extensions.Logging 9.x
- Requires Serilog 4.x and `Microsoft.Extensions.Logging` ≥ 8.0.0

### Serilog.Settings.Configuration 9.x
- Requires Serilog 4.x and `Microsoft.Extensions.Configuration` ≥ 8.0.0

### FluentAssertions 7.x (do not apply)
- `BeEquivalentTo()` options changed, `WithMessage()` requires exact match, many deprecated methods removed

---

## Reference: E2E test setup (for context — CI handles this)

E2E tests require:
1. `az login` (uses `AzureCliCredential`)
2. Env vars: `ingestionURI`, `databaseName`, `tenant`
3. Network access to the ADX cluster (VPN if internal/PPE)

You do not need to run E2E tests. The PR will trigger CI which runs them.

---

## References

- [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management)
- [.NET Support Policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
- [Serilog Docs](https://serilog.net/)
- [Azure Data Explorer .NET SDK](https://learn.microsoft.com/azure/data-explorer/kusto/api/netfx/about-the-sdk)
- [dotnet list package --vulnerable](https://learn.microsoft.com/dotnet/core/tools/dotnet-list-package)

---

## Project context (read this first — especially important when running from CLI)

This is **Serilog.Sinks.AzureDataExplorer** — a NuGet library that ships structured logs to Azure Data Explorer (Kusto), owned by Microsoft.

### Repository layout
```
repo root/
  src/
    Directory.Packages.props        ← ALL package versions live here (Central Package Management)
    Directory.Build.props           ← shared TargetFrameworks, AssemblyVersion, FileVersion, Version
    Serilog.Sinks.AzureDataExplorer/
      Serilog.Sinks.AzureDataExplorer.csproj   ← main library (no Version= in PackageReferences)
      Extensions/
        AzureDataExplorerSinkOptionsExtensions.cs  ← contains hardcoded ClientVersion string
    Serilog.Sinks.AzureDataExplorer.Tests/
      Serilog.Sinks.AzureDataExplorer.Tests.csproj
    Serilog.Sinks.AzureDataExplorer.Samples/
      Serilog.Sinks.AzureDataExplorer.Samples.csproj
```

### Target frameworks
- **Main library**: net9.0, net8.0, net6.0, netstandard2.0, net471, net462
- **Tests + Samples**: net8.0, net6.0

### Key architectural facts for this task
- **Central Package Management (CPM)**: `src/Directory.Packages.props` is the single source of truth for all package versions. Individual `.csproj` files reference packages *without* `Version=` attributes — adding one causes build error NU1008.
- **TFM-conditional versions**: Some packages (e.g. `System.Text.Json`) have separate `<PackageVersion>` entries per TFM using `Condition` attributes. These must stay separate — do not collapse them.
- **FluentAssertions override**: The test `.csproj` has `<PackageVersion Update="FluentAssertions" Version="6.9.0" />` — intentional, leave it.
- **Hardcoded version string**: `ClientVersion = "2.0.0"` in `AzureDataExplorerSinkOptionsExtensions.cs` — only update when doing a library release, not for dependency-only upgrades.
- **Shared build props**: `Directory.Build.props` holds `<Version>`, `<AssemblyVersion>`, `<FileVersion>`, and `<TargetFrameworks>` — do not edit these for a dependency upgrade.

---

## Your task

Upgrade all outdated NuGet packages in this repository following the constraints below, verify the build and tests pass, then commit and open a pull request for human review. Do not wait for approval at each step — work autonomously from discovery through PR creation.

---

## Phase 1 — Discover what needs upgrading

Run from the **repository root**:

```sh
dotnet list src/Serilog.Sinks.AzureDataExplorer/Serilog.Sinks.AzureDataExplorer.csproj package --outdated
dotnet list src/Serilog.Sinks.AzureDataExplorer.Samples/Serilog.Sinks.AzureDataExplorer.Samples.csproj package --outdated
dotnet list src/Serilog.Sinks.AzureDataExplorer.Tests/Serilog.Sinks.AzureDataExplorer.Tests.csproj package --outdated
```

From the output, build a list of: package name, current version, latest version, upgrade type (patch / minor / major).

---

## Phase 2 — Plan before touching any file

Before making any edits, produce an upgrade plan that lists:
- Every package you will bump and to what version
- Every package you will intentionally defer and why (see constraints below)
- Any breaking-change risk (major bumps)

Apply the constraints in the section below to decide what to bump and what to skip. If a constraint blocks an upgrade, note it in the plan — do not skip silently.

---

## Phase 3 — Apply changes

### The only file you edit for versions: `src/Directory.Packages.props`

This project uses Central Package Management. **Never add or change `Version=` in any `.csproj` file** — that causes error NU1008 and will break the build.

Exception: the test project overrides `FluentAssertions` via `<PackageVersion Update="FluentAssertions" ... />` in its own `.csproj` — this is intentional, leave it.

**For simple bumps** (one unconditional `<PackageVersion>` entry): change `Version` in `Directory.Packages.props` only.

**For TFM-conditional entries** (e.g. `System.Text.Json`): there are multiple `<PackageVersion>` entries with `Condition` attributes — update EACH one separately, keeping the conditions intact. Never collapse them into a single unconditional entry.

After every batch of edits, run:
```sh
cd src && dotnet restore && dotnet build
```
Fix any errors before proceeding to the next batch.

---

## Phase 4 — Verify: build

From the **`src/` directory** (`cd src` first):

```sh
dotnet clean
dotnet restore
dotnet build
```

All 6 target frameworks must build. Pre-existing warnings NETSDK1138 (net6.0 EOL) and NU1603 are acceptable — do not treat them as failures. Any new error or new warning is a blocker.

---

## Phase 5 — Verify: tests

Run Tier 1 unit tests (no credentials needed) from the **`src/` directory**:

```sh
dotnet test Serilog.Sinks.AzureDataExplorer.Tests/Serilog.Sinks.AzureDataExplorer.Tests.csproj --filter "FullyQualifiedName!~E2E&FullyQualifiedName!~AppSettings"
```

**Pass condition**: all tests that were passing before your changes still pass. If a test that was already failing before your changes still fails, note it in the PR but do not block the PR on it. CI will run E2E tests after merge.

If any previously-passing test now fails:
1. Check whether the failure is caused by a breaking API change in the upgraded package.
2. If yes, fix the test or revert only that package upgrade and document the deferral.
3. If the cause is unclear, revert the change and note it as a blocked upgrade in the PR.

---

## Phase 6 — Commit and open a PR

From the **repository root**:

```sh
git checkout -b chore/upgrade-nuget-deps-<YYYY-MM>
git add src/Directory.Packages.props
git commit -m "chore: upgrade NuGet dependencies <Month YYYY>"
git push --set-upstream origin chore/upgrade-nuget-deps-<YYYY-MM>
```

Then open a PR with the GitHub CLI:

```sh
gh pr create \
  --title "chore: upgrade NuGet dependencies <Month YYYY>" \
  --body "$(cat <<'EOF'
## Changes

### Packages bumped
| Package | Old | New | Type |
|---------|-----|-----|------|
<!-- fill from your upgrade plan -->

### Intentionally deferred
<!-- list any packages skipped and why -->

## Verification
- Tier 1 unit tests: X passed, Y failed (pre-existing: list any)
- Build: all 6 target frameworks clean
- E2E tests: skipped locally — CI will validate

## Notes
- `ClientVersion` in `AzureDataExplorerSinkOptionsExtensions.cs` was NOT updated (dependency-only upgrade)
EOF
)" \
  --base main
```

Fill in the table and deferred list from your Phase 2 plan before running `gh pr create`.

---

## Constraints you must follow

### Central Package Management rules
- Edit `src/Directory.Packages.props` only — never `.csproj` files (except the intentional FluentAssertions override already there)
- Never add `Version=` to a `<PackageReference>` — this causes NU1008

### TFM version alignment (System.* and Microsoft.Extensions.*)

| TFM | Allowed version range |
|-----|----------------------|
| net462 / net471 | 8.0.x |
| netstandard2.0 | 8.0.x |
| net6.0 (EOL) | 8.0.x — do NOT go to 9.x |
| net8.0 (LTS) | 8.0.x or 9.0.x |
| net9.0 (STS) | 9.0.x |

Serilog ecosystem packages (`Serilog`, `Serilog.Sinks.*`, `Serilog.Formatting.*`) are TFM-agnostic — use the same version for all targets.

### Known breaking changes — apply these rules before bumping

| Package | Rule |
|---------|------|
| `FluentAssertions` 7.x | **Do not upgrade.** Major breaking changes in test assertions. Stay on 6.x unless explicitly asked. |
| `Serilog` 4.x | If already on 4.x, bumping within 4.x is safe. If upgrading from 3.x, API changes required (see reference below). |
| `Serilog.Sinks.File` 7.x | Requires Serilog 4.x — ensure both are upgraded together. |
| `Microsoft.Azure.Kusto.Ingest` 14.x | Public API unchanged, safe to bump. |
| `System.Text.Json` 9.x | Only for net8.0 / net9.0 entries — do not apply to net6.0 entry. |

### Hardcoded version string
`src/Serilog.Sinks.AzureDataExplorer/Extensions/AzureDataExplorerSinkOptionsExtensions.cs` contains `ClientVersion = "2.0.0"`. Only update this if you are doing a library release (not a dependency-only upgrade). Note its current value in the PR body either way.

---

## Reference: Breaking changes

### Serilog 3.x → 4.x (only relevant if upgrading from 3.x)
- `IBatchedLogEventSink` moved from `Serilog.Sinks.PeriodicBatching` to `Serilog` core
- `PeriodicBatchingSink` wrapper removed — use `IBatchedLogEventSink` directly with `BatchingOptions`
- `Serilog.Sinks.PeriodicBatching` NuGet package becomes obsolete

### Serilog.Extensions.Logging 9.x
- Requires Serilog 4.x and `Microsoft.Extensions.Logging` ≥ 8.0.0

### Serilog.Settings.Configuration 9.x
- Requires Serilog 4.x and `Microsoft.Extensions.Configuration` ≥ 8.0.0

### FluentAssertions 7.x (do not apply)
- `BeEquivalentTo()` options changed, `WithMessage()` requires exact match, many deprecated methods removed

---

## Reference: E2E test setup (for context — CI handles this)

E2E tests require:
1. `az login` (uses `AzureCliCredential`)
2. Env vars: `ingestionURI`, `databaseName`, `tenant`
3. Network access to the ADX cluster (VPN if internal/PPE)

You do not need to run E2E tests. The PR will trigger CI which runs them. If you have the env vars available, you may optionally run:

```sh
cd src
dotnet test Serilog.Sinks.AzureDataExplorer.Tests/Serilog.Sinks.AzureDataExplorer.Tests.csproj
```

---

## References

- [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management)
- [.NET Support Policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
- [Serilog Docs](https://serilog.net/)
- [Azure Data Explorer .NET SDK](https://learn.microsoft.com/azure/data-explorer/kusto/api/netfx/about-the-sdk)
