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

		var flat = BuildFlatTakeTargets(room, mode);

		var takeAllKeys = MainViewRoomSlots.CollectTakeAllTreasureHighlightKeys(room);
		if (takeAllKeys.Count < 2 || mode != DungeonMode.Exploration)
			return flat;

		var aggregate = new TargetDescriptor
		{
			Label = "Take all",
			HighlightKey = null,
			TakeAllHighlightKeys = takeAllKeys,
			Payload = new TargetPayload { Kind = TargetPayloadKind.TakeAllEligibleTreasure }
		};

		var combined = new List<TargetDescriptor> { aggregate };
		combined.AddRange(flat);
		return combined;
	}

	private static List<TargetDescriptor> BuildFlatTakeTargets(DungeonRoom room, DungeonMode mode)
	{
		var list = new List<TargetDescriptor>();
		var treasureFeatureOrdinal = 0;
		var containerOrdinal = 0;

		foreach (var f in room.Features)
		{
			switch (f)
			{
				case TreasureFeature tf:
				{
					for (var ti = 0; ti < tf.TreasureItems.Count; ti++)
					{
						var inst = tf.TreasureItems[ti];
						if (!TreasurePickupService.IsInstanceEligibleForRoomTake(inst))
							continue;
						var key = $"{MainViewRoomSlots.TreasureKeyPrefix}{treasureFeatureOrdinal}:{ti}";
						list.Add(new TargetDescriptor
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
					break;
				}
				case ContainerFeature cf:
				{
					if (mode != DungeonMode.Combat &&
					    RoomContainerLocator.HasLootableStacks(cf))
					{
						list.Add(new TargetDescriptor
						{
							Label = ContainerTakeLabel(cf),
							HighlightKey = $"{MainViewRoomSlots.ContainerKeyPrefix}{containerOrdinal}",
							Payload = new TargetPayload
							{
								Kind = TargetPayloadKind.LootContainerAll,
								ContainerOrdinal = containerOrdinal
							}
						});
					}

					containerOrdinal++;
					break;
				}
			}
		}

		return list;
	}

	private static string ContainerTakeLabel(ContainerFeature cf) =>
		cf switch
		{
			SalvageFeature => "Salvage pile",
			CorpseFeature => "Remains",
			ChestFeature ch => ch.Locked ? "Chest (locked)" : "Chest",
			_ => "Loot"
		};

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
