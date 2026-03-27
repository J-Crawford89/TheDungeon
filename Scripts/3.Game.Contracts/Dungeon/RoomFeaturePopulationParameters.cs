using System;
using System.Collections.Generic;

public sealed class RoomFeaturePopulationParameters
{
	public int MinFeaturesPerRoom { get; init; }
	public int MaxFeaturesPerRoom { get; init; } = 4;
	public double ContinueAfterFirstOptionalFeatureProbability { get; init; } = 0.45;
	public double DiminishMultiplierPerOptionalFeature { get; init; } = 0.65;
	public IReadOnlyList<FeatureTypeRule> FeatureTypeRules { get; init; } = Array.Empty<FeatureTypeRule>();

	public static RoomFeaturePopulationParameters CreateDefault() =>
		new()
		{
			MinFeaturesPerRoom = 0,
			MaxFeaturesPerRoom = 4,
			ContinueAfterFirstOptionalFeatureProbability = 0.75,
			DiminishMultiplierPerOptionalFeature = 0.65,
			FeatureTypeRules = new[]
			{
				new FeatureTypeRule
				{
					Kind = PopulateableFeatureKind.Monster,
					Weight = 10,
					MinPerRoom = 0,
					MaxPerRoom = 1,
					CannotCoexistWith = new[] { PopulateableFeatureKind.Npc }
				},
				new FeatureTypeRule
				{
					Kind = PopulateableFeatureKind.Trap,
					Weight = 4,
					MinPerRoom = 0,
					MaxPerRoom = 2
				},
				new FeatureTypeRule
				{
					Kind = PopulateableFeatureKind.Treasure,
					Weight = 3,
					MinPerRoom = 0,
					MaxPerRoom = 2
				},
				new FeatureTypeRule
				{
					Kind = PopulateableFeatureKind.Npc,
					Weight = 1,
					MinPerRoom = 0,
					MaxPerRoom = 1,
					CannotCoexistWith = new[] { PopulateableFeatureKind.Monster }
				},
				new FeatureTypeRule
				{
					Kind = PopulateableFeatureKind.Lore,
					Weight = 2,
					MinPerRoom = 0,
					MaxPerRoom = 1
				}
			}
		};
}
