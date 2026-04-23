# Rollout

## Phase 1: Establish Structural Boundaries

Completed in this refactor:

- create `docs/architecture/`
- introduce `Features/ShoppingLists/`
- move shared UI into `Shared/`
- move realtime notification into `Shared/Realtime`
- move SQLite persistence into feature infrastructure
- reduce `Program.cs` to composition and startup wiring

## Phase 2: Stabilize The First Vertical Slice

Next work after this refactor:

1. Add focused tests around shopping-list use cases.
2. Add a `FEATURE.md` for Shopping Lists with business rules and invariants.
3. Split the application layer into separate command/query handlers if the feature grows.
4. Keep all new shopping-list UI inside the feature presentation folder.

## Phase 3: Standardize New Features

When adding a new feature:

1. Create the feature folder from the template.
2. Add domain entities and terms first.
3. Add application contracts and use cases.
4. Add infrastructure implementations.
5. Add presentation pages and components.
6. Register the feature in the host through a single module extension.
7. Add feature tests and a `FEATURE.md`.

## Phase 4: Reduce Shared Surface Area

Guard against `Shared/` becoming a new miscellaneous folder.

Move code into `Shared/` only when:

- it is used by multiple features
- it contains no feature-specific business language
- its ownership is genuinely cross-cutting

## Phase 5: Prepare For Extraction

When a feature becomes large enough, make it extraction-ready by:

1. keeping host wiring thin
2. avoiding direct access to another feature's infrastructure
3. isolating feature docs, tests, contracts, and presentation
4. reducing static cross-feature assumptions

## Phase 6: Consider Micro-frontends Only With Real Triggers

Adopt route-level independent modules only if at least one of these is true:

- different teams need separate release cadence
- the UI surface becomes independently deployable
- runtime isolation has real operational value
- the host app becomes a bottleneck for change velocity

Until then, keep the modular monolith.