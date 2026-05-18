# ADR-004: Generic Open Data Polling Framework

## Status

Accepted

## Date

2026-05-18

## Context

The system needs to poll external open data sources — starting with Uimastadion swimmer fill level — and expose their time series via the same REST API that serves bike availability data. Several design questions arose:

1. **One function per source vs. a generic framework** — should each new data source get its own timer-triggered function, or should all sources share a single timer and a common abstraction?
2. **How to represent missing or unavailable data** — Uimastadion is seasonal; the external API may return no data outside the swim season, or fail transiently.
3. **Where to store source metadata** — display name, coordinates, and attribution URL are needed at read time. Should they be derived from config at read time, or embedded in the blob?
4. **DI registration for multiple source instances** — using `IEnumerable<T>` registration via `AddSingleton<T, TImpl>` would allow auto-resolution, but conflicts with the way the isolated worker host resolves constructor parameters.

## Decisions

### 1. Generic `IOpenDataSource` framework

A single `PollOpenDataFunction` fans out over `IReadOnlyList<IOpenDataSource>`. New sources are added by implementing `IOpenDataSource` and registering a new entry in the config; no new function file is required.

**Rationale:** The polling schedule is the same for all sources. A bespoke function per source would duplicate the retry/sentinel/blob-write logic and spread config across multiple timer expressions.

### 2. `-1` sentinel for missing or failed values

`IOpenDataSource.FetchAsync` returns `double?`. A `null` return signals that the value is currently unavailable (e.g. out of season). An unhandled exception is caught by the poll service. Both cases record `-1` in the time series, consistent with the `-1` convention already used in snapshot data for stations not yet seen.

**Rationale:** Clients can distinguish "no data yet" (empty array) from "known-unavailable" (`-1`), enabling future UI treatment such as greying out a card outside season.

### 3. Metadata embedded in each blob write

`OpenDataTimeSeries` carries `displayName`, `lat`, `lon`, and `attributionUrl` alongside the time series data. These fields are refreshed from the source configuration on every poll write.

**Rationale:** `GetOpenDataFunction` reads blobs in parallel without needing access to the source configuration at read time, keeping the HTTP path simple. Config updates (e.g. a renamed display name) propagate automatically on the next successful poll.

### 4. `IReadOnlyList<IOpenDataSource>` registered via a factory singleton

Rather than `AddSingleton<IOpenDataSource, VenueFillLevelSource>` (which would allow only one implementation), sources are registered as `IReadOnlyList<IOpenDataSource>` via a factory delegate that reads `OpenDataOptions.VenueFillLevelSources` at startup.

**Rationale:** The isolated worker host resolves constructor parameters by exact type. `IEnumerable<IOpenDataSource>` can conflict with how the DI container resolves open-generic enumerables. `IReadOnlyList<T>` is unambiguous and clearly communicates that the list is fixed at startup.

## Consequences

### Positive

- New open data sources require only a config entry and an `IOpenDataSource` implementation — no new function boilerplate.
- Single failure isolation: one source throwing does not affect others in the same poll cycle.
- Blob path `open-data/{sourceId}/recent.json` keeps open data segregated from bike data within the same container.

### Negative

- All sources share the same poll interval (`OpenDataPollIntervalCron`). If sources need different intervals in future, the framework would need to be extended.
- Metadata staleness: if a source is removed from config but its blob remains, stale blobs are not automatically cleaned up.
