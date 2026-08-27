namespace Boodschap.Features.ShoppingLists.Application;

public readonly record struct MutationResult<T>(T? Value, bool Changed) where T : class;