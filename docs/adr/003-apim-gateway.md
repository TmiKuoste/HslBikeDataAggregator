# ADR-003: Add Azure API Management gateway in front of HTTP endpoints

## Status

Accepted

## Date

2026-04-08

## Context

All HTTP functions used `AuthorizationLevel.Anonymous` with no server-side traffic control. CORS is enforced by browsers but easily bypassed by scripts, bots, or CLI tools. Every invocation on the Flex Consumption plan incurs per-execution charges, so unrestricted public endpoints present a cost-abuse risk.

Embedding a function key in the Blazor WASM frontend was considered but rejected — the key would be visible in browser network requests, offering only obscurity rather than real protection.

## Decision

Place an Azure API Management (APIM) Consumption-tier instance in front of all HTTP-triggered functions.

### Traffic control

- **Global rate limiting** (`rate-limit`): 200 requests per minute across all callers. The Consumption tier does not support per-IP `rate-limit-by-key` — that requires Developer tier or above.
- **Daily quota** (`quota`): hard cap of 10,000 requests per 24 hours across all callers as a cost ceiling.
- **Response caching** (`cache-lookup` / `cache-store`): 30 seconds for live endpoints (`/stations`, `/snapshots`), 3,600 seconds for slow-changing endpoints (`/availability`, `/destinations`).

### Function App lock-down

- HTTP triggers switched from `AuthorizationLevel.Anonymous` to `AuthorizationLevel.Function`.
- APIM injects the auto-generated Function App host key via a `set-header` inbound policy (`x-functions-key`).
- The host key is extracted at deployment time using `listKeys()` in Bicep and stored as a secret APIM Named Value — no Key Vault required.
- Direct callers without the key receive `401 Unauthorized`.

### Caching alignment

- APIM caches responses at the gateway layer, reducing Function App invocations.
- `Cache-Control` headers on `/stations` reduced from 120 s to 30 s (aligned with APIM cache TTL).
- `Cache-Control` on `/snapshots` (900 s), `/availability` and `/destinations` (3,600 s) remain unchanged — these serve longer-term trend data.
- The in-memory `LiveStationCacheService` (2 min TTL) is retained to protect the upstream Digitransit API independently of APIM.

## Alternatives considered

| Option | Outcome |
|---|---|
| **IP allow-list on Function App** | APIM Consumption tier does not have fixed outbound IPs — unreliable |
| **VNet integration** | Requires Premium APIM tier (~£230/month) — excessive for a hobby project |
| **Function key in frontend** | Key visible in browser DevTools — security by obscurity only |

## Consequences

### Positive

- Server-side cost protection — rejected requests never reach the Function App.
- All callers share a global rate limit; casual abusers are throttled without needing a higher-tier APIM SKU.
- Response caching reduces Function App invocations significantly for repeated requests.
- Fully automated via Bicep — no manual portal steps or secret management.
- APIM Consumption tier adds minimal cost (~£3/month).

### Negative

- Additional infrastructure component to maintain and monitor.
- APIM Consumption tier has a cold-start delay (~1-2 seconds) on idle periods.
- Rate limiting is global (not per-IP) on Consumption tier — a burst of legitimate traffic can exhaust the shared allowance.
- Frontend must be updated to call the APIM gateway URL instead of the Function App URL directly.
