using System.Reflection;
using Boodschap.Features.Authentication;
using Boodschap.Features.Nutrition;
using Boodschap.Features.Recipes;
using Boodschap.Features.ShoppingLists;
using Boodschap.Features.Updates;
using Boodschap.Shared.Localization;
using NetArchTest.Rules;

namespace Boodschap.Tests;

public sealed class ArchitectureTests
{
	private static readonly FeatureAssembly[] FeatureAssemblies =
	[
		new(typeof(AuthenticationModule).Assembly, "Boodschap.Features.Authentication", []),
		new(typeof(ShoppingListsModule).Assembly, "Boodschap.Features.ShoppingLists", []),
		new(typeof(NutritionModule).Assembly, "Boodschap.Features.Nutrition", ["Boodschap.Features.Authentication"]),
		new(typeof(RecipesModule).Assembly, "Boodschap.Features.Recipes", ["Boodschap.Features.Nutrition"]),
		new(typeof(UpdatesModule).Assembly, "Boodschap.Features.Updates", [])
	];

	[Fact]
	public void Shared_DoesNotReferenceFeatures()
	{
		var featureReferences = typeof(AppStrings).Assembly
			.GetReferencedAssemblies()
			.Select(reference => reference.Name)
			.Where(name => name?.StartsWith("Boodschap.Features.", StringComparison.Ordinal) == true);

		Assert.Empty(featureReferences);
	}

	[Fact]
	public void Features_HaveOnlyApprovedCrossFeatureReferences()
	{
		foreach (var feature in FeatureAssemblies)
		{
			var actualReferences = feature.Assembly
				.GetReferencedAssemblies()
				.Select(reference => reference.Name)
				.Where(name => name?.StartsWith("Boodschap.Features.", StringComparison.Ordinal) == true)
				.Cast<string>()
				.Order(StringComparer.Ordinal)
				.ToArray();

			Assert.Equal(feature.AllowedFeatureReferences.Order(StringComparer.Ordinal), actualReferences);
		}
	}

	[Fact]
	public void FeatureLayers_HaveOneWayDependencies()
	{
		foreach (var feature in FeatureAssemblies)
		{
			AssertSuccessful(Types.InAssembly(feature.Assembly)
				.That()
				.ResideInNamespaceStartingWith($"{feature.Namespace}.Domain")
				.ShouldNot()
				.HaveDependencyOnAny(
					$"{feature.Namespace}.Application",
					$"{feature.Namespace}.Infrastructure",
					$"{feature.Namespace}.Presentation")
				.GetResult());

			AssertSuccessful(Types.InAssembly(feature.Assembly)
				.That()
				.ResideInNamespaceStartingWith($"{feature.Namespace}.Application")
				.ShouldNot()
				.HaveDependencyOnAny(
					$"{feature.Namespace}.Infrastructure",
					$"{feature.Namespace}.Presentation")
				.GetResult());
		}
	}

	[Fact]
	public void Features_DoNotUseOtherFeatureInfrastructureOrPresentation()
	{
		foreach (var feature in FeatureAssemblies)
		{
			var forbiddenNamespaces = FeatureAssemblies
				.Where(other => other.Namespace != feature.Namespace)
				.SelectMany(other => new[]
				{
					$"{other.Namespace}.Infrastructure",
					$"{other.Namespace}.Presentation"
				})
				.ToArray();

			AssertSuccessful(Types.InAssembly(feature.Assembly)
				.That()
				.ResideInNamespaceStartingWith(feature.Namespace)
				.ShouldNot()
				.HaveDependencyOnAny(forbiddenNamespaces)
				.GetResult());
		}
	}

	private static void AssertSuccessful(TestResult result)
	{
		Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
	}

	private sealed record FeatureAssembly(
		Assembly Assembly,
		string Namespace,
		IReadOnlyCollection<string> AllowedFeatureReferences);
}