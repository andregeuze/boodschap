# Target Shape

## Repository Layout

```text
Components/
  App.razor
  Routes.razor
  Layout/

Features/
  ShoppingLists/
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

Shared/
  Presentation/
    Components/
  Realtime/

Styles/
docs/
  architecture/

Program.cs
Dockerfile
```

## Layer Responsibilities

### `Components/`

This is the app shell only:

- app root
- routing
- layouts
- reconnect UI

No feature behavior should be implemented here.

### `Features/<FeatureName>/Domain`

Owns the feature language and rules:

- entities
- value objects
- constants and invariants
- domain behavior when rules become richer

This layer should not know about EF Core, Blazor, or HTTP.

### `Features/<FeatureName>/Application`

Owns use cases and contracts:

- service interfaces
- repository interfaces when needed
- command/query orchestration
- mutation result types

This layer coordinates work but does not know how persistence is implemented.

### `Features/<FeatureName>/Infrastructure`

Owns technical implementation details:

- EF DbContext
- repository implementations
- migrations
- seed/init logic
- configuration helpers

This layer depends on the inner feature layers, never the other way around.

### `Features/<FeatureName>/Presentation`

Owns feature UI:

- routable pages
- feature-local components
- optional feature-local `.razor.css`

Tailwind utility styling should stay close to markup. Add a feature-local style file only when the markup becomes repetitive or you need scoped overrides.

### `Shared/`

Shared is reserved for cross-feature technical building blocks.

Current examples:

- tab control UI
- realtime change notifications

## Dependency Rule

Treat this as the non-negotiable rule:

```text
Presentation -> Application -> Domain
Infrastructure -> Application + Domain
Shared -> no feature-specific business rules
```

Feature A must not directly depend on Feature B's Presentation or Infrastructure.

## Feature Template

Every future feature should start with this shape:

```text
Features/
  <FeatureName>/
    Application/
      Contracts/
      Services/
    Domain/
    Infrastructure/
      Persistence/
    Presentation/
      Components/
      Pages/
```

Optional additions when needed:

- `Domain/ValueObjects/`
- `Application/Commands/`
- `Application/Queries/`
- `Presentation/Styles/`
- `Tests/<FeatureName>/`

## Path To Extraction

If a feature grows enough to need stronger isolation, extract in this order:

1. keep the folder and dependency boundaries intact
2. move the feature into a Razor Class Library
3. expose route-level composition from the host
4. only then consider separate deployables or true micro-frontends