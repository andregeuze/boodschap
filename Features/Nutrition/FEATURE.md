# Nutrition Feature

## Purpose

Nutrition owns locally stored NEVO food data and per-portion nutrition calculations. NEVO data is kept in SQLite rather than queried through an external API, so lookups stay fast, cheap, and available without an external service dependency.

## Owned Surface

### Routes

- `/nutrition`

### Presentation

- `Presentation/Pages/NutritionPage.razor`

### Application

- `IFoodService`
- `IFoodRepository`
- `FoodService`

### Infrastructure

- `NutritionDbContext`
- `FoodRepository`
- `NevoDetailsCsvImporter`
- `NutritionInitializer`
- `NutritionDevelopmentSeeder`
- `Infrastructure/Persistence/Migrations/`

## Domain Language

- `Food`
- `FoodPortion`

Nutrition values are stored per 100 grams. Portion values are calculated with `Food.Calculate(per100g, grams)`.
NEVO records are matched by `Food.NevoCode`; the app-owned `Food.Id` remains an internal SQLite identity.

## Import Strategy

Import NEVO as a local file, not as a runtime API dependency:

1. Download or export the NEVO dataset to a local CSV/XLSX file.
2. Parse the NEVO details CSV rows into `Food` identity fields and `FoodNutrientDetail` rows.
3. Derive kcal, protein, carbohydrates, fat, and fiber from their matching nutrient detail rows.
4. Normalize Dutch decimal values before writing decimals to SQLite.
5. Upsert foods by Nevocode so the same export can be imported repeatedly without duplicate foods.
6. Keep the raw source file outside the web root; do not fetch NEVO online during normal app usage.

The detailed NEVO 2025 v9.0 CSV is checked in as a Nutrition test fixture at `tests/Boodschap.Features.Nutrition.Tests/Fixtures/NEVO2025_v9.0_Details.csv` and is parsed by `NevoDetailsCsvImporter`.

`Food` stores the NEVO food identity and grouping fields from the details export. Every nutrient row from the details export is modeled as a `FoodNutrientDetail` with group names, component names, raw value, parsed decimal value, unit, trace/fortified marker, source code, and reference.

## Architectural Boundary

This feature follows Onion/Clean Architecture:

- `Presentation` depends on `Application`
- `Application` depends on `Domain`
- `Infrastructure` depends on `Application` and `Domain`
- `Domain` stays free of Blazor, EF Core, and transport concerns

The app shell composes this feature through `Program.cs` and `NutritionModule`.

## Invariants

- Nutrition routes require an authenticated user.
- Food nutrient values are stored per 100 grams.
- `Food.NevoCode` is the stable external key for NEVO imports.
- `FoodNutrientDetail.NutrientCode` is not unique per food; the NEVO details export can repeat a code in different nutrient groups, and those rows must be preserved.
- Portion calculations use `per100g / 100m * grams`.
- SQLite persistence stays behind feature-level contracts.
- NEVO data should be imported into the local database by an admin rather than fetched from a live API at runtime.
- Development startup seeds five basic food articles when the nutrition database is empty.
- The `/nutrition` page exposes the NEVO details CSV upload only to admin users.

## Test Strategy

Feature tests live under `tests/Boodschap.Features.Nutrition.Tests/`.

Current focus:

- per-100g portion calculation behavior
- SQLite repository read/search behavior
- feature wiring and migration safety