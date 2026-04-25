#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

public static class ItemResourceListMapper
{
	/// <summary>Maps exported <see cref="ItemResource"/> references to item definition ids for domain models.</summary>
	public static List<string> ToDefinitionIds(Array<ItemResource>? items) =>
		items == null || items.Count == 0
			? []
			: items.OfType<ItemResource>()
				.Select(r => r.Id?.Trim() ?? "")
				.Where(id => id.Length > 0)
				.ToList();
}
