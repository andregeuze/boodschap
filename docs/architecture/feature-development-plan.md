# Feature Development Plan

## Goal

Make every feature easy for both humans and agents to understand, change, test, and eventually extract.

## Default Workflow For A New Feature

1. Create `Features/<FeatureName>/`.
2. Add `Domain/` for the feature language and rules.
3. Add `Application/` for use cases and contracts.
4. Add `Infrastructure/` for EF, repositories, seed/init, and configuration.
5. Add `Presentation/Pages/` and `Presentation/Components/` for UI.
6. Add tests that mirror the feature boundary.
7. Add a `FEATURE.md` describing scope, routes, invariants, and integration points.

## Required Folder Shape

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

## Rules For Future Features

### Domain

- Use the feature's own vocabulary.
- Keep business rules close to the feature.
- Avoid leaking EF, Blazor, or infrastructure concerns into domain types.

### Application

- Expose use cases through interfaces.
- Keep orchestration here.
- Introduce command/query handlers when a feature becomes busy enough to justify them.

### Infrastructure

- Implement persistence and external-system access here.
- Keep configuration helpers here.
- Depend inward on Application and Domain.

### Presentation

- Keep pages and feature-local components inside the feature.
- Keep Tailwind styling in markup by default.
- Add feature-local `.razor.css` only when it improves clarity.

### Shared

- Move something into `Shared/` only after a second feature needs it.
- Never move feature business rules into `Shared/`.

## Agent-Friendly Working Model

The repository should support one Feature Builder agent later, not one agent per feature.

That future setup should look like this:

1. one workspace-level agent for creating and extending features
2. one `FEATURE.md` per feature for local context
3. optional feature-scoped instructions when a feature has special rules

This keeps agent behavior consistent while preserving bounded context per feature.

## Definition Of Done For A Feature

A feature is complete when:

- folder boundaries are respected
- routes and UI live in the feature
- application contracts are explicit
- infrastructure is hidden behind those contracts
- shared code is minimal
- documentation is updated
- tests cover the main feature flow

## Near-Term Backlog For Boodschap

1. Add Shopping Lists feature tests.
2. Add `FEATURE.md` under `Features/ShoppingLists/`.
3. Introduce command/query separation if Shopping Lists gains more workflows.
4. Apply the same template to the next real feature instead of expanding shared folders.