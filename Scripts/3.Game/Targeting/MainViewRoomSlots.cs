#nullable enable
using System.Collections.Generic;

/// <summary>Main-view feature row order and highlight-key prefixes used by targeting locators.</summary>
public static class MainViewRoomSlots
{
	public const string MonsterKeyPrefix = "monster:";
	public const string TreasureKeyPrefix = "treasure:";
	public const string TrapKeyPrefix = "trap:";
	public const string NpcKeyPrefix = "npc:";
	public const string LoreKeyPrefix = "lore:";
	public const string ExitKeyPrefix = "exit:";
	public const string ContainerKeyPrefix = "container:";

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
