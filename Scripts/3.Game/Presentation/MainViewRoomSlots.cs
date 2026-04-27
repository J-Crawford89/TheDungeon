#nullable enable
using System.Collections.Generic;

/// <summary>Single source for main-view feature row order and <see cref="MainViewFeatureSlot.HighlightKey"/> values used by targeting.</summary>
public static class MainViewRoomSlots
{
	public const string MonsterKeyPrefix = "monster:";
	public const string TreasureKeyPrefix = "treasure:";
	public const string TrapKeyPrefix = "trap:";
	public const string NpcKeyPrefix = "npc:";
	public const string LoreKeyPrefix = "lore:";
	public const string ExitKeyPrefix = "exit:";
	public const string ContainerKeyPrefix = "container:";

	public static IReadOnlyList<MainViewFeatureSlot> Enumerate(DungeonRoom room)
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
							HighlightKey = $"{MonsterKeyPrefix}{livingMonsterOrdinal}",
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
							HighlightKey = $"{TrapKeyPrefix}{tfOrd}:{ti}",
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
							HighlightKey = $"{TreasureKeyPrefix}{tfOrd}:{ti}",
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
							HighlightKey = $"{NpcKeyPrefix}{ord}:{ni}",
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
							HighlightKey = $"{LoreKeyPrefix}{ord}:{li}",
							PresentationIconKey = PresentationIconKeys.MainView.Lore(lf.Lore[li].Definition.Id)
						});
					}

					break;
				}
				case ContainerFeature cf:
				{
					list.Add(new MainViewFeatureSlot
					{
						HighlightKey = $"{ContainerKeyPrefix}{containerOrdinal}",
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
							HighlightKey = $"{ExitKeyPrefix}{exitOrdinal}",
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

	public static bool TryGetLivingMonsterByOrdinal(DungeonRoom room, int ordinal, out MonsterFeature? feature, out int indexInFeature)
	{
		feature = null;
		indexInFeature = -1;
		var n = 0;
		foreach (var f in room.Features)
		{
			if (f is not MonsterFeature mf)
				continue;
			for (var i = 0; i < mf.Monsters.Count; i++)
			{
				if (mf.Monsters[i].CurrentHp <= 0)
					continue;
				if (n == ordinal)
				{
					feature = mf;
					indexInFeature = i;
					return true;
				}

				n++;
			}
		}

		return false;
	}

	public static bool TryGetTreasureSlot(DungeonRoom room, int treasureFeatureOrdinal, int itemIndexInFeature, out TreasureFeature? treasureFeature, out TreasureInstance? instance)
	{
		treasureFeature = null;
		instance = null;
		var ord = 0;
		foreach (var f in room.Features)
		{
			if (f is not TreasureFeature tf)
				continue;
			if (ord == treasureFeatureOrdinal)
			{
				if (itemIndexInFeature < 0 || itemIndexInFeature >= tf.TreasureItems.Count)
					return false;
				var inst = tf.TreasureItems[itemIndexInFeature];
				if (!inst.IsRevealed)
					return false;
				treasureFeature = tf;
				instance = inst;
				return true;
			}

			ord++;
		}

		return false;
	}

	public static bool TryGetTrapSlot(DungeonRoom room, int trapFeatureOrdinal, int trapIndexInFeature, out TrapFeature? trapFeature, out TrapInstance? trapInstance)
	{
		trapFeature = null;
		trapInstance = null;
		var ord = 0;
		foreach (var f in room.Features)
		{
			if (f is not TrapFeature tf)
				continue;
			if (ord == trapFeatureOrdinal)
			{
				if (trapIndexInFeature < 0 || trapIndexInFeature >= tf.Traps.Count)
					return false;
				var t = tf.Traps[trapIndexInFeature];
				if (!t.IsRevealed)
					return false;
				trapFeature = tf;
				trapInstance = t;
				return true;
			}

			ord++;
		}

		return false;
	}

	/// <summary>Highlight keys for every revealed treasure instance that <see cref="TreasurePickupService.TakeAllEligibleFromCurrentRoom"/> would remove.</summary>
	public static IReadOnlyList<string> CollectTakeAllTreasureHighlightKeys(DungeonRoom room)
	{
		var keys = new List<string>();
		var treasureFeatureOrdinal = 0;
		foreach (var f in room.Features)
		{
			if (f is not TreasureFeature tf)
				continue;
			for (var ti = 0; ti < tf.TreasureItems.Count; ti++)
			{
				if (!TreasurePickupService.IsInstanceEligibleForRoomTake(tf.TreasureItems[ti]))
					continue;
				keys.Add($"{TreasureKeyPrefix}{treasureFeatureOrdinal}:{ti}");
			}

			treasureFeatureOrdinal++;
		}

		return keys;
	}
}
