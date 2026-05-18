# HslBikeDataAggregator — Claude Instructions

> **Keep in sync with `.github/copilot-instructions.md`** and the files under `.github/instructions/`. When updating either file, apply the same change to the other.

## System Overview

Helsinki city bike data backend. Part of a two-repo system:

| Repo | Role | Tech | Hosting |
|---|---|---|---|
| **HslBikeApp** | Blazor WASM frontend | .NET 10, Blazor WebAssembly, Leaflet.js | GitHub Pages (tmikuoste.github.io/HslBikeApp/) |
| **HslBikeDataAggregator** (this repo) | C# backend service | .NET 10, Azure Functions (isolated worker), Azure API Management | Azure Functions (Flex Consumption) |

## Architecture

```
┌──────────────────────────┐       REST/JSON        ┌─────────────────────────────┐
│  HslBikeApp              │ ◄────────────────────── │  HslBikeDataAggregator      │
│  Blazor WASM             │                         │  Azure Functions            │
│  (GitHub Pages)          │                         │  + APIM gateway             │
└──────────────────────────┘                         └──────────┬──────────────────┘
                                                                │
                                              ┌─────────────────┼──────────────────┐
                                              │                 │                  │
                                   ┌──────────▼──────┐  ┌──────▼──────┐  ┌───────▼──────┐
                                   │ HSL Digitransit │  │ HSL Open    │  │ External     │
                                   │ GraphQL API     │  │ History CSV │  │ Open Data    │
                                   └─────────────────┘  └─────────────┘  └──────────────┘
```

This service is the **only component** that holds the HSL Digitransit API key. The frontend never calls upstream APIs directly.

### Data pipelines

Three independent pipelines share the same blob container:

1. **Live station availability** — `PollStations` (timer, 15 min) writes `bike-data/snapshots/recent.json`; `LiveStationCacheService` calls Digitransit on demand with 2-minute in-memory cache; `GetStations` and `GetSnapshots` serve the data.
2. **Historical trip statistics** — `ProcessStationHistory` (timer, daily 02:00 UTC) downloads HSL open-history CSVs, aggregates per-station demand profiles and destination tables, writes `bike-data/monthly-stats/{stationId}.json`; `GetStationStatistics` serves the data.
3. **External open data** — `PollOpenData` (timer, 15 min) fans out over all `IOpenDataSource` implementations, writes `open-data/{sourceId}/recent.json` per source; `GetOpenData` serves the data.

### Write/read separation

Timer-triggered functions write precomputed JSON blobs. HTTP functions read those blobs directly — sub-second responses even on cold start. See [ADR-001](docs/adr/001-cold-start-mitigation.md).

## Functions

| Function | Trigger | Route | Purpose |
|---|---|---|---|
| `PollStations` | Timer `%PollIntervalCron%` (default every 15 min) | — | Poll Digitransit GraphQL, append snapshot to rolling time series |
| `ProcessStationHistory` | Timer `%HistoryProcessingCron%` (default 02:00 UTC daily) | — | Download HSL CSV, aggregate monthly statistics per station |
| `PollOpenData` | Timer `%OpenDataPollIntervalCron%` (default every 15 min) | — | Poll all `IOpenDataSource` instances, append value to per-source blob |
| `GetStations` | HTTP GET | `/api/stations` | Live station availability (in-memory cache → Digitransit fallback) |
| `GetSnapshots` | HTTP GET | `/api/snapshots` | Compact rolling snapshot time series |
| `GetStationStatistics` | HTTP GET | `/api/stations/{id}/statistics` | Monthly demand profile and top destinations |
| `GetOpenData` | HTTP GET | `/api/open-data` | Rolling time series for all configured open data sources |

All HTTP triggers use `AuthorizationLevel.Function`. APIM injects the function host key — direct calls without the key receive `401 Unauthorized`.

## API Contract

- `GET /api/stations` → `BikeStation[]`
- `GET /api/snapshots` → `SnapshotTimeSeries` (columnar; `-1` = station not yet seen)
- `GET /api/stations/{id}/statistics` → `MonthlyStationStatistics`
- `GET /api/open-data` → `OpenDataTimeSeries[]` (`-1` = source unavailable/out of season)

## Key Models

- `BikeStation` — id, name, lat/lon, capacity, bikesAvailable, spacesAvailable, isActive
- `SnapshotTimeSeries` — intervalMinutes, timestamps[], rows[] (columnar: stationId + int counts per timestamp)
- `StationCountSeries` — stationId + int[] counts aligned with timestamps
- `MonthlyStationStatistics` — month, DemandProfile, DestinationTable (columnar)
- `DemandProfile` — departuresByHour, arrivalsByHour, weekday/weekend variants (24-element int arrays)
- `OpenDataTimeSeries` — sourceId, displayName, lat, lon, attributionUrl, timestamps[], values[] (double)
- `IOpenDataSource` — interface: SourceId, DisplayName, Lat, Lon, AttributionUrl + `Task<double?> FetchAsync(CancellationToken)` (`null` = unavailable → `-1` sentinel stored)
- `VenueFillLevelSource` — `IOpenDataSource` implementation for jaskaretail venue fill level API

## Storage Layout

| Blob path | Content | Writer |
|---|---|---|
| `bike-data/snapshots/recent.json` | Rolling snapshot time series (up to `SnapshotHistoryLimit` entries) | `PollStations` |
| `bike-data/monthly-stats/{stationId}.json` | Monthly demand profile + destination table for one station | `ProcessStationHistory` |
| `open-data/{sourceId}/recent.json` | Rolling time series for one open data source (up to `OpenData:HistoryLimit` entries) | `PollOpenData` |

## Configuration

| Setting | Description | Default |
|---|---|---|
| `DigitransitSubscriptionKey` | HSL Digitransit API key (GitHub secret) | — |
| `PollIntervalCron` | NCRONTAB for `PollStations` | `0 */15 * * * *` |
| `SnapshotHistoryLimit` | Max snapshots retained | `60` |
| `HistoryProcessingCron` | NCRONTAB for `ProcessStationHistory` (GitHub secret) | `0 0 2 * * *` |
| `OpenDataPollIntervalCron` | NCRONTAB for `PollOpenData` (GitHub secret) | `0 */15 * * * *` |
| `OpenData:HistoryLimit` | Max values retained per open data source | `60` |
| `OpenData:VenueFillLevelSources:0:*` | First venue fill level source (SourceId, DisplayName, Lat, Lon, AttributionUrl, LocationId, LocationUrlName) | Uimastadion defaults |

In Azure, hierarchical config keys use double-underscore notation (`OpenData__VenueFillLevelSources__0__SourceId`).

## Conventions

- Records for all data models (immutable DTOs); classes for services and state.
- File-scoped namespaces, nullable enabled, implicit usings.
- Services return empty collections on failure (never null); `null` is acceptable for optional single values.
- `ReadFromJsonAsync<T>()` / `System.Text.Json` for JSON deserialisation; use `[JsonPropertyName]` when C# names differ from JSON keys.
- `-1` sentinel in time series arrays signals a missing or unavailable value (consistent across snapshots and open data).
- `IReadOnlyList<IOpenDataSource>` is registered as a factory singleton — avoids DI conflicts with `IEnumerable<T>` in the isolated worker host. See [ADR-004](docs/adr/004-open-data-polling-framework.md).
- British English in all identifiers where practical, preserving required external API, framework, library, and contract names.

## Azure Functions Guidelines

- Use `[Function]`, `[HttpTrigger]`, `[TimerTrigger]` attributes from the isolated worker model.
- HTTP functions return `HttpResponseData`; timer functions return `Task`.
- Inject services via constructor (`ILogger<T>`, storage clients, `HttpClient`, `TimeProvider`).
- HTTP triggers use `AuthorizationLevel.Function` — APIM injects the host key; no anonymous access.
- Use `CancellationToken` on all async functions.
- CORS for `https://tmikuoste.github.io` is configured in both the Function App (Bicep) and the APIM gateway policy.

## Infrastructure

- Bicep templates in `infra/` with environment parameter files (`dev.bicepparam`, `prod.bicepparam`).
- Per-environment: resource group, storage account, Application Insights + Log Analytics workspace, Flex Consumption hosting plan, Function App, APIM Consumption instance.
- User-assigned managed identity for passwordless blob storage access (no connection strings in production).
- APIM Consumption tier: rate limit (200 req/min), response caching (30 s stations/snapshots, 120 s open-data, 3600 s statistics), function key injection. See [ADR-003](docs/adr/003-apim-gateway.md).

## Delivery Workflow

- Always start by pulling the latest from the remote (`git pull`) before beginning any work to prevent merge conflicts.
- Keep implementation work tied to an open GitHub issue.
- Use an issue branch named `issue-<number>-<short-description>` for delivery.
- If an issue was closed before its code was pushed, reopen the issue before continuing work.
- Add or update automated tests for each delivered behaviour or repository-level configuration change.
- Run `dotnet build HslBikeDataAggregator.slnx` and then `dotnet test HslBikeDataAggregator.slnx` before considering the issue complete.
- Do not treat an issue as done until the branch is pushed, the pull request is open, and CI is passing.
- Explicitly link pull requests to their GitHub issue using closing keywords such as `Closes #<issue>`.
- Continue to use Architecture Decision Records (ADRs) for significant architectural decisions.
- When reorganising files, use moves/renames that preserve Git history instead of recreating files at new paths.

## Language Preferences

- Use British English consistently in responses, code comments, documentation, commit and pull request text, and GitHub content.
- Avoid non-English or stray foreign text in responses.

## Corrections & Lessons Learned

### Configuration format

- Local `local.settings.json` uses colon notation (`OpenData:VenueFillLevelSources:0:SourceId`).
- Azure app settings and GitHub Actions workflow `az functionapp config appsettings set` use double-underscore notation (`OpenData__VenueFillLevelSources__0__SourceId`).
- Both are valid for `IConfiguration` binding; `IConfiguration.GetSection("OpenData:VenueFillLevelSources")` works with either.

### VenueFillLevelSource response shape

- The jaskaretail fill level API returns `{ "result": { "fill_level": 185, ... }, "isError": false }`.
- The field is `fill_level` inside `result`, not a flat `currentAmount` field.
- Always verify actual API response shapes against the code in `VenueFillLevelSource.cs` before assuming field names.

### Open Issues

- **#68** (phase:4-independent): Generalise architecture for multi-city deployments.
