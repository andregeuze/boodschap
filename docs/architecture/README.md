# Architecture Overview

This folder contains the canonical architecture reference for Boodschap.

Boodschap is a feature-first modular monolith built with Blazor Server on .NET 10. The codebase is intentionally structured so features can evolve independently inside one deployable app, with extraction kept as a later option rather than a current runtime concern.

## Core Decisions

- Keep one Blazor Server application, not micro-frontends or multiple deployables.
- Organize code by business feature first, then by `Domain`, `Application`, `Infrastructure`, and `Presentation` inside each feature.
- Keep `Shared/` limited to cross-feature technical building blocks.
- Persist to one SQLite database file while letting each feature own its own EF Core `DbContext` and migrations history table.
- Keep host startup thin by composing features through module extension methods in `Program.cs`.
- Keep feature-specific rules, routes, and invariants in feature-local `FEATURE.md` files.

## Current Repository Shape

```text
Components/
  App.razor
  Routes.razor
  Layout/

Features/
  Authentication/
    Application/
      Contracts/
      Services/
    Domain/
    Infrastructure/
      Persistence/
    Presentation/
  ShoppingLists/
    Application/
      Contracts/
      Services/
    Domain/
    Infrastructure/
      Persistence/
    Presentation/

Shared/
  Infrastructure/
    Persistence/
  Presentation/
    Components/
  Realtime/

Styles/
tests/
Program.cs
```

## System Composition

`Program.cs` is the composition root.

At startup the host:

1. Normalizes the `ConnectionStrings:Boodschap` value through `SqliteConnectionStringResolver` so the app accepts either a full SQLite connection string or a raw database file path.
2. Registers the Authentication feature through `AddAuthenticationFeature(sqliteConnectionString)`.
3. Registers the Shopping Lists feature through `AddShoppingListsFeature(sqliteConnectionString)`.
4. Runs `AuthenticationStoreInitializer.InitializeAsync(...)` and `ShoppingListsInitializer.InitializeAsync(...)` before serving requests.
5. Maps authentication endpoints separately, then maps the Blazor app shell with `InteractiveServer` render mode.

This keeps the host responsible for composition and middleware, while feature behavior stays inside the owning feature.

## Architectural Boundaries

Dependencies are intentionally one-way inside a feature:

```text
Presentation -> Application -> Domain
Infrastructure -> Application + Domain
```

Rules that follow from that:

- `Domain` should stay free of Blazor, EF Core, and transport concerns.
- `Application` owns use cases, orchestration, and feature-facing contracts.
- `Infrastructure` implements persistence and technical adapters behind those contracts.
- `Presentation` contains routable pages and feature-local UI.
- Features must not depend directly on another feature's `Presentation` or `Infrastructure`.

`Shared/` is not a second application layer. It exists only for cross-feature technical pieces that do not carry feature-specific business language.

## Feature Ownership

### Authentication

Authentication owns:

- local username/password sign-in
- bootstrap creation of the first administrator
- authenticated account management
- route protection and auth endpoints
- password hashing and local-user persistence

Implementation choices already present in the codebase:

- cookie authentication is configured inside `AuthenticationModule`
- auth cookies use a persistent 90-day sliding expiration window rather than browser-session-only tickets, so active sessions renew while inactive ones eventually expire
- authenticated-user access is exposed through `ICurrentUserAccessor`
- EF Core persistence lives in the feature-owned `AuthenticationDbContext`
- the ASP.NET Core data-protection key ring is persisted in the authentication SQLite store in the `DataProtectionKeys` table
- anonymous self-service registration closes after the first account is created

Feature-specific details live in `Features/Authentication/FEATURE.md`.

### Shopping Lists

Shopping Lists owns:

- list creation, naming, and browsing
- archived versus active list behavior
- item add, remove, rename, purchase, and reorder flows
- filter behavior for `All`, `Needed`, and `Purchased`
- realtime refresh across connected Blazor Server sessions

Implementation choices already present in the codebase:

- drag-and-drop reordering stays in Blazor C# event handlers, not JavaScript
- application services publish refresh events through `StoreChangeNotifier`
- SQLite persistence stays behind `IShoppingListRepository` and `IShoppingListService`
- routes are available only to authenticated users

Feature-specific details live in `Features/ShoppingLists/FEATURE.md`.

## Shared Cross-Feature Pieces

`Shared/` currently holds technical building blocks used across feature boundaries:

- `Shared/Infrastructure/Persistence/SqliteConnectionStringResolver.cs` for normalized SQLite connection handling
- `Shared/Realtime/StoreChangeNotifier.cs` for cross-circuit refresh notifications
- shared presentation components that are not specific to a single feature

If code contains business language that belongs to one feature, it should stay in that feature instead of moving to `Shared/`.

## Persistence Model

The application uses one SQLite database file, but persistence is sliced by feature.

- Authentication uses its own `AuthenticationDbContext`.
- Shopping Lists uses its own `BoodschapDbContext`.
- Each feature owns its EF Core migrations history table.
- The intended design does not rely on the default `__EFMigrationsHistory` table.

Current migration-history ownership:

- Authentication: `__AuthenticationMigrationsHistory`
- Shopping Lists: `__ShoppingListsMigrationsHistory`

This keeps schema evolution scoped to the feature that owns it, even though both features share the same SQLite file.

The authentication store also persists the ASP.NET Core data-protection key ring in the `DataProtectionKeys` table, which allows existing valid cookies and antiforgery payloads to survive app restarts or redeployments as long as the SQLite database is retained.

## UI And Runtime Choices

- The app shell lives under `Components/` and should remain free of feature behavior.
- The app uses Blazor Server with `InteractiveServer` render mode.
- Tailwind CSS is authored in `Styles/app.tailwind.css` and built into `wwwroot/app.css`.
- Shopping-list interactivity should stay in C# when Blazor event handlers are sufficient.
- The app is expected to run behind a reverse proxy, and forwarded headers are enabled in `Program.cs`.

## Tests And Documentation

- Feature tests live under `tests/` and mirror the feature boundary.
- `tests/Boodschap.Features.Authentication.Tests/` covers authentication behavior.
- `tests/Boodschap.Features.ShoppingLists.Tests/` covers shopping-list behavior.
- Each feature should keep its `FEATURE.md` current when routes, invariants, or ownership change.

## How To Extend The Architecture

When adding or refactoring a feature:

1. Start with `Features/<FeatureName>/`.
2. Keep business language inside that feature's `Domain` and `Application` layers.
3. Hide persistence and external integration details in the feature's `Infrastructure` layer.
4. Keep feature UI in `Presentation/Pages/` and `Presentation/Components/`.
5. Add or update the feature's `FEATURE.md`.
6. Add focused tests in the matching feature test project.
7. Move code to `Shared/` only after a second feature truly needs the same technical building block.

If a feature eventually needs stronger isolation, preserve the existing boundary first and extract from the route boundary later. The current architecture is designed to allow that path without paying the complexity cost today.