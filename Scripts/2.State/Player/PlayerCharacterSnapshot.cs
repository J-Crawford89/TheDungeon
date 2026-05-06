using System;
using System.Collections.Generic;
using System.Linq;

public sealed class PlayerCharacterSnapshot
{
	public string Name { get; init; } = "";
	public CharacterSex Sex { get; init; }
	public int MaxHp { get; init; }
	public int Level { get; init; }
	public int Experience { get; init; }
	public CoinPurse Purse { get; init; } = new();
	public HorizontalDirection Facing { get; init; }
	public AbilityScores AbilityScores { get; init; } = new();
	/// <summary>Ability ids granted at time of snapshot (e.g. fallen adventurer).</summary>
	public IReadOnlyList<string> GrantedAbilityIds { get; init; } = Array.Empty<string>();

	public static PlayerCharacterSnapshot From(PlayerState player)
	{
		var src = player.AbilityScores;
		return new PlayerCharacterSnapshot
		{
			Name = player.Name,
			Sex = player.Sex,
			MaxHp = player.MaxHp,
			Level = player.Level,
			Experience = player.Experience,
			Purse = player.Purse.Clone(),
			Facing = player.Facing,
			AbilityScores = AbilityScoresCopy.From(src),
			GrantedAbilityIds = player.GrantedAbilities.Select(g => g.AbilityId).ToArray(),
		};
	}
}
