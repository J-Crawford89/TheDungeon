#nullable enable
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class GameBalanceSettingsResource : Resource
{
	[Export] public int MaxUnequippedBackpackRows { get; set; } = 16;
	[Export] public int DefaultFleeDc { get; set; } = 12;
	[Export] public int StartingSpellPoints { get; set; } = 10;

	[Export] public int ExperiencePerFirstRoomVisit { get; set; }
	[Export] public int ExperiencePerFloorEntry { get; set; }

	[Export] public int MinRooms { get; set; } = 6;
	[Export] public int MaxRooms { get; set; } = 12;
	[Export] public double DoorChance { get; set; } = 0.15;

	[Export] public double VerticalWeightStairs { get; set; } = 1;
	[Export] public double VerticalWeightHole { get; set; } = 1;
	[Export] public double VerticalWeightLadder { get; set; } = 1;

	[Export] public int MinFeaturesPerRoom { get; set; }
	[Export] public int MaxFeaturesPerRoom { get; set; } = 4;
	[Export] public double ContinueAfterFirstOptionalFeatureProbability { get; set; } = 0.75;
	[Export] public double DiminishMultiplierPerOptionalFeature { get; set; } = 0.65;

	[Export] public Array<FeaturePopulationRuleResource> FeatureRules { get; set; } = [];

	[Export] public int ChestMinStacks { get; set; } = 1;
	[Export] public int ChestMaxStacks { get; set; } = 3;
	[Export] public int ChestQuantityMin { get; set; } = 1;
	[Export] public int ChestQuantityMax { get; set; } = 3;

	[Export] public Array<ChestLootCategoryWeightResource> ChestLootCategoryWeights { get; set; } = [];
}
