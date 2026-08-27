---
name: "Feature Development"
description: "Use when creating or refactoring features, vertical slices, DDD modules, clean architecture layers, application contracts, feature docs, and feature tests in this repository."
applyTo: "sources/Features/**,tests/**"
---
# Feature Development Guidelines

- Build each feature as one Razor project under `sources/Features/<FeatureName>/`, with `Domain`, `Application`, `Infrastructure`, and `Presentation` folders inside it. Do not create projects per layer.
- Keep the app shell thin: `sources/Boodschap/Components/` is for composition only.
- Features must not reference another feature's `Infrastructure` or `Presentation` namespaces. Keep cross-feature references explicit, acyclic, and limited to application/domain contracts.
- Keep `Domain` free from Blazor, EF Core, and infrastructure details.
- Put orchestration and use-case behavior behind `Application` contracts.
- Put EF Core, SQLite, migrations, and external integration details in `Infrastructure`.
- Move code into `sources/Shared/` only when multiple features need it and it contains no feature-specific business language.
- Use English for source code, identifiers, comments, documentation, and tests. User-facing UI text must be Dutch through .NET localization resources; only Dutch resources are implemented for now.
- Add or update a feature-local `FEATURE.md` when a feature's scope, invariants, routes, or dependencies change.
- Keep tests aligned with the feature boundary rather than the old layer-first structure.
- Validate with focused tests first, then `dotnet build`, and smoke test visible behavior changes.