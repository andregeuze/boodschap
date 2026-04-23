namespace Boodschap.Features.ShoppingLists.Domain;

public sealed class ShoppingList
{
	public int Id { get; set; }
	public required string Name { get; set; }
	public string Description { get; set; } = string.Empty;
	public bool Archived { get; set; }
	public int SortOrder { get; set; }
	public List<ShoppingListItem> Items { get; set; } = [];
}