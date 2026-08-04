# Claude Code Instructions

## Project

This repository contains the 10-day MVP for the ERP Delivery Prediction System.

The system predicts manufacturing order delivery time using:

- Rule-Based prediction
- Critical Path Method
- AI prediction
- Final Hybrid prediction
- Read-only Mock ERP integration

## Source of Truth

The authoritative architecture document is:

`docs/SAD-v1.2.md`

Do not invent or change architectural decisions.

Consult the SAD only when the current task requires architectural, database, API, prediction, security, or integration details. Do not repeatedly summarize the entire document unless explicitly requested.

## Core Architecture

- Modular Monolith with lightweight Clean Architecture
- ASP.NET Core Web API on .NET 8
- React + Vite + TypeScript frontend
- PostgreSQL application database
- Python + FastAPI AI prediction service
- Separate read-only Mock ERP API
- Docker Compose with five services

## Dependency Rules

- Domain has no external dependencies.
- Application depends on Domain.
- Integration depends on Application.
- Persistence depends on Application.
- Infrastructure implements technical concerns.
- API is the composition root.
- Concrete implementations are registered only in App.Api.
- HTTP, EF Core, FastAPI and infrastructure details must not leak into Domain.

## Prediction Rules

- Rule-Based + CPM is the primary prediction provider.
- AI is a secondary independent provider.
- AI features are built only from raw PredictionContext snapshot data.
- AI must not depend on Rule-Based intermediate results.
- AI failure must not prevent Rule-Based fallback.
- Final Hybrid combination belongs to Application, not Domain.
- Internal lead-time calculations use working minutes.
- Delivery dates are calculated by the C# WorkingCalendar service.

## ERP Rules

- ERP data is read-only.
- ERP access goes through `IErpDataProvider`.
- Mock-specific types must remain inside Integration.
- Do not create ERP CRUD tables or ERP master-data write operations.

## MVP Constraints

Do not introduce the following unless explicitly approved:

- MediatR
- AutoMapper
- Generic Repository
- Separate Unit of Work
- Hangfire or Quartz
- SignalR
- Event Sourcing
- Microservices beyond the explicitly defined Mock ERP and AI services
- APS or finite-capacity optimization
- Additional architectural layers or abstractions

Prefer the simplest implementation that satisfies the current Linear task and the SAD.

## Working Method

Before changing files:

1. Read the current Linear task.
2. Inspect only the relevant files.
3. Identify blocking dependencies.
4. Explain the planned changes briefly.
5. Modify only files required by the task.
6. Build and run relevant tests.
7. Report changed files, test results and remaining risks.

Do not commit, push, create branches, install packages or modify unrelated files without explicit approval.

## Git Conventions

Branch flow:

`feature/T-xxx-short-description` → `develop` → `main`

Commit prefixes:

- `feat:`
- `fix:`
- `test:`
- `docs:`
- `refactor:`
- `chore:`

Each task should have one primary owner. Reviewers may assist, but multiple developers should not independently modify the same architectural core files at the same time.