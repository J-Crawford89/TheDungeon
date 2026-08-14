#nullable enable
using Godot;
using System;
using System.Collections.Generic;

public sealed class IconResolver
{
	private readonly MonsterResourceDatabase? _monsters;
	private readonly ItemResourceDatabase? _items;
	private readonly TreasureResourceDatabase? _treasures;
	private readonly TrapResourceDatabase? _traps;
	private readonly NpcResourceDatabase? _npcs;
	private readonly LoreResourceDatabase? _lore;
	private readonly MainViewTraversalIcons? _traversal;
	private readonly ChestIconsResource? _chestIcons;
	private readonly HashSet<string> _warned = new(StringComparer.Ordinal);

	public IconResolver(
		MonsterResourceDatabase? monsters,
		ItemResourceDatabase? items,
		TreasureResourceDatabase? treasures,
		TrapResourceDatabase? traps,
		NpcResourceDatabase? npcs,
		LoreResourceDatabase? lore,
		MainViewTraversalIcons? traversal,
		ChestIconsResource? chestIcons)
	{
		_monsters = monsters;
		_items = items;
		_treasures = treasures;
		_traps = traps;
		_npcs = npcs;
		_lore = lore;
		_traversal = traversal;
		_chestIcons = chestIcons;
	}

	public Texture2D? Resolve(string key)
	{
		if (string.IsNullOrEmpty(key))
			return null;

		var invPrefix = $"{PresentationIconKeys.Inventory.Root}/";
		if (key.StartsWith(invPrefix, StringComparison.Ordinal))
			return ResolveInventoryKey(key, invPrefix);

		var mvPrefix = $"{PresentationIconKeys.MainView.Root}/";
		if (!key.StartsWith(mvPrefix, StringComparison.Ordinal))
		{
			WarnOnce(key,
				$"Icon key must start with '{mvPrefix}' or '{invPrefix}'.");
			return null;
		}

		var tail = key[mvPrefix.Length..];
		var slash = tail.IndexOf('/');
		if (slash < 0)
		{
			WarnOnce(key, "Invalid mainview icon key (missing category).");
			return null;
		}

		var category = tail[..slash];
		var id = tail[(slash + 1)..];
		if (id.Length == 0)
		{
			WarnOnce(key, "Invalid mainview icon key (empty id segment).");
			return null;
		}

		return category switch
		{
			PresentationIconKeys.Categories.Monster => ResolveMonster(key, id),
			PresentationIconKeys.Categories.Trap => ResolveTrap(key, id),
			PresentationIconKeys.Categories.Item => ResolveItem(key, id),
			PresentationIconKeys.Categories.Treasure => ResolveTreasure(key, id),
			PresentationIconKeys.Categories.Container => ResolveContainer(key, id),
			PresentationIconKeys.Categories.Npc => ResolveNpc(key, id),
			PresentationIconKeys.Categories.Lore => ResolveLore(key, id),
			PresentationIconKeys.Categories.Vertical => ResolveVertical(key, id),
			_ => UnknownCategory(key, category)
		};
	}

	private Texture2D? UnknownCategory(string key, string category)
	{
		WarnOnce(key, $"Unknown mainview icon category '{category}'.");
		return null;
	}

	private Texture2D? ResolveInventoryKey(string key, string invPrefix)
	{
		var tail = key[invPrefix.Length..];
		var slash = tail.IndexOf('/');
		if (slash < 0)
		{
			WarnOnce(key, "Invalid inventory icon key (missing category).");
			return null;
		}

		var category = tail[..slash];
		var id = tail[(slash + 1)..];
		if (id.Length == 0)
		{
			WarnOnce(key, "Invalid inventory icon key (empty id segment).");
			return null;
		}

		return category switch
		{
			PresentationIconKeys.Categories.Item => ResolveInventoryItem(key, id),
			_ => UnknownInventoryCategory(key, category)
		};
	}

	private Texture2D? UnknownInventoryCategory(string key, string category)
	{
		WarnOnce(key, $"Unknown inventory icon category '{category}'.");
		return null;
	}

	private Texture2D? ResolveInventoryItem(string fullKey, string id)
	{
		var row = FindItemById(id);
		if (row == null)
		{
			WarnOnce(fullKey, $"No ItemResource with Id '{id}'.");
			return null;
		}

		var tex = row.InventoryIcon ?? row.Icon;
		if (tex == null)
			WarnOnce($"{fullKey}|nullicon", $"ItemResource '{id}' has no InventoryIcon or Icon assigned.");
		return tex;
	}

	private Texture2D? ResolveMonster(string fullKey, string id)
	{
		var row = FindMonsterById(id);
		if (row == null)
		{
			WarnOnce(fullKey, $"No MonsterResource with Id '{id}'.");
			return null;
		}

		if (row.Icon == null)
			WarnOnce($"{fullKey}|nullicon", $"MonsterResource '{id}' has no Icon assigned.");
		return row.Icon;
	}

	private Texture2D? ResolveTrap(string fullKey, string id)
	{
		var row = FindTrapById(id);
		if (row == null)
		{
			WarnOnce(fullKey, $"No TrapResource with Id '{id}'.");
			return null;
		}

		if (row.Icon == null)
			WarnOnce($"{fullKey}|nullicon", $"TrapResource '{id}' has no Icon assigned.");
		return row.Icon;
	}

	private Texture2D? ResolveItem(string fullKey, string id)
	{
		var row = FindItemById(id);
		if (row == null)
		{
			WarnOnce(fullKey, $"No ItemResource with Id '{id}'.");
			return null;
		}

		if (row.Icon == null)
			WarnOnce($"{fullKey}|nullicon", $"ItemResource '{id}' has no Icon assigned.");
		return row.Icon;
	}

	private Texture2D? ResolveCorpseTexture(string fullKey, string monsterIdSegment, bool opened)
	{
		var monsterId = monsterIdSegment.Trim();
		if (string.IsNullOrWhiteSpace(monsterId) || monsterId.Equals("unknown", StringComparison.OrdinalIgnoreCase))
		{
			WarnOnce($"{fullKey}|corpseunknown", "Corpse has no monster id for icon.");
			return null;
		}

		var m = FindMonsterById(monsterId);
		if (m == null)
		{
			WarnOnce(fullKey, $"No MonsterResource with Id '{monsterId}' for corpse icon.");
			return null;
		}

		Texture2D? tex = opened
			? m.CorpseOpenedIcon ?? m.CorpseIcon ?? m.Icon
			: m.CorpseIcon ?? m.Icon;
		if (tex == null)
			WarnOnce($"{fullKey}|nullicon",
				$"MonsterResource '{monsterId}' has no {(opened ? "CorpseOpenedIcon or CorpseIcon or Icon" : "CorpseIcon or Icon")} for corpse.");
		return tex;
	}

	private Texture2D? ResolveContainer(string fullKey, string id)
	{
		if (id.StartsWith("corpse/", StringComparison.Ordinal))
			return ResolveCorpseTexture(fullKey, id["corpse/".Length..], opened: false);

		if (id.StartsWith("corpse_opened/", StringComparison.Ordinal))
			return ResolveCorpseTexture(fullKey, id["corpse_opened/".Length..], opened: true);

		if (_chestIcons == null)
		{
			WarnOnce($"{fullKey}|nochesticons", "Assign ChestIconsResource on GameRoot for container icons.");
			return null;
		}

		switch (id)
		{
			case "chest":
				return _chestIcons.ChestIcon;
			case "chest_opened":
				return _chestIcons.ChestOpenedIcon ?? _chestIcons.ChestIcon;
			case "chest_locked":
				return _chestIcons.ChestLockedIcon ?? _chestIcons.ChestIcon;
			case "salvage":
				return _chestIcons.SalvageIcon;
			case "salvage_opened":
				return _chestIcons.SalvageOpenedIcon ?? _chestIcons.SalvageIcon;
			case "other":
				return _chestIcons.OtherContainerIcon;
			default:
				WarnOnce(fullKey, $"Unknown container icon id '{id}'.");
				return null;
		}
	}

	private Texture2D? ResolveTreasure(string fullKey, string id)
	{
		var row = FindTreasureById(id);
		if (row == null)
		{
			WarnOnce(fullKey, $"No TreasureResource with Id '{id}'.");
			return null;
		}

		if (row.Icon == null)
			WarnOnce($"{fullKey}|nullicon", $"TreasureResource '{id}' has no Icon assigned.");
		return row.Icon;
	}

	private Texture2D? ResolveNpc(string fullKey, string id)
	{
		var row = FindNpcById(id);
		if (row == null)
		{
			WarnOnce(fullKey, $"No NpcResource with Id '{id}'.");
			return null;
		}

		if (row.Icon == null)
			WarnOnce($"{fullKey}|nullicon", $"NpcResource '{id}' has no Icon assigned.");
		return row.Icon;
	}

	private Texture2D? ResolveLore(string fullKey, string id)
	{
		var row = FindLoreById(id);
		if (row == null)
		{
			WarnOnce(fullKey, $"No LoreResource with Id '{id}'.");
			return null;
		}

		if (row.Icon == null)
			WarnOnce($"{fullKey}|nullicon", $"LoreResource '{id}' has no Icon assigned.");
		return row.Icon;
	}

	private Texture2D? ResolveVertical(string fullKey, string id)
	{
		if (!Enum.TryParse<FloorConnectionType>(id, out var t) || !PresentationIconKeys.MainView.IsVerticalExitIcon(t))
		{
			WarnOnce(fullKey, $"Unrecognized vertical exit type '{id}'.");
			return null;
		}

		if (_traversal == null)
		{
			WarnOnce($"{fullKey}|notraversal", "Assign MainViewTraversalIcons on GameRoot for vertical exit icons.");
			return null;
		}

		var tex = t switch
		{
			FloorConnectionType.Stairs => _traversal.StairsIcon,
			FloorConnectionType.Hole => _traversal.HoleIcon,
			FloorConnectionType.Ladder => _traversal.LadderIcon,
			_ => null
		};

		if (tex == null)
			WarnOnce($"{fullKey}|nullicon", $"MainViewTraversalIcons has no icon for {t}.");
		return tex;
	}

	private MonsterResource? FindMonsterById(string id)
	{
		var items = _monsters?.Monsters;
		if (items == null)
			return null;
		for (var i = 0; i < items.Count; i++)
		{
			if (items[i] is { } m && m.Id == id)
				return m;
		}

		return null;
	}

	private TrapResource? FindTrapById(string id)
	{
		var items = _traps?.Traps;
		if (items == null)
			return null;
		for (var i = 0; i < items.Count; i++)
		{
			if (items[i] is { } t && t.Id == id)
				return t;
		}

		return null;
	}

	private ItemResource? FindItemById(string id)
	{
		var items = _items?.Items;
		if (items != null)
		{
			for (var i = 0; i < items.Count; i++)
			{
				if (items[i] is { } it && it.Id == id)
					return it;
			}
		}

		// Items authored only as embedded ItemResources on MonsterResource.DeathLoot are omitted from
		// ItemDatabase on purpose; align lookup with GodotItemDefinitionRepository merge behavior.
		if (_monsters?.Monsters != null)
		{
			foreach (var monster in _monsters.Monsters)
			{
				if (monster?.DeathLoot == null)
					continue;
				foreach (var row in monster.DeathLoot)
				{
					if (row?.ItemResource == null)
						continue;
					if (row.ItemResource.Id == id)
						return row.ItemResource;
				}
			}
		}

		return null;
	}

	private TreasureResource? FindTreasureById(string id)
	{
		var items = _treasures?.Treasures;
		if (items == null)
			return null;
		for (var i = 0; i < items.Count; i++)
		{
			if (items[i] is { } t && t.Id == id)
				return t;
		}

		return null;
	}

	private NpcResource? FindNpcById(string id)
	{
		var items = _npcs?.Npcs;
		if (items == null)
			return null;
		for (var i = 0; i < items.Count; i++)
		{
			if (items[i] is { } n && n.Id == id)
				return n;
		}

		return null;
	}

	private LoreResource? FindLoreById(string id)
	{
		var items = _lore?.LoreEntries;
		if (items == null)
			return null;
		for (var i = 0; i < items.Count; i++)
		{
			if (items[i] is { } l && l.Id == id)
				return l;
		}

		return null;
	}

	private void WarnOnce(string dedupeKey, string message)
	{
		if (!_warned.Add(dedupeKey))
			return;
		GD.PushWarning($"IconResolver: {message} (key: {dedupeKey.Split('|')[0]})");
	}
}
