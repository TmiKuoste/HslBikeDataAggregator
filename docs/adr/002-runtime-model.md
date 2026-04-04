# ADR-002: Use Azure Functions isolated worker model

## Status

Accepted

## Date

2026-04-04

## Context

This backend is a greenfield Azure Functions application intended to use modern .NET, constructor injection, explicit startup configuration, and current Azure Functions guidance. The service is also expected to evolve quickly as polling, storage, and HTTP APIs are implemented.

## Decision

Use the Azure Functions isolated worker model for the backend. Target `.NET 10` and keep the Functions worker packages on versions that officially support that target framework.

## Rationale

- Isolated worker is the current long-term Azure Functions model for .NET.
- It supports newer .NET versions than the in-process model.
- It provides standard host startup, dependency injection, and configuration patterns.
- It matches the repository instructions already checked into `.github/`.

## Consequences

### Positive

- Modern .NET hosting model with explicit `Program.cs` composition.
- Easier service registration and future middleware/configuration changes.
- Avoids building new functionality on the in-process model, which has a defined end of support.

### Negative

- Slightly more setup than older in-process templates.
- Package versions must track the official Functions support matrix for the chosen .NET version.
