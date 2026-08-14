using System.Collections.Generic;

public static class MainViewPresentationBuilder
{
	public static MainViewRenderModel Build(
		GameSessionState session,
		FloorConnectionType verticalConnection,
		IReadOnlyDictionary<string, string>? targetingLabelsByHighlightKey = null)
	{
		var dungeon = session.Dungeon;
		var facing = session.Player.Facing;
		var coord = dungeon.PlayerCoord;

		var verticalNote = verticalConnection == FloorConnectionType.None
			? string.Empty
			: $" | {verticalConnection}";

		var floorPrefix = dungeon.CurrentFloor is { } floor
			? $"Floor {floor.Level} — "
			: string.Empty;
		var title = $"{floorPrefix}Room {coord} — Facing: {facing}{verticalNote}";

		RoomConnectionType left, front, right;
		if (dungeon.CurrentRoom is { } room)
		{
			left = room.Exits.Get(DirectionHelper.TurnLeft(facing));
			front = room.Exits.Get(facing);
			right = room.Exits.Get(DirectionHelper.TurnRight(facing));
		}
		else
			left = front = right = RoomConnectionType.None;

		var rawSlots = dungeon.CurrentRoom is { } r
			? EnumerateFeatureSlots(r)
			: (IReadOnlyList<MainViewFeatureSlot>)[];

		var slots = new List<MainViewFeatureSlot>(rawSlots.Count);
		foreach (var s in rawSlots)
		{
			string? label = null;
			if (targetingLabelsByHighlightKey != null &&
			    targetingLabelsByHighlightKey.TryGetValue(s.HighlightKey, out var l))
				label = l;
			slots.Add(new MainViewFeatureSlot
			{
				HighlightKey = s.HighlightKey,
				PresentationIconKey = s.PresentationIconKey,
				TargetingLabel = label
			});
		}

		return new MainViewRenderModel
		{
			Title = title,
			LeftConnection = left,
			FrontConnection = front,
			RightConnection = right,
			FeatureSlots = slots
		};
	}

	public static IReadOnlyList<MainViewFeatureSlot> EnumerateFeatureSlots(DungeonRoom room)
	{
		var list = new List<MainViewFeatureSlot>();
		var livingMonsterOrdinal = 0;
		var treasureFeatureOrdinal = 0;
		var trapFeatureOrdinal = 0;
		var npcFeatureOrdinal = 0;
		var loreFeatureOrdinal = 0;
		var exitOrdinal = 0;
		var containerOrdinal = 0;

		foreach (var feature in room.Features)
		{
			switch (feature)
			{
				case MonsterFeature mf:
					foreach (var m in mf.Monsters)
					{
						if (m.CurrentHp <= 0)
							continue;
						list.Add(new MainViewFeatureSlot
						{
							HighlightKey = $"{MainViewRoomSlots.MonsterKeyPrefix}{livingMonsterOrdinal}",
							PresentationIconKey = PresentationIconKeys.MainView.Monster(m.Definition.Id)
						});
						livingMonsterOrdinal++;
					}

					break;
				case TrapFeature tf:
				{
					var tfOrd = trapFeatureOrdinal++;
					for (var ti = 0; ti < tf.Traps.Count; ti++)
					{
						if (!tf.Traps[ti].IsRevealed)
							continue;
						list.Add(new MainViewFeatureSlot
						{
							HighlightKey = $"{MainViewRoomSlots.TrapKeyPrefix}{tfOrd}:{ti}",
							PresentationIconKey = PresentationIconKeys.MainView.Trap(tf.Traps[ti].Definition.Id)
						});
					}

					break;
				}
				case TreasureFeature treasure:
				{
					var tfOrd = treasureFeatureOrdinal++;
					for (var ti = 0; ti < treasure.TreasureItems.Count; ti++)
					{
						var item = treasure.TreasureItems[ti];
						if (!item.IsRevealed)
							continue;
						list.Add(new MainViewFeatureSlot
						{
							HighlightKey = $"{MainViewRoomSlots.TreasureKeyPrefix}{tfOrd}:{ti}",
							PresentationIconKey = PresentationIconKeys.MainView.ForTreasureInstance(item.Definition)
						});
					}

					break;
				}
				case NpcFeature nf:
				{
					var ord = npcFeatureOrdinal++;
					for (var ni = 0; ni < nf.NPCs.Count; ni++)
					{
						if (!nf.NPCs[ni].IsRevealed)
							continue;
						list.Add(new MainViewFeatureSlot
						{
							HighlightKey = $"{MainViewRoomSlots.NpcKeyPrefix}{ord}:{ni}",
							PresentationIconKey = PresentationIconKeys.MainView.Npc(nf.NPCs[ni].Definition.Id)
						});
					}

					break;
				}
				case LoreFeature lf:
				{
					var ord = loreFeatureOrdinal++;
					for (var li = 0; li < lf.Lore.Count; li++)
					{
						if (!lf.Lore[li].IsRevealed)
							continue;
						list.Add(new MainViewFeatureSlot
						{
							HighlightKey = $"{MainViewRoomSlots.LoreKeyPrefix}{ord}:{li}",
							PresentationIconKey = PresentationIconKeys.MainView.Lore(lf.Lore[li].Definition.Id)
						});
					}

					break;
				}
				case ContainerFeature cf:
				{
					list.Add(new MainViewFeatureSlot
					{
						HighlightKey = $"{MainViewRoomSlots.ContainerKeyPrefix}{containerOrdinal}",
						PresentationIconKey = PresentationIconKeys.MainView.ForContainer(cf)
					});
					containerOrdinal++;
					break;
				}
				case FloorExitFeature exit:
				{
					if (PresentationIconKeys.MainView.IsVerticalExitIcon(exit.ExitType))
					{
						list.Add(new MainViewFeatureSlot
						{
							HighlightKey = $"{MainViewRoomSlots.ExitKeyPrefix}{exitOrdinal}",
							PresentationIconKey = PresentationIconKeys.MainView.Vertical(exit.ExitType)
						});
						exitOrdinal++;
					}

					break;
				}
			}
		}

		return list;
	}
}
