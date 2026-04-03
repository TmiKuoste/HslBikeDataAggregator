---
applyTo: "**/Models/*.cs"
---
# Model Guidelines

- Use records for all data models (immutable DTOs).
- Keep models compatible with the HslBikeApp frontend (same property names, same JSON shape).
- Use `System.Text.Json` attributes where needed (`[JsonPropertyName]`).
- Nullable enabled — use `required` for mandatory properties.
