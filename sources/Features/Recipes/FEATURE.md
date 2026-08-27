# Recipes

## Scope

Recipes owns recipe-search requests, suggestions, the n8n integration, and the `/recipes` user interface.

## Architectural Boundary

The feature is one Razor project containing its Domain, Application, Infrastructure, and Presentation layers. It references Nutrition's application/domain API to obtain food names for ingredient suggestions. It must not reference Nutrition infrastructure or presentation.

The Web host composes the feature through `RecipesModule`, registers its assembly for Razor route discovery, and controls navigation and route availability through `RecipeFeatureOptions`.

## Test Strategy

Feature behavior lives in `tests/Boodschap.Features.Recipes.Tests/`. App-shell navigation behavior belongs in `tests/Boodschap.Tests/`.