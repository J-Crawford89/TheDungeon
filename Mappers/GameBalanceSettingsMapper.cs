#nullable enable
using System;
using System.Collections.Generic;
using Godot.Collections;

/// <summary>Maps <see cref="GameBalanceSettingsResource"/> exports to runtime DTOs used by floor generation and chest loot.</summary>
public static class GameBalanceSettingsMapper
{
	public static RoomFeaturePopulationParameters ToRoomFeaturePopulationParameters(GameBalanceSettingsResource? resource)
	{
		if (resource?.FeatureRules == null || resource.FeatureRules.Count == 0)
			return RoomFeaturePopulationParameters.CreateDefault();

		var rules = new List<FeatureTypeRule>();
		foreach (var row in resource.FeatureRules)
		{
			if (row == null)
				continue;
			rules.Add(new FeatureTypeRule
			{
				Kind = row.Kind,
				Weight = row.Weight,
				MinPerRoom = row.MinPerRoom,
				MaxPerRoom = row.MaxPerRoom,
				AllowedInEntrance = row.AllowedInEntrance,
				AllowedInExit = row.AllowedInExit,
				CannotCoexistWith = MapCoexist(row.CannotCoexistWith),
			});
		}

		if (rules.Count == 0)
			return RoomFeaturePopulationParameters.CreateDefault();

		return new RoomFeaturePopulationParameters
		{
			MinFeaturesPerRoom = resource.MinFeaturesPerRoom,
			MaxFeaturesPerRoom = resource.MaxFeaturesPerRoom,
			ContinueAfterFirstOptionalFeatureProbability = resource.ContinueAfterFirstOptionalFeatureProbability,
			DiminishMultiplierPerOptionalFeature = resource.DiminishMultiplierPerOptionalFeature,
			FeatureTypeRules = rules,
		};
	}

	public static ChestLootGenerationParameters ToChestLootGenerationParameters(GameBalanceSettingsResource? resource)
	{
		if (resource == null)
			return ChestLootGenerationParameters.Default;

		var weights = new List<ChestLootCategoryWeight>();
		if (resource.ChestLootCategoryWeights != null && resource.ChestLootCategoryWeights.Count > 0)
		{
			foreach (var w in resource.ChestLootCategoryWeights)
			{
				if (w == null)
					continue;
				weights.Add(new ChestLootCategoryWeight { Category = w.Category, Weight = w.Weight });
			}
		}

		if (weights.Count == 0)
		{
			foreach (var x in ChestLootGenerationParameters.Default.CategoryWeights)
				weights.Add(new ChestLootCategoryWeight { Category = x.Category, Weight = x.Weight });
		}

		var minS = Math.Max(0, resource.ChestMinStacks);
		var maxS = Math.Max(minS, resource.ChestMaxStacks);
		var qMin = Math.Max(1, resource.ChestQuantityMin);
		var qMax = Math.Max(qMin, resource.ChestQuantityMax);

		return new ChestLootGenerationParameters
		{
			MinStacks = minS,
			MaxStacks = maxS,
			QuantityMin = qMin,
			QuantityMax = qMax,
			CategoryWeights = weights,
		};
	}

	public static FloorGenerationParameters BuildFloorGenerationParameters(
		GameBalanceSettingsResource? balance,
		int seed,
		int currentFloorCount,
		FloorConnectionType previousExitType)
	{
		var roomFeatures = ToRoomFeaturePopulationParameters(balance);

		IReadOnlyDictionary<FloorConnectionType, double>? verticalWeights = null;
		if (balance != null &&
		    (balance.VerticalWeightStairs > 0 || balance.VerticalWeightHole > 0 || balance.VerticalWeightLadder > 0))
		{
			verticalWeights = new System.Collections.Generic.Dictionary<FloorConnectionType, double>
			{
				[FloorConnectionType.Stairs] = balance.VerticalWeightStairs,
				[FloorConnectionType.Hole] = balance.VerticalWeightHole,
				[FloorConnectionType.Ladder] = balance.VerticalWeightLadder,
			};
		}

		return new FloorGenerationParameters
		{
			MinRooms = balance?.MinRooms ?? 6,
			MaxRooms = balance?.MaxRooms ?? 12,
			Seed = seed,
			DoorChance = balance?.DoorChance ?? 0.15,
			CurrentFloorCount = currentFloorCount,
			PreviousFloorConnectionType = previousExitType,
			VerticalConnectionWeights = verticalWeights,
			RoomFeatures = roomFeatures,
		};
	}

	private static IReadOnlyList<PopulateableFeatureKind> MapCoexist(Godot.Collections.Array<PopulateableFeatureKind>? raw)
	{
		if (raw == null || raw.Count == 0)
			return System.Array.Empty<PopulateableFeatureKind>();

		var list = new List<PopulateableFeatureKind>(raw.Count);
		foreach (var v in raw)
			list.Add(v);

		return list;
	}
}
