#nullable enable
using Godot;

/// <summary>Inline sub-resource for <see cref="LootableItemResource.Harvest"/>.</summary>
[GlobalClass]
public partial class HarvestRequirementResource : Resource
{
	[Export] public int HarvestDc { get; set; }

	[Export] public AbilityScore HarvestAbility { get; set; }
}
