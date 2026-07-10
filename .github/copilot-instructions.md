# Copilot instructions for msgraph-cloud-support

## What this tool does

`CheckCloudSupport` is a .NET console tool that determines which Microsoft national
clouds (Global, US Government L4/L5, China) each Microsoft Graph API supports, then
injects a `[!INCLUDE [national-cloud-support]...]` line into the API's Markdown
reference doc. It does this by cross-referencing two external inputs:

1. **OpenAPI descriptions** (`Prod.yml` = Global, `Fairfax.yml` = US Gov,
   `Mooncake.yml` = China) from the `msgraph-metadata` repo.
2. **API reference Markdown docs** from the `microsoft-graph-docs` (or
   `m365copilot-docs-pr`) repo.

Neither of those repos lives here — paths are passed as CLI arguments. See
`.vscode/launch.json` for concrete example invocations.

## Build / test / run

- **Target framework:** `net10.0` (both projects). CI (`.github/workflows/dotnet.yml`) uses the `10.x` SDK.
- **Build:** `dotnet build`
- **Test (full suite):** `dotnet test`
- **Run a single test:** `dotnet test --filter "FullyQualifiedName~ApiDocumentTests.CreateFromMarkdownFile_LoadsGraphApiFileCorrectly"` (or `--filter "DisplayName~<name>"`).
- **Run the tool:** `dotnet run --project src -- --open-api <folder> --api-docs <folder> [--overrides <file>] [--excludes <file>]`.
  Use the `copilot` subcommand for the M365 Copilot docs, which expects the OpenAPI
  folder to contain `v1.0/` and `beta/` subfolders and requires `--include-directory`.

## Architecture / data flow

The whole pipeline is orchestrated in `src/Program.cs` (top-level statements,
System.CommandLine). There are two commands: the root command (single API version)
and `copilot` (processes v1.0 and beta separately, emitting zone-pivoted INCLUDEs when
the two versions differ).

Flow: `DocSet.CreateFromDirectory` recursively loads every `*.md`, building an
`ApiDocument` per file (files whose `doc_type` is not `apiPageType` are skipped). Each
`ApiDocument` parses its "HTTP request" section with Markdig into `ApiOperation`s.
Separately, each cloud's OpenAPI YAML is attached to an `OpenApiUrlTreeNode`. For every
operation, `OpenApiUrlTreeNodeExtensions.GetNodeByPath` locates the matching tree node
and `GetCloudSupportStatus` checks which clouds expose that method, producing a
`CloudSupportStatus`. Finally `ApiDocument.AddOrUpdateIncludeLine` (or
`AddOrUpdatePivotedIncludeLine`) rewrites the Markdown file.

Key types:
- `CloudSupportStatus` — a `[Flags]` enum. Individual clouds (`Global`, `China`,
  `USGovL4`, `USGovL5`) are OR-combined; named combinations (`AllClouds`,
  `GlobalAndChina`, etc.) map 1:1 to specific INCLUDE files (`all-clouds.md`,
  `global-china.md`, ...). When adding a new cloud combination you must add both an enum
  member and a matching `switch` arm in `ApiDocument.GetIncludeLine`.
- `OpenAPIOverrides` — static, initialized once from two optional JSON files.
  `overrides.json` remaps an API path (docs path -> OpenAPI path) when the two don't
  match; `cloud-exclusions.json` force-excludes a specific API+method+cloud, or an
  entire file+cloud. JSON schemas are in `schema/`. `src/overrides.json` and
  `src/cloud-exclusions.json` are sample/seed files.

## Conventions

- **Doc/OpenAPI path mismatches are handled by string normalization, not by editing
  data.** `ApiOperation.CreateFromStringLine` runs the raw path through a chain of
  `StringExtensions` methods (`MakePathRelativeToVersion`, `NormalizeIdSegments`,
  `FixUserDrivePath`, `FixWellKnownMailFoldersId`, etc.). Add new quirks as another
  chained extension + `[GeneratedRegex]` method rather than special-casing callers.
- **Path segment matching is case-insensitive and namespace-aware.** Use the
  `IsEqualIgnoringCase` extension (it also has special handling for OData function
  parameter formatting) rather than raw `==`/`string.Compare`.
- **StyleCop is enforced** via `Stylecop.Analyzers` with `stylecop.json`. Every source
  file starts with the `// Copyright (c) Microsoft Corporation.` / `// Licensed under
  the MIT license.` header, `using` directives go **outside** the namespace, and all
  public members need XML doc comments (`GenerateDocumentationFile` is on). `SA1101`
  (prefix local calls with `this.`) is silenced in `.editorconfig`.
- **Indentation:** 4 spaces for C#, 2 spaces otherwise (`.editorconfig`).
- **Logging:** never `Console.WriteLine` for diagnostics — use
  `OutputLogger.Logger?.Log...` with structured message templates.
- **Tests** are xUnit; the src project exposes internals via `InternalsVisibleTo`.
  Path-related tests branch on OS (`RuntimeInformation.IsOSPlatform`) because Windows
  and Unix produce different relative paths, so keep both `TheoryData` sets in sync.
