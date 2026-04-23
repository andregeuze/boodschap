# Shopping Lists Feature

## Purpose

Shopping Lists is the primary user-facing feature in Boodschap. It owns grocery-list creation, list browsing, archiving, item management, filtering, drag-and-drop ordering, and realtime refresh across connected Blazor Server sessions.

## Owned Surface

### Routes

- `/`
- `/lists/{id}`

### Presentation

- `Presentation/Pages/Home.razor`
- `Presentation/Pages/ShoppingListPage.razor`
- `Presentation/Components/FilterBar.razor`

### Application

- `IShoppingListService`
- `IShoppingListRepository`
- `ShoppingListService`

### Infrastructure

- `BoodschapDbContext`
- `ShoppingListRepository`
- `ShoppingListsInitializer`
- `SqliteConnectionStringResolver`
- `Infrastructure/Persistence/Migrations/`

## Domain Language

- `ShoppingList`
- `ShoppingListItem`
- `ShoppingItemFilters`

Keep these terms inside the feature. Do not move them into `Shared/`.

## Architectural Boundary

This feature follows Onion/Clean Architecture:

- `Presentation` depends on `Application`
- `Application` depends on `Domain`
- `Infrastructure` depends on `Application` and `Domain`
- `Domain` stays free of Blazor, EF Core, and transport concerns

The app shell should only compose this feature through `Program.cs` and `ShoppingListsModule`.

## Invariants

- Item filter values are `All`, `Needed`, and `Purchased`, sourced from `ShoppingItemFilters`.
- New items are inserted before the first purchased item when a list contains both needed and purchased items.
- Marking an item as purchased moves it to the end of the list.
- Drag-and-drop reordering is only available in the `All` filter view.
- Drag-and-drop stays implemented in Blazor C# event handlers; do not add JavaScript for it.
- Realtime updates are published through `StoreChangeNotifier` from the application layer, not from Razor pages.
- SQLite persistence stays behind feature-level contracts.

## Integration Points

- Shared UI: `Shared/Presentation/Components/TabBar.razor`
- Shared realtime notifications: `Shared/Realtime/StoreChangeNotifier.cs`
- Host composition: `Program.cs`

## Test Strategy

Feature tests live under `tests/Boodschap.Features.ShoppingLists.Tests/`.

Current focus:

- application-layer notification behavior
- repository ordering and mutation semantics
- feature invariants that should survive refactors

When adding a new use case, extend tests in the same feature test project before broadening `Shared/` or host-level wiring.

## Evolution Rules

- Prefer adding a new application use case before expanding page code-behind logic.
- If the service surface grows significantly, split into commands and queries.
- Keep feature-specific docs in this file updated whenever routes, invariants, or ownership change.