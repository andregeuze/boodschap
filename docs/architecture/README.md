# Architecture Overview

This folder contains the canonical architecture reference for Boodschap.

Boodschap is a feature-first, project-based modular monolith built with Blazor Server on .NET 10. Each feature has a compile-time assembly boundary while the system remains one deployable application and one runtime process.

## Core Decisions

- Keep one Blazor Server Web host, not micro-frontends or multiple deployables.
- Give each business feature one Razor Class Library containing its `Domain`, `Application`, `Infrastructure`, and `Presentation` folders. Do not create projects per layer.
- Keep `sources/Shared/` as a Razor Class Library limited to cross-feature technical building blocks; Shared references no feature project.
- Persist to one SQLite database file while letting each feature own its own EF Core `DbContext` and migrations history table.
- Keep host startup thin by composing features through module extension methods in `sources/Boodschap/Program.cs`.
- Keep feature-specific rules, routes, and invariants in feature-local `FEATURE.md` files.

## Current Repository Shape

```text
sources/
  Boodschap/
    Boodschap.csproj
    Program.cs
    Components/
    Styles/
    wwwroot/
  Features/
    Authentication/
      Boodschap.Features.Authentication.csproj
      Application/
      Domain/
      Infrastructure/
      Presentation/
    ShoppingLists/
      Boodschap.Features.ShoppingLists.csproj
      Application/
      Domain/
      Infrastructure/
      Presentation/
    Nutrition/
      Boodschap.Features.Nutrition.csproj
      Application/
      Domain/
      Infrastructure/
      Presentation/
    Recipes/
      Boodschap.Features.Recipes.csproj
      Application/
      Domain/
      Infrastructure/
      Presentation/
    Updates/
      Boodschap.Features.Updates.csproj
      Application/
      Domain/
      Infrastructure/
      Presentation/
  Shared/
    Boodschap.Shared.csproj
    Infrastructure/
    Presentation/
    Realtime/
  Directory.Build.props
tests/
  Boodschap.Tests/
  Boodschap.Features.*.Tests/
```

## System Composition

`sources/Boodschap/Program.cs` is the composition root.

At startup the host:

1. Normalizes the `ConnectionStrings:Boodschap` value through `SqliteConnectionStringResolver` so the app accepts either a full SQLite connection string or a raw database file path.
2. Registers the Authentication feature through `AddAuthenticationFeature(sqliteConnectionString)`.
3. Registers the Shopping Lists feature through `AddShoppingListsFeature(sqliteConnectionString)`.
4. Registers Nutrition, Recipes, and Updates through their feature module extension methods and applies their configured feature flags.
5. Runs the Authentication, Shopping Lists, and enabled Nutrition initializers before serving requests.
6. Maps authentication endpoints separately, then maps the Blazor app shell with `InteractiveServer` render mode.
7. Registers every feature assembly for Razor endpoint discovery; `Components/Routes.razor` registers the same assemblies with the Blazor router.

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

The allowed project dependency graph is:

```text
Boodschap (Web host) -> Shared + all features
Authentication      -> Shared
ShoppingLists       -> Shared
Updates             -> Shared
Nutrition           -> Shared + Authentication.Application
Recipes             -> Shared + Nutrition.Application/Domain
Shared              -> no features
```

Architecture tests in `tests/Boodschap.Tests/` enforce this graph and the internal layer direction. Cross-feature references must remain explicit and acyclic.

`sources/Shared/` is not a second application layer. It is an independent project for cross-feature technical pieces that do not carry feature-specific business language.

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

Feature-specific details live in `sources/Features/Authentication/FEATURE.md`.

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

Feature-specific details live in `sources/Features/ShoppingLists/FEATURE.md`.

## Shared Cross-Feature Pieces

`sources/Shared/` currently holds technical building blocks used across feature boundaries:

- `sources/Shared/Infrastructure/Persistence/SqliteConnectionStringResolver.cs` for normalized SQLite connection handling
- `sources/Shared/Realtime/StoreChangeNotifier.cs` for cross-circuit refresh notifications
- shared presentation components that are not specific to a single feature
- shared localization resources and the `AppStrings` marker

If code contains business language that belongs to one feature, it should stay in that feature instead of moving to `sources/Shared/`.

## Persistence Model

The application uses one SQLite database file, but persistence is sliced by feature.

- Authentication uses its own `AuthenticationDbContext`.
- Shopping Lists uses its own `BoodschapDbContext`.
- Nutrition uses its own `NutritionDbContext` when the feature is enabled.
- Each feature owns its EF Core migrations history table.
- The intended design does not rely on the default `__EFMigrationsHistory` table.

Current migration-history ownership:

- Authentication: `__AuthenticationMigrationsHistory`
- Shopping Lists: `__ShoppingListsMigrationsHistory`
- Nutrition: `__NutritionMigrationsHistory`

This keeps schema evolution scoped to the feature that owns it, even though both features share the same SQLite file.

The authentication store also persists the ASP.NET Core data-protection key ring in the `DataProtectionKeys` table, which allows existing valid cookies and antiforgery payloads to survive app restarts or redeployments as long as the SQLite database is retained.

## UI And Runtime Choices

- The app shell lives under `sources/Boodschap/Components/` and should remain free of feature behavior.
- The app uses Blazor Server with `InteractiveServer` render mode.
- Tailwind CSS is authored in `sources/Boodschap/Styles/app.tailwind.css` and built into `sources/Boodschap/wwwroot/app.css`. Its content configuration scans the host, Shared, and every feature project.
- Shopping-list interactivity should stay in C# when Blazor event handlers are sufficient.
- The app is expected to run behind a reverse proxy, and forwarded headers are enabled in `sources/Boodschap/Program.cs`.

## Tests And Documentation

- Feature tests live under `tests/` and mirror the feature boundary.
- Host composition and architecture tests live under `tests/Boodschap.Tests/`.
- `tests/Boodschap.Features.Authentication.Tests/` covers authentication behavior.
- `tests/Boodschap.Features.ShoppingLists.Tests/` covers shopping-list behavior.
- Each feature should keep its `FEATURE.md` current when routes, invariants, or ownership change.

## How To Extend The Architecture

When adding or refactoring a feature:

1. Create one `sources/Features/<FeatureName>/Boodschap.Features.<FeatureName>.csproj` Razor project.
2. Keep business language inside that feature's `Domain` and `Application` layers.
3. Hide persistence and external integration details in the feature's `Infrastructure` layer.
4. Keep feature UI in `Presentation/Pages/` and `Presentation/Components/`.
5. Reference Shared and approved feature application/domain APIs only; never reference another feature's Infrastructure or Presentation.
6. Register routable feature assemblies in both `sources/Boodschap/Program.cs` and `sources/Boodschap/Components/Routes.razor`.
7. Add or update the feature's `FEATURE.md` and add focused tests in the matching feature test project.
8. Move code to `sources/Shared/` only after a second feature truly needs the same technical building block.

If a feature eventually needs stronger isolation, preserve the existing boundary first and extract from the route boundary later. The current architecture is designed to allow that path without paying the complexity cost today.