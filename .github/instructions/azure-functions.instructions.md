---
applyTo: "**/Functions/*.cs"
---
# Azure Functions Guidelines

- Use isolated worker model attributes (`[Function]`, `[HttpTrigger]`, `[TimerTrigger]`).
- HTTP functions return `IActionResult` or `HttpResponseData`.
- Timer functions return `Task` (void).
- Inject services via constructor (`ILogger<T>`, storage clients, `HttpClient`).
- Enable CORS for `https://kuoste.github.io` origin in `host.json` or function-level.
- Use `CancellationToken` parameter on all async functions.
