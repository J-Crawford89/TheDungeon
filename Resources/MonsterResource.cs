#nullable enable
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class MonsterResource : Resource
{
	[Export] public string Id { get; set; } = string.Empty;
	[Export] public string Name { get; set; } = string.Empty;
	[Export] public int MaxHp { get; set; }
	[Export] public int Might { get; set; }
	[Export] public int Constitution { get; set; }
	[Export] public int Dexterity { get; set; }
	[Export] public int Agility { get; set; }
	[Export] public int Intelligence { get; set; }
	[Export] public int Wisdom { get; set; }
	[Export] public int Gravitas { get; set; }
	[Export] public int Luck { get; set; }
	[Export] public Array<AttackResource> Attacks { get; set; } = [];
	[Export] public int Defense { get; set; }
	[Export] public int ExperienceReward { get; set; }
	[Export] public bool IsBoss { get; set; }
	[Export] public int RandomizerWeight { get; set; }

	[Export] public Texture2D? Icon { get; set; }
}
