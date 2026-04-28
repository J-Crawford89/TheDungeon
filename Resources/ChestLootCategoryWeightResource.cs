#nullable enable
using Godot;

/// <summary>Weighted chest loot category row for <see cref="GameBalanceSettingsResource"/>.</summary>
[GlobalClass]
public partial class ChestLootCategoryWeightResource : Resource
{
	[Export] public ChestLootCategory Category { get; set; }

	[Export] public double Weight { get; set; } = 1;
}
