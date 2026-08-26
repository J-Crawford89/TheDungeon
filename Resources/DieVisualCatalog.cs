#nullable enable
using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class DieVisualCatalog : Resource
{
	[Export] public DieRollVisualKind Situation { get; set; } = DieRollVisualKind.Player;
	[Export] public Material? BodyMaterial { get; set; }
	[Export] public Godot.Collections.Array<DieVisualCatalogEntry> Entries { get; set; } = [];

	private Dictionary<(DieType DieType, DieVisualRole Role), PackedScene>? _lookup;

	public DieVisualSelection? Resolve(DieType dieType, DieVisualRole role)
	{
		_lookup ??= BuildLookup();
		foreach (var key in DieVisualCatalogKeys.GetResolveKeys(dieType, role))
		{
			if (_lookup.TryGetValue((key.DieType, key.Role), out var scene))
			{
				return new DieVisualSelection
				{
					VisualScene = scene,
					BodyMaterial = BodyMaterial,
				};
			}
		}

		return null;
	}

	private Dictionary<(DieType, DieVisualRole), PackedScene> BuildLookup()
	{
		var map = new Dictionary<(DieType, DieVisualRole), PackedScene>();
		if (Entries == null)
			return map;
		foreach (var entry in Entries)
		{
			if (entry?.VisualScene == null)
				continue;
			map[(entry.DieType, entry.Role)] = entry.VisualScene;
		}

		return map;
	}
}
