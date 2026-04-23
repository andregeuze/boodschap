namespace Boodschap.Features.ShoppingLists.Domain;

public static class ShoppingItemFilters
{
	public const string All = "All";
	public const string Needed = "Needed";
	public const string Purchased = "Purchased";

	public static readonly IReadOnlyList<string> AllValues = [All, Needed, Purchased];
}