namespace Boodschap.Data;

public sealed class ShoppingListItem
{
	public int Id { get; set; }
	public int ShoppingListId { get; set; }
	public required string Name { get; set; }
	public bool IsDone { get; set; }
	public int SortOrder { get; set; }
	public ShoppingList? ShoppingList { get; set; }
}