---
applyTo: "**/Functions/*.cs"
---
# Azure Functions Guidelines

- Use isolated worker model attributes (`[Function]`, `[HttpTrigger]`, `[TimerTrigger]`).
- HTTP functions return `IActionResult` or `HttpResponseData`.
- Timer functions return `Task` (void).
- Inject services via constructor (`ILogger<T>`, storage clients, `HttpClient`).
- HTTP triggers use `AuthorizationLevel.Function` — APIM injects the host key; anonymous access is not permitted.
- Use `CancellationToken` parameter on all async functions.
