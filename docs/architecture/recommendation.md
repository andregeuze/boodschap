# Recommendation

## Decision

Use a feature-first modular monolith that is extraction-ready, not true micro-frontends today.

For this Blazor Server app, the modern move is to structure the repository around business features and keep each feature internally layered with Domain, Application, Infrastructure, and Presentation. That gives you most of the operational benefits you want now:

- clear ownership boundaries
- smaller change surfaces
- easier onboarding
- safer agent-driven work
- a clean path toward later extraction

True micro-frontends are not the right first step here. They add deployment, composition, routing, styling, state-sharing, and observability complexity that this codebase does not currently need.

## Why This Fits Boodschap

The host is already relatively small. The real coupling is inside the shopping-lists flow, where page components and the data service currently own presentation, orchestration, and persistence concerns at the same time.

The right next step is:

1. Keep one deployable app.
2. Split by feature first.
3. Add clean architectural boundaries inside each feature.
4. Extract only when a feature proves it needs separate deployment or runtime isolation.

## Architectural Principles

### 1. Feature-first before layer-first

Repository structure should answer: "Where does Shopping Lists live?" not "Where do pages live?"

### 2. Domain-driven boundaries

Each feature owns its language, rules, entities, and use cases. Shared code stays small and technical.

### 3. Onion/Clean dependencies

Dependencies flow inward:

- Presentation depends on Application
- Application depends on Domain
- Infrastructure depends on Application and Domain
- Domain depends on nothing outside itself

### 4. Shared is a last resort

Only move code into `Shared/` when at least two features genuinely need it. Do not create a second dumping ground after removing `Data/` and `Components/` coupling.

### 5. Extractability over premature distribution

If a feature later needs stronger isolation, extract it into a Razor Class Library or a separate frontend package at the route boundary. Design for that path now, but do not pay the runtime cost before it is justified.

## Micro-frontends Guidance

For this repo, the practical roadmap is:

- now: modular monolith with feature-first slices
- next: route-level modules with clean feature seams
- later: Razor Class Libraries for independently evolved UI modules
- only then: separate deployable frontends if team structure or release cadence demands it

## Agent Strategy

Do not create one agent per feature.

Create one workspace-level Feature Builder agent later, and pair it with feature-local documentation and instructions. That keeps the agent model simple while giving each feature its own bounded context.

Each feature should eventually own:

- a `FEATURE.md` with invariants and scope
- its own folders for Domain, Application, Infrastructure, and Presentation
- its own tests
- minimal dependencies on `Shared/`