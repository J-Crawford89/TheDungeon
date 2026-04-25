using System.Collections.Generic;
using System.Linq;

public sealed class PlayerState
{
	public string Name { get; set; } = "Testy McTestface";
	public CharacterSex Sex { get; set; } = CharacterSex.Male;
	/// <summary>Definition id from character resources; empty if unset.</summary>
	public string CharacterRaceId { get; set; } = "";
	/// <summary>Definition id from character resources; empty if unset.</summary>
	public string CharacterClassId { get; set; } = "";
	/// <summary>Definition id from character resources; empty if unset.</summary>
	public string CharacterBackgroundId { get; set; } = "";

	public int CurrentHp { get; set; } = 10;
	public int MaxHp { get; set; } = 10;
	public int Level { get; set; } = 1;
	public int Experience { get; set; }
	public int TotalArmorBonus { get; set; }
	public int TotalDamageReduction { get; set; }
	public int Gold { get; set; }
	public HorizontalDirection Facing { get; set; } = HorizontalDirection.North;
	public AbilityScores AbilityScores { get; set; } = new();

	public List<GrantedAbility> GrantedAbilities { get; set; } = new();

	public int? CurrentSpellPoints { get; set; }
	public int? MaxSpellPoints { get; set; }

	public InventoryState InventoryState { get; set; } = new();

	public bool HasAbility(string abilityId)
	{
		if (string.IsNullOrWhiteSpace(abilityId))
			return false;
		var id = abilityId.Trim();
		return GrantedAbilities.Any(g => g.AbilityId == id);
	}

	public void ResetToNewAdventurer()
	{
		Name = "Testy McTestface";
		Sex = CharacterSex.Male;
		CurrentHp = 10;
		MaxHp = 10;
		Level = 1;
		Experience = 0;
		TotalArmorBonus = 0;
		TotalDamageReduction = 0;
		Gold = 0;
		Facing = HorizontalDirection.North;
		AbilityScores = new AbilityScores();
		GrantedAbilities = new List<GrantedAbility>();
		CurrentSpellPoints = null;
		MaxSpellPoints = null;
		InventoryState = new InventoryState();
		CharacterRaceId = "";
		CharacterClassId = "";
		CharacterBackgroundId = "";
	}
}
