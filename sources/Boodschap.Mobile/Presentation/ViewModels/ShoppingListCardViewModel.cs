using System.Globalization;
using Boodschap.Features.ShoppingLists.Domain;

namespace Boodschap.Mobile.Presentation.ViewModels;

public sealed class ShoppingListCardViewModel(ShoppingList list, string summaryFormat)
{
	public ShoppingList List { get; } = list;

	public string Name => List.Name;

	public string Description => List.Description;

	public string Summary => string.Format(
		CultureInfo.CurrentCulture,
		summaryFormat,
		List.Items.Count(item => item.IsDone),
		List.Items.Count);

	public bool Archived => List.Archived;
}