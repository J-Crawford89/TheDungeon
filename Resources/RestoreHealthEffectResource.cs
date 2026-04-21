using Godot;

[GlobalClass]
public partial class RestoreHealthEffectResource : Resource
{
	[Export] public int FlatHealAmount { get; set; }

	/// <summary>Number of dice to roll for additional healing (0 = none).</summary>
	[Export] public int NumberOfDice { get; set; }

	/// <summary>Serialized as enum for the Godot inspector.</summary>
	[Export] public DieType DiceType { get; set; } = DieType.d6;
}
