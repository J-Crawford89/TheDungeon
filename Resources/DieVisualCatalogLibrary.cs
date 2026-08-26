#nullable enable
using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class DieVisualCatalogLibrary : Resource
{
	[Export] public Godot.Collections.Array<DieVisualCatalog> Catalogs { get; set; } = [];

	public DieVisualSelection? Resolve(DieRollVisualKind situation, DieType dieType, DieVisualRole role)
	{
		if (Catalogs == null)
			return null;
		foreach (var catalog in Catalogs)
		{
			if (catalog == null || catalog.Situation != situation)
				continue;
			var selection = catalog.Resolve(dieType, role);
			if (selection != null)
				return selection;
		}

		return null;
	}
}
