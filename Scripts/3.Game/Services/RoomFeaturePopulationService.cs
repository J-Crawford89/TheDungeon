using System;
using System.Collections.Generic;
using System.Linq;

public sealed class RoomFeaturePopulationService
{
	public void Populate(
		DungeonFloor floor,
		RoomFeaturePopulationParameters parameters,
		Random random,
		Action<string>? logDiagnostic = null)
	{
		var rulesByKind = parameters.FeatureTypeRules.ToDictionary(r => r.Kind);
		var downExitCoord = FindDownExitRoomCoord(floor);
		var effectiveMaxFeatures = Math.Max(parameters.MaxFeaturesPerRoom, parameters.MinFeaturesPerRoom);

		foreach (var (coord, room) in floor.Rooms)
		{
			if (floor.Level == 1 && coord.Equals(DirectionHelper.Origin))
				continue;

			var isEntrance = coord.Equals(floor.Entrance);
			var isExit = downExitCoord.HasValue && coord.Equals(downExitCoord.Value);

			while (GetPopulateableFeatureCount(room) < parameters.MinFeaturesPerRoom &&
			       GetPopulateableFeatureCount(room) < effectiveMaxFeatures)
			{
				if (!TryPickAndAdd(room, parameters, rulesByKind, random, isEntrance, isExit, onlyKind: null))
				{
					logDiagnostic?.Invoke(DebugMessage.Format(
						"Room feature population (global minimum)",
						"No eligible feature type could be placed to reach MinFeaturesPerRoom",
						$"floorLevel={floor.Level}, coord={coord}, currentCount={GetPopulateableFeatureCount(room)}, min={parameters.MinFeaturesPerRoom}"));
					break;
				}
			}

			foreach (var rule in parameters.FeatureTypeRules.OrderByDescending(r => r.MinPerRoom))
			{
				while (rule.MinPerRoom > 0 &&
				       CountKind(room, rule.Kind) < rule.MinPerRoom &&
				       GetPopulateableFeatureCount(room) < effectiveMaxFeatures)
				{
					if (!TryPickAndAdd(room, parameters, rulesByKind, random, isEntrance, isExit, rule.Kind))
					{
						logDiagnostic?.Invoke(DebugMessage.Format(
							"Room feature population (per-type minimum)",
							"Could not place required feature kind",
							$"floorLevel={floor.Level}, coord={coord}, kind={rule.Kind}, need={rule.MinPerRoom}, have={CountKind(room, rule.Kind)}"));
						break;
					}
				}
			}

			var optionalSlot = 0;
			while (GetPopulateableFeatureCount(room) < effectiveMaxFeatures)
			{
				var probability = parameters.ContinueAfterFirstOptionalFeatureProbability *
				                  Math.Pow(parameters.DiminishMultiplierPerOptionalFeature, optionalSlot);
				if (random.NextDouble() >= probability)
					break;

				if (!TryPickAndAdd(room, parameters, rulesByKind, random, isEntrance, isExit, onlyKind: null))
					break;

				optionalSlot++;
			}
		}
	}

	private static RoomCoord? FindDownExitRoomCoord(DungeonFloor floor)
	{
		var candidates = new List<RoomCoord>();
		foreach (var (coord, room) in floor.Rooms)
		{
			if (!RoomFeatureHelper.HasFeature<FloorExitFeature>(room))
				continue;
			if (DirectionHelper.GetVerticalExitDirection(coord) != VerticalDirection.Down)
				continue;
			candidates.Add(coord);
		}

		if (candidates.Count == 0)
			return null;
		candidates.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
		return candidates[0];
	}

	private static bool TryPickAndAdd(
		DungeonRoom room,
		RoomFeaturePopulationParameters parameters,
		Dictionary<PopulateableFeatureKind, FeatureTypeRule> rulesByKind,
		Random random,
		bool isEntrance,
		bool isExit,
		PopulateableFeatureKind? onlyKind)
	{
		var weighted = new List<(PopulateableFeatureKind kind, double weight)>();
		foreach (var rule in parameters.FeatureTypeRules)
		{
			if (onlyKind.HasValue && rule.Kind != onlyKind.Value)
				continue;
			if (rule.Weight <= 0)
				continue;
			if (CountKind(room, rule.Kind) >= rule.MaxPerRoom)
				continue;
			if (isEntrance && !rule.AllowedInEntrance)
				continue;
			if (isExit && !rule.AllowedInExit)
				continue;
			if (!rulesByKind.TryGetValue(rule.Kind, out _))
				continue;
			if (!CanCoexist(room, rule.Kind, rulesByKind))
				continue;
			weighted.Add((rule.Kind, rule.Weight));
		}

		if (weighted.Count == 0)
			return false;

		var picked = WeightedRandomSelection.Pick(random, weighted);
		room.Features.Add(CreateFeature(picked, random));
		return true;
	}

	private static bool CanCoexist(
		DungeonRoom room,
		PopulateableFeatureKind candidate,
		Dictionary<PopulateableFeatureKind, FeatureTypeRule> rulesByKind)
	{
		if (!rulesByKind.TryGetValue(candidate, out var candidateRule))
			return false;

		foreach (var feature in room.Features)
		{
			var existing = TryGetKind(feature);
			if (existing == null)
				continue;

			foreach (var banned in candidateRule.CannotCoexistWith)
			{
				if (banned == existing)
					return false;
			}

			if (rulesByKind.TryGetValue(existing.Value, out var existingRule))
			{
				foreach (var banned in existingRule.CannotCoexistWith)
				{
					if (banned == candidate)
						return false;
				}
			}
		}

		return true;
	}

	private static int GetPopulateableFeatureCount(DungeonRoom room) =>
		room.Features.Count(f => TryGetKind(f) != null);

	private static int CountKind(DungeonRoom room, PopulateableFeatureKind kind) =>
		room.Features.Count(f => TryGetKind(f) == kind);

	private static PopulateableFeatureKind? TryGetKind(RoomFeature feature) =>
		feature switch
		{
			MonsterFeature => PopulateableFeatureKind.Monster,
			TrapFeature => PopulateableFeatureKind.Trap,
			TreasureFeature => PopulateableFeatureKind.Treasure,
			NpcFeature => PopulateableFeatureKind.Npc,
			LoreFeature => PopulateableFeatureKind.Lore,
			_ => null
		};

	private static RoomFeature CreateFeature(PopulateableFeatureKind kind, Random random) =>
		kind switch
		{
			PopulateableFeatureKind.Monster => CreateMonsterFeature(random),
			PopulateableFeatureKind.Trap => CreateTrapFeature(random),
			PopulateableFeatureKind.Treasure => CreateTreasureFeature(random),
			PopulateableFeatureKind.Npc => CreateNpcFeature(random),
			PopulateableFeatureKind.Lore => CreateLoreFeature(random),
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
		};

	private static MonsterFeature CreateMonsterFeature(Random random)
	{
		var weighted = MonsterLibrary.All.Select(d => (d, (double)d.RandomizerWeight)).ToList();
		var def = WeightedRandomSelection.Pick(random, weighted);
		return new MonsterFeature
		{
			Monsters = new List<MonsterInstance>
			{
				new() { Definition = def, CurrentHp = def.MaxHp }
			}
		};
	}

	private static TrapFeature CreateTrapFeature(Random random)
	{
		var weighted = TrapLibrary.All.Select(t => (t, 1.0)).ToList();
		var def = WeightedRandomSelection.Pick(random, weighted);
		return new TrapFeature
		{
			Traps = new List<TrapInstance>
			{
				new() { Definition = def, CurrentHp = 1 }
			}
		};
	}

	private static TreasureFeature CreateTreasureFeature(Random random)
	{
		var weighted = TreasureLibrary.All.Select(t => (t, 1.0)).ToList();
		var def = WeightedRandomSelection.Pick(random, weighted);
		return new TreasureFeature
		{
			RemoveFeatureWhenEmpty = true,
			TreasureItems = new List<TreasureInstance> { new() { Definition = def } }
		};
	}

	private static NpcFeature CreateNpcFeature(Random random)
	{
		var weighted = NpcLibrary.All.Select(n => (n, 1.0)).ToList();
		var def = WeightedRandomSelection.Pick(random, weighted);
		return new NpcFeature
		{
			NPCs = new List<NpcInstance>
			{
				new() { Definition = def, CurrentHp = 10 }
			}
		};
	}

	private static LoreFeature CreateLoreFeature(Random random)
	{
		var weighted = LoreLibrary.All.Select(l => (l, 1.0)).ToList();
		var def = WeightedRandomSelection.Pick(random, weighted);
		return new LoreFeature
		{
			Lore = new List<LoreInstance>
			{
				new() { Definition = def, CurrentHp = 0 }
			}
		};
	}
}
