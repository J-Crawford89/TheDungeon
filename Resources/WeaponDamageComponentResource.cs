#nullable enable
using Godot;

[GlobalClass]
public partial class WeaponDamageComponentResource : Resource
{
	[Export] public int NumberOfDice { get; set; }
	[Export] public DieType DiceType { get; set; } = DieType.d6;
	[Export] public bool InD20CheckPool { get; set; }
	[Export] public int FlatAmount { get; set; }
	[Export] public DamageTypeResource? DamageType { get; set; }
}
