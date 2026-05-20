using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class DieVisualCatalogLibrary : Resource
{
	[Export] public Godot.Collections.Array<DieVisualCatalog> Catalogs { get; set; } = [];

	public PackedScene? Resolve(DieRollVisualKind situation, DieType dieType, DieVisualRole role)
	{
		if (Catalogs == null)
			return null;
		foreach (var catalog in Catalogs)
		{
			if (catalog == null || catalog.Situation != situation)
				continue;
			var scene = catalog.Resolve(dieType, role);
			if (scene != null)
				return scene;
		}

		return null;
	}
}
