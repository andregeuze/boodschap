---
name: "Feature Development"
description: "Use when creating or refactoring features, vertical slices, DDD modules, clean architecture layers, application contracts, feature docs, and feature tests in this repository."
applyTo:
  - "Features/**"
  - "tests/**"
---
# Feature Development Guidelines

- Build features under `Features/<FeatureName>/` with `Domain`, `Application`, `Infrastructure`, and `Presentation` folders.
- Keep the app shell thin: `Components/` is for composition only.
- Keep `Domain` free from Blazor, EF Core, and infrastructure details.
- Put orchestration and use-case behavior behind `Application` contracts.
- Put EF Core, SQLite, migrations, and external integration details in `Infrastructure`.
- Move code into `Shared/` only when multiple features need it and it contains no feature-specific business language.
- Add or update a feature-local `FEATURE.md` when a feature's scope, invariants, routes, or dependencies change.
- Keep tests aligned with the feature boundary rather than the old layer-first structure.
- Validate with focused tests first, then `dotnet build`, and smoke test visible behavior changes.