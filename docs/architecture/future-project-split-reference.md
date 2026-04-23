# Future Reference: Good Next Project Split

## Status

This document is reference material only.

It does not replace the current recommendation for Boodschap, and it should not be treated as the next required architectural step. The current default remains the single-project modular monolith described in the existing architecture docs.

This split becomes interesting only if the repository grows enough that compile-time boundaries would provide real value.

## Proposed Next Split

If Boodschap benefits from a first project split later, the pragmatic next shape is:

- `Boodschap.Web`
- `Boodschap.ShoppingLists`

This is intentionally a small split.

It creates one hard boundary between the host and the first real feature without exploding the solution into one project per layer.

## Why This Is The Right First Split

This split gives the repo the main benefits of multi-project boundaries without paying too much ceremony too early.

Benefits:

- the host becomes clearly responsible for composition and startup only
- the Shopping Lists feature gains a hard compile-time boundary
- feature extraction later becomes much easier
- dependency direction becomes more obvious
- tests can target the feature project directly

It avoids the downsides of an over-split setup:

- no separate `Domain`, `Application`, and `Infrastructure` projects yet
- no premature `Shared` project
- no complex cross-project coordination for a still-small app

## Target Solution Shape

```text
src/
  Boodschap.Web/
  Boodschap.ShoppingLists/

tests/
  Boodschap.Features.ShoppingLists.Tests/
```

## Project Responsibilities

### `Boodschap.Web`

This project is the app host.

It owns:

- ASP.NET Core startup and host wiring
- Blazor app shell
- root routing composition
- layout components
- Tailwind build pipeline
- static assets and host-level CSS output
- configuration files
- containerization files
- future feature composition

Typical contents:

```text
Boodschap.Web/
  Components/
    App.razor
    Routes.razor
    Layout/
    Pages/
      Error.razor
      NotFound.razor
  Styles/
  wwwroot/
  Program.cs
  appsettings.json
  appsettings.Development.json
  package.json
  tailwind.config.js
  Dockerfile
  Boodschap.Web.csproj
```

### `Boodschap.ShoppingLists`

This project is the first feature module.

It should be a Razor Class Library so the feature can own both its UI and its internal layers.

It owns:

- Shopping Lists domain types
- Shopping Lists application contracts and services
- Shopping Lists persistence
- Shopping Lists Razor pages and components
- Shopping Lists seed/init logic
- Shopping Lists migrations
- feature-local documentation

Typical contents:

```text
Boodschap.ShoppingLists/
  Application/
    Contracts/
    Services/
  Domain/
  Infrastructure/
    Persistence/
      Migrations/
  Presentation/
    Components/
    Pages/
  FEATURE.md
  Boodschap.ShoppingLists.csproj
```

## Exact Move Map

### Move Into `Boodschap.Web`

Keep these in the host project:

- `Program.cs`
- `Components/App.razor`
- `Components/Routes.razor`
- `Components/Layout/**`
- `Components/Pages/Error.razor`
- `Components/Pages/NotFound.razor`
- `Styles/**`
- `wwwroot/**`
- `appsettings.json`
- `appsettings.Development.json`
- `package.json`
- `tailwind.config.js`
- `Dockerfile`

### Move Into `Boodschap.ShoppingLists`

Move these into the feature project:

- `Features/ShoppingLists/**`
- `Features/ShoppingLists/FEATURE.md`

### Re-home Current `Shared/` Code

The current `Shared/` surface is still mostly Shopping Lists support code.

For the first split, move these into `Boodschap.ShoppingLists` instead of preserving them as a separate shared project:

- `Shared/Presentation/Components/TabBar.razor`
- `Shared/Realtime/StoreChangeNotifier.cs`

Reason:

- `TabBar` is currently only used by Shopping Lists
- `StoreChangeNotifier` currently exists to support Shopping Lists behavior
- keeping them in a separate project now would create more structure than value

Suggested destination:

```text
Boodschap.ShoppingLists/
  Presentation/
    Components/
      TabBar.razor
  Realtime/
    StoreChangeNotifier.cs
```

If another real feature later needs them, then revisit whether they should move into a true shared project.

## Dependency Rule

The dependency rule should become:

```text
Boodschap.Web -> Boodschap.ShoppingLists
Boodschap.ShoppingLists -> no reference to Boodschap.Web
tests -> Boodschap.ShoppingLists
```

The feature project must never depend on the host project.

## Composition Model

The feature should continue to expose one clear composition entry point.

Recommended shape:

- `Boodschap.ShoppingLists` exposes `AddShoppingListsFeature(...)`
- `Boodschap.Web` calls that extension from `Program.cs`
- `Boodschap.Web` passes the resolved SQLite connection string into the feature registration
- `Boodschap.Web` remains responsible for forwarded headers, host startup, and render mode setup

## Routing Model

The Shopping Lists feature should continue to own its routes:

- `/`
- `/lists/{id}`

Because those pages would live in a Razor Class Library, the host router should include the feature assembly as an additional route source.

That keeps route ownership with the feature while the host remains thin.

## Persistence Ownership

The SQLite persistence for Shopping Lists should move with the feature.

That includes:

- `BoodschapDbContext`
- repository implementations
- configuration helpers
- seed/init logic
- EF Core migrations

This is a good boundary because the persistence is already feature-specific.

## Tailwind And Static Asset Considerations

If the split happens, the Tailwind pipeline should still stay in `Boodschap.Web`.

That means `tailwind.config.js` in the host must scan both projects.

Conceptually, the content globs would need to include:

- host Razor files
- Shopping Lists Razor files inside the feature project

This is important because moving Razor markup into a separate project without updating Tailwind scanning would recreate the styling regression that already happened once during the folder refactor.

## Test Shape After The Split

The current test project can stay where it is:

```text
tests/
  Boodschap.Features.ShoppingLists.Tests/
```

Recommended references:

- feature tests reference `Boodschap.ShoppingLists`
- future host or integration tests reference `Boodschap.Web` only when full app startup is the thing being tested

That keeps most tests focused on the feature boundary rather than the full host.

## What Not To Split Yet

Even after this first split, avoid doing these things immediately:

- do not create `Boodschap.Domain`
- do not create `Boodschap.Application`
- do not create `Boodschap.Infrastructure`
- do not create `Boodschap.Shared` yet
- do not split Shopping Lists into multiple projects internally

That would add ceremony before the repository has enough scale to justify it.

## Why Not A Separate Shared Project Yet

A separate shared project becomes worth it only when at least two features need the same code and that code is genuinely cross-cutting.

Right now, creating a shared project would mostly move Shopping Lists support code into a neutral-sounding container.

That would weaken feature ownership instead of clarifying it.

## When This Split Becomes Justified

This split is worth doing when one or more of these become true:

- a second substantial feature exists
- compile-time boundaries would prevent recurring dependency leaks
- Shopping Lists needs to evolve more independently from the host
- feature ownership is split across people or teams
- extraction into a route-level module becomes a real possibility

Until then, the current single-project setup remains the better tradeoff.

## Migration Sketch If This Is Ever Chosen

1. Create `src/Boodschap.Web`.
2. Create `src/Boodschap.ShoppingLists` as a Razor Class Library.
3. Move all Shopping Lists code into the feature project.
4. Move current Shopping Lists-only shared code into the feature project.
5. Keep the host project limited to app shell, startup, styling pipeline, and composition.
6. Update the host router to include the Shopping Lists assembly.
7. Update Tailwind content globs to scan both projects.
8. Point feature tests at `Boodschap.ShoppingLists`.
9. Keep the solution otherwise operationally identical.

## Bottom Line

If Boodschap ever moves beyond the current single-project modular monolith, the right next split is:

- one host project
- one feature project for Shopping Lists

That is the smallest split that adds meaningful structure without turning the codebase into project-per-layer overhead.
