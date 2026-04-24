#nullable enable
using System.Collections.Generic;

public static class PlayerActionTargetResolvers
{
	public static IReadOnlyList<TargetDescriptor> ResolveAttackTargets(GameSessionState session)
	{
		if (session.Dungeon.CurrentRoom is not { } room)
			return [];
		var list = new List<TargetDescriptor>();
		var ordinal = 0;
		foreach (var f in room.Features)
		{
			if (f is not MonsterFeature mf)
				continue;
			foreach (var m in mf.Monsters)
			{
				if (m.CurrentHp <= 0)
					continue;
				var label = m.Definition.Name;
				list.Add(new TargetDescriptor
				{
					Label = label,
					HighlightKey = $"{MainViewRoomSlots.MonsterKeyPrefix}{ordinal}",
					Payload = new TargetPayload
					{
						Kind = TargetPayloadKind.AttackLivingMonsterOrdinal,
						LivingMonsterOrdinal = ordinal
					}
				});
				ordinal++;
			}
		}

		return list;
	}

	public static IReadOnlyList<TargetDescriptor> ResolveTakeTargets(GameSessionState session, DungeonMode mode)
	{
		if (session.Dungeon.CurrentRoom is not { } room)
			return [];
		var perItem = new List<TargetDescriptor>();
		var treasureFeatureOrdinal = 0;
		foreach (var f in room.Features)
		{
			if (f is not TreasureFeature tf)
				continue;
			for (var ti = 0; ti < tf.TreasureItems.Count; ti++)
			{
				var inst = tf.TreasureItems[ti];
				if (!TreasurePickupService.IsInstanceEligibleForRoomTake(inst))
					continue;
				var key = $"{MainViewRoomSlots.TreasureKeyPrefix}{treasureFeatureOrdinal}:{ti}";
				perItem.Add(new TargetDescriptor
				{
					Label = inst.Definition.Name,
					HighlightKey = key,
					Payload = new TargetPayload
					{
						Kind = TargetPayloadKind.TakeTreasureItem,
						TreasureFeatureOrdinal = treasureFeatureOrdinal,
						TreasureItemIndexInFeature = ti
					}
				});
			}

			treasureFeatureOrdinal++;
		}

		if (perItem.Count <= 1 || mode != DungeonMode.Exploration)
			return perItem;

		var takeAllKeys = MainViewRoomSlots.CollectTakeAllTreasureHighlightKeys(room);
		var aggregate = new TargetDescriptor
		{
			Label = "Take all",
			HighlightKey = null,
			TakeAllHighlightKeys = takeAllKeys,
			Payload = new TargetPayload { Kind = TargetPayloadKind.TakeAllEligibleTreasure }
		};

		var combined = new List<TargetDescriptor> { aggregate };
		combined.AddRange(perItem);
		return combined;
	}

	public static IReadOnlyList<TargetDescriptor> ResolveDisarmTargets(GameSessionState session)
	{
		if (session.Dungeon.CurrentRoom is not { } room)
			return [];
		var list = new List<TargetDescriptor>();
		var trapFeatureOrdinal = 0;
		foreach (var f in room.Features)
		{
			if (f is not TrapFeature tf)
				continue;
			for (var ti = 0; ti < tf.Traps.Count; ti++)
			{
				var t = tf.Traps[ti];
				if (!t.IsRevealed)
					continue;
				var label = t.Definition.Name;
				list.Add(new TargetDescriptor
				{
					Label = label,
					HighlightKey = $"{MainViewRoomSlots.TrapKeyPrefix}{trapFeatureOrdinal}:{ti}",
					Payload = new TargetPayload
					{
						Kind = TargetPayloadKind.DisarmTrapInstance,
						TrapFeatureOrdinal = trapFeatureOrdinal,
						TrapIndexInFeature = ti
					}
				});
			}

			trapFeatureOrdinal++;
		}

		return list;
	}
}
