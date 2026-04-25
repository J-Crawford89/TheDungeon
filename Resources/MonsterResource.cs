#nullable enable
using Godot;

[GlobalClass]
public partial class MonsterResource : Resource
{
	[Export] public string Id { get; set; } = string.Empty;
	[Export] public string Name { get; set; } = string.Empty;
	[Export] public int MaxHp { get; set; }
	[Export] public int Attack { get; set; }
	[Export] public int Defense { get; set; }
	[Export] public int ExperienceReward { get; set; }
	[Export] public bool IsBoss { get; set; }
	[Export] public int RandomizerWeight { get; set; }

	[Export] public Texture2D? Icon { get; set; }
}
