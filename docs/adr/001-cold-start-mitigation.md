# ADR-001: Cold Start Mitigation via Write/Read Separation

## Status

Accepted — updated 2025-07-19 (revised polling interval and cold-start analysis)

## Date

2026-04-15

## Context

Azure Functions on the Flex Consumption plan (FC1) scale to zero between invocations. Cold start latency for a .NET 10 isolated worker is typically 5–15 seconds. With the current 15-minute polling interval (`PollIntervalCron = 0 */15 * * * *`), the Function App is idle between timer invocations and cold starts are **expected on most requests** — both timer and HTTP triggers.

Users should not wait for the backend to warm up before seeing basic station data.

## Decision

### Write/Read Separation

- A **timer-triggered function** (every 15 minutes) polls the HSL Digitransit API and writes a rolling snapshot time series to **Azure Blob Storage**.
- A separate **daily timer** downloads HSL open-data monthly trip-history CSVs and writes per-station statistics blobs.
- `GetSnapshots` and `GetStationStatistics` **read precomputed blobs** — these responses are fast (sub-second after the runtime has started) because they perform no upstream API calls.
- `GetStations` does **not** read from blob storage. Instead, `LiveStationCacheService` calls the Digitransit API on the first request and caches the result in-memory for 2 minutes. This gives fresher data than the 15-minute timer but means `GetStations` is the one HTTP endpoint that incurs an upstream network call on cold start.

### API Management Caching

Azure API Management caches HTTP responses (30 s for `/stations` and `/snapshots`, 3 600 s for statistics). Cached responses are returned instantly regardless of Function App state, which masks cold starts for repeated requests within the cache window.

### Frontend Progressive Loading

The HslBikeApp frontend uses progressive loading:

1. **Immediate**: fetch stations directly from HSL Digitransit (no backend dependency).
2. **Background**: call the aggregator for enriched data (trends, snapshots).
3. **Progressive**: hourly graphs and destinations load when the aggregator responds.

Basic station view is never blocked by the aggregator cold start.

## Consequences

### Positive

- Users see station data instantly regardless of backend state (frontend fetches directly from Digitransit for the basic view).
- `GetSnapshots` and `GetStationStatistics` are lightweight blob reads, minimising cold-start impact for those endpoints.
- APIM response caching shields end users from cold starts on repeated requests.

### Negative

- With a 15-minute polling interval on Flex Consumption, the Function App is idle most of the time — cold starts are frequent for both timer and HTTP triggers.
- Timer and HTTP functions run in separate instances; timer activity does not keep HTTP instances warm.
- Snapshot data can be up to 15 minutes stale; live station data (`GetStations`) relies on an on-demand Digitransit call and its 2-minute in-memory cache.
- `GetStations` pays the full cold-start penalty plus an upstream Digitransit call when the cache is empty, making it the slowest endpoint on a cold start.
- Blob storage and APIM add infrastructure dependencies.
