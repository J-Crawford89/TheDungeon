using System.Collections.Generic;

public static class MainViewPresentationBuilder
{
	public static MainViewRenderModel Build(GameSessionState session, FloorConnectionType verticalConnection)
	{
		var dungeon = session.Dungeon;
		var facing = session.Player.Facing;
		var coord = dungeon.PlayerCoord;

		var verticalNote = verticalConnection == FloorConnectionType.None
			? string.Empty
			: $" | {verticalConnection}";
		var title = $"Room {coord} — Facing: {facing}{verticalNote}";

		RoomConnectionType left, front, right;
		if (dungeon.CurrentRoom is { } room)
		{
			left = room.Exits.Get(DirectionHelper.TurnLeft(facing));
			front = room.Exits.Get(facing);
			right = room.Exits.Get(DirectionHelper.TurnRight(facing));
		}
		else
			left = front = right = RoomConnectionType.None;

		var icons = new List<MainViewFeatureIconKind>();
		if (dungeon.CurrentRoom is { } r)
			CollectFeatureIcons(r, icons);

		return new MainViewRenderModel
		{
			Title = title,
			LeftConnection = left,
			FrontConnection = front,
			RightConnection = right,
			FeatureIcons = icons
		};
	}

	private static void CollectFeatureIcons(DungeonRoom room, List<MainViewFeatureIconKind> icons)
	{
		foreach (var feature in room.Features)
		{
			switch (feature)
			{
				case MonsterFeature mf:
					foreach (var m in mf.Monsters)
					{
						if (m.CurrentHp <= 0)
							continue;
						var k = MapMonsterId(m.Definition.Id);
						if (k.HasValue)
							icons.Add(k.Value);
					}

					break;
				case TrapFeature tf:
					foreach (var _ in tf.Traps)
						icons.Add(MainViewFeatureIconKind.Trap);
					break;
				case TreasureFeature treasure:
					foreach (var t in treasure.TreasureItems)
					{
						var k = MapTreasure(t.Definition);
						if (k.HasValue)
							icons.Add(k.Value);
					}

					break;
				case NpcFeature nf:
					foreach (var _ in nf.NPCs)
						icons.Add(MainViewFeatureIconKind.Npc);
					break;
				case LoreFeature lf:
					foreach (var _ in lf.Lore)
						icons.Add(MainViewFeatureIconKind.Lore);
					break;
				case FloorExitFeature exit:
					var v = MapVerticalExit(exit.ExitType);
					if (v.HasValue)
						icons.Add(v.Value);
					break;
			}
		}
	}

	private static MainViewFeatureIconKind? MapMonsterId(string id) =>
		id switch
		{
			"rat" => MainViewFeatureIconKind.Rat,
			"giant_rat" => MainViewFeatureIconKind.GiantRat,
			"rat_king" => MainViewFeatureIconKind.RatKing,
			_ => null
		};

	private static MainViewFeatureIconKind? MapTreasure(TreasureDefinition def)
	{
		if (def.Id == TreasureIds.CopperCoins || def.GrantKind == TreasureKind.Gold)
			return MainViewFeatureIconKind.Coins;
		if (def.Id == TreasureIds.HealthPotion || def.InventoryItemId == InventoryIds.HealthPotionItemId)
			return MainViewFeatureIconKind.HealthPotion;
		return null;
	}

	private static MainViewFeatureIconKind? MapVerticalExit(FloorConnectionType t) =>
		t switch
		{
			FloorConnectionType.Stairs => MainViewFeatureIconKind.Stairs,
			FloorConnectionType.Hole => MainViewFeatureIconKind.Hole,
			FloorConnectionType.Ladder => MainViewFeatureIconKind.Ladder,
			_ => null
		};
}
