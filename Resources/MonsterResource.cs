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
	[Export] public Array<MonsterAttackResource> Attacks { get; set; } = [];
	[Export] public ProficiencyRank DefaultAttackProficiencyRank { get; set; } = ProficiencyRank.Untrained;
	[Export] public int Defense { get; set; }
	[Export] public int ExperienceReward { get; set; }
	[Export] public bool IsBoss { get; set; }
	[Export] public int RandomizerWeight { get; set; }

	/// <summary>Loot on death; validated when spawning <see cref="CorpseFeature"/>.</summary>
	[Export] public Array<LootableItemResource> DeathLoot { get; set; } = [];

	[Export] public Texture2D? Icon { get; set; }

	/// <summary>Main-view icon for this monster&apos;s <see cref="CorpseFeature"/>; falls back to <see cref="Icon"/> when unset.</summary>
	[Export] public Texture2D? CorpseIcon { get; set; }

	/// <summary>Main-view icon after the corpse container has been opened at least once; falls back to <see cref="CorpseIcon"/> then <see cref="Icon"/>.</summary>
	[Export] public Texture2D? CorpseOpenedIcon { get; set; }
}
