#nullable enable
using Godot;
using Godot.Collections;

/// <summary>One row of room feature mix rules; maps to <see cref="FeatureTypeRule"/>.</summary>
[GlobalClass]
public partial class FeaturePopulationRuleResource : Resource
{
	[Export] public PopulateableFeatureKind Kind { get; set; }

	[Export] public double Weight { get; set; } = 1;

	[Export] public int MinPerRoom { get; set; }

	[Export] public int MaxPerRoom { get; set; } = 1;

	[Export] public bool AllowedInEntrance { get; set; } = true;

	[Export] public bool AllowedInExit { get; set; } = true;

	/// <summary>Feature kinds that must not appear in the same room as this rule's kind.</summary>
	[Export] public Array<PopulateableFeatureKind> CannotCoexistWith { get; set; } = [];
}
