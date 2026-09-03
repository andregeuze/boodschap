# Shopping Lists Feature

## Purpose

Shopping Lists is the primary user-facing feature in Boodschap. It owns grocery-list creation, list naming, list browsing, archiving, item management, drag-and-drop ordering, and realtime refresh across connected Blazor Server sessions.

Shopping Lists renders only for authenticated users and depends on the Authentication feature for route protection.

## Owned Surface

### Routes

- `/`
- `/lists/{id}`

### Mobile API And Realtime

- `GET /api/shopping-lists`
- `GET /api/shopping-lists/{listId}`
- `POST /api/shopping-lists`
- `PUT /api/shopping-lists/{listId}`
- `POST /api/shopping-lists/{listId}/archive`
- `POST /api/shopping-lists/{listId}/unarchive`
- `DELETE /api/shopping-lists/{listId}`
- `POST /api/shopping-lists/{listId}/items`
- `PUT /api/shopping-lists/{listId}/items/{itemId}/name`
- `PUT /api/shopping-lists/{listId}/items/{itemId}/purchased`
- `DELETE /api/shopping-lists/{listId}/items/{itemId}`
- `PUT /api/shopping-lists/{listId}/items/{itemId}/order`
- `/hubs/store-changes` authenticated SignalR hub

All API and hub routes explicitly require the mobile bearer scheme. JSON DTOs are feature-owned application contracts; EF entities are never serialized. `HttpShoppingListService` maps transport DTOs to new domain models for the native client.

### Copilot MCP

- `/mcp` Streamable HTTP endpoint
- `list_shopping_lists` lists shopping lists and their current items
- `create_shopping_list` creates a list with an optional description and initial items
- `add_shopping_list_item` adds an item to an existing shopping list
- `remove_shopping_list_item` removes an item from an existing shopping list

MCP uses its own `BoodschapMcp` access-key authentication scheme. It never accepts browser cookies or mobile bearer tokens. The endpoint fails closed unless `Mcp:AccessKey` is configured, and tools orchestrate only through `IShoppingListService`.

### Presentation

- `Presentation/Pages/Home.razor`
- `Presentation/Pages/ShoppingListPage.razor`

### Application

- `IShoppingListService`
- `IShoppingListRepository`
- `ShoppingListService`
- API request/response contracts under `Application/Contracts/Api/`

### Infrastructure

- `BoodschapDbContext`
- `ShoppingListRepository`
- `ShoppingListsInitializer`
- `Infrastructure/Remote/HttpShoppingListService`
- `Infrastructure/Persistence/Migrations/`

## Domain Language

- `ShoppingList`
- `ShoppingListItem`

Keep these terms inside the feature. Do not move them into `sources/Shared/`.

## Architectural Boundary

This feature follows Onion/Clean Architecture:

- `Presentation` depends on `Application`
- `Application` depends on `Domain`
- `Infrastructure` depends on `Application` and `Domain`
- `Domain` stays free of Blazor, EF Core, and transport concerns

The app shell should only compose this feature through `sources/Boodschap/Program.cs` and `ShoppingListsModule`.

## Invariants

- Shopping list routes require an authenticated user.
- Lists may only be permanently removed after they are archived.
- New items are inserted before the first purchased item when a list contains both needed and purchased items.
- List names and descriptions are user-provided at creation time and can be edited later from the overview page.
- The overview shows active lists first and archived lists in a section below them, without separate status tabs.
- Lists within each overview section are ordered by their most recent successful list or item update.
- Item names are user-provided and can be renamed inline from the list page without leaving the current list.
- Marking an item as purchased moves it to the end of the list.
- Drag-and-drop reordering is available when no item is being renamed inline.
- Drag-and-drop stays implemented in Blazor C# event handlers; do not add JavaScript for it.
- Realtime updates are published through `StoreChangeNotifier` from the application layer, not from Razor pages.
- `StoreChangeBroadcastService` subscribes once to `StoreChangeNotifier` and broadcasts successful mutations to mobile clients. It never republishes received client events, so notifications cannot loop.
- The MAUI SignalR client translates `StoreChanged` messages into its process-local `StoreChangeNotifier`, allowing native view models to refresh from the same notification contract.
- Native realtime refreshes are coalesced, update only the visible overview or relevant open list, and never enable the global busy overlay.
- SQLite persistence stays behind feature-level contracts.
- Android uses only `HttpShoppingListService`; it does not register the SQLite repository, DbContext, or initializer.
- MCP tools use `IShoppingListService`; they do not access repositories or EF Core directly.

## Integration Points

- Authentication gate and current user context: `sources/Features/Authentication/`
- Shared realtime notifications: `sources/Shared/Realtime/StoreChangeNotifier.cs`
- Host composition: `sources/Boodschap/Program.cs`
- Native composition: `sources/Boodschap.Mobile/MauiProgram.cs`

## Test Strategy

Feature tests live under `tests/Boodschap.Features.ShoppingLists.Tests/`.

Current focus:

- application-layer notification behavior
- API endpoint HTTP semantics and DTO mapping
- remote HTTP adapter request/response behavior
- notifier-to-SignalR broadcast behavior
- repository ordering and mutation semantics
- feature invariants that should survive refactors

When adding a new use case, extend tests in the same feature test project before broadening `sources/Shared/` or host-level wiring.

## Evolution Rules

- Prefer adding a new application use case before expanding page code-behind logic.
- If the service surface grows significantly, split into commands and queries.
- Keep shopping-list migrations isolated in the feature-owned history table `__ShoppingListsMigrationsHistory`; do not rely on the default EF Core history table.
- Keep feature-specific docs in this file updated whenever routes, invariants, or ownership change.
- Keep API handlers orchestrating through `IShoppingListService`; do not move notifications into endpoints, hubs, pages, or repositories.