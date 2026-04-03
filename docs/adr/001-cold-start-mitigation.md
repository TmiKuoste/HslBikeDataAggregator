# ADR-001: Cold Start Mitigation via Write/Read Separation

## Status

Accepted

## Date

2025-07-17

## Context

Azure Functions on the Consumption plan have cold start latency of 5-15 seconds for .NET isolated worker. Users should not wait for the backend to warm up before seeing basic station data.

## Decision

### Write/Read Separation

- A **timer-triggered function** (every 2 minutes) polls the HSL Digitransit API and writes aggregated JSON to **Azure Blob Storage**.
- **HTTP-triggered functions** read precomputed JSON from blob storage instead of calling HSL directly.
- This ensures HTTP responses are fast (sub-second) even on cold start since they only read a blob.

### Frontend Hybrid Fallback

The HslBikeApp frontend uses progressive loading:

1. **Immediate**: fetch stations directly from HSL Digitransit (no backend dependency).
2. **Background**: call the aggregator for enriched data (trends, snapshots).
3. **Progressive**: hourly graphs and destinations load when the aggregator responds.

Basic station view is never blocked by the aggregator cold start.

## Consequences

### Positive

- Users see station data instantly regardless of backend state.
- HTTP functions are lightweight (blob read only), minimizing cold start impact.
- Data stays fresh within the 2-minute polling interval.

### Negative

- Blob storage adds an infrastructure dependency.
- Timer and HTTP functions are separate instances; timer activity does not prevent HTTP cold starts.
- 2-minute data staleness window.
