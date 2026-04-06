#nullable enable
using System;
using System.Collections.Generic;

public sealed class CharacterCreationService
{
	private static readonly AbilityScore[] RolledScoreOrder = Enum.GetValues<AbilityScore>();

	private readonly DiceRollService _diceRollService;
	private readonly Random _random;
	private readonly IAbilityDefinitionRepository _abilityDefinitions;

	public CharacterCreationService(
		DiceRollService diceRollService,
		Random random,
		IAbilityDefinitionRepository abilityDefinitions)
	{
		_diceRollService = diceRollService;
		_random = random;
		_abilityDefinitions = abilityDefinitions;
	}

	public void RollAndApplyRolledScores(CharacterCreationState state)
	{
		var rolled = state.RolledAbilityScores;
		foreach (var ability in RolledScoreOrder)
		{
			var die = _diceRollService.Roll(DieType.d3);
			SetAbility(rolled, ability, die.RolledValue);
		}

		ApplyReduction(rolled);
		RecomputeFinalAbilityScores(state);
	}

	private void ApplyReduction(AbilityScores rolled)
	{
		while (SumAbilities(rolled) >= 8)
		{
			var candidates = new List<AbilityScore>();
			var minAboveFloor = int.MaxValue;
			foreach (var ability in RolledScoreOrder)
			{
				var v = rolled.GetScore(ability);
				if (v <= -2)
					continue;
				if (v < minAboveFloor)
				{
					minAboveFloor = v;
					candidates.Clear();
					candidates.Add(ability);
				}
				else if (v == minAboveFloor)
				{
					candidates.Add(ability);
				}
			}

			if (candidates.Count == 0)
				break;

			var pick = candidates[_random.Next(candidates.Count)];
			SetAbility(rolled, pick, rolled.GetScore(pick) - 1);
		}
	}

	public void RecomputeFinalAbilityScores(CharacterCreationState state)
	{
		state.FinalAbilityScores = AbilityScoresCopy.From(state.RolledAbilityScores);
		// TODO: add modifiers from SelectedClass / SelectedRace / SelectedBackground when those rules exist.
	}

	public void ApplyToPlayer(CharacterCreationState state, PlayerState player)
	{
		ArgumentNullException.ThrowIfNull(state);
		ArgumentNullException.ThrowIfNull(player);

		player.Name = CharacterNameValidator.Sanitize(state.Name).Trim();
		if (string.IsNullOrEmpty(player.Name))
			player.Name = "Adventurer";

		player.Sex = state.Sex;
		player.AbilityScores = AbilityScoresCopy.From(state.FinalAbilityScores);

		var classHp = state.SelectedClass?.BaseHp ?? 0;
		var raceHp = state.SelectedRace?.BaseHp ?? 0;
		var con = state.FinalAbilityScores.Constitution;
		var maxHp = classHp + raceHp + con;
		player.MaxHp = maxHp;
		player.CurrentHp = maxHp;

		player.Gold = state.SelectedBackground?.StartingGold ?? 0;
		player.Level = 1;
		player.HealthPotionCount = 0;
		player.Facing = HorizontalDirection.North;

		player.GrantedAbilities = PlayerAbilityGrantBuilder.Build(
			state.SelectedClass,
			state.SelectedRace,
			state.SelectedBackground,
			player.Level,
			_abilityDefinitions);

		if (player.HasAbility(AbilityIds.Spellcasting))
		{
			player.CurrentSpellPoints = 10;
			player.MaxSpellPoints = 10;
		}
		else
		{
			player.CurrentSpellPoints = null;
			player.MaxSpellPoints = null;
		}

		// TODO: merge StartingEquipment from class/race/background into inventory when equipment is modeled.
	}

	private static int SumAbilities(AbilityScores s) =>
		s.Might + s.Constitution + s.Dexterity + s.Agility + s.Intelligence + s.Wisdom + s.Gravitas + s.Luck;

	private static void SetAbility(AbilityScores scores, AbilityScore ability, int value)
	{
		switch (ability)
		{
			case AbilityScore.Might:
				scores.Might = value;
				break;
			case AbilityScore.Constitution:
				scores.Constitution = value;
				break;
			case AbilityScore.Dexterity:
				scores.Dexterity = value;
				break;
			case AbilityScore.Agility:
				scores.Agility = value;
				break;
			case AbilityScore.Intelligence:
				scores.Intelligence = value;
				break;
			case AbilityScore.Wisdom:
				scores.Wisdom = value;
				break;
			case AbilityScore.Gravitas:
				scores.Gravitas = value;
				break;
			case AbilityScore.Luck:
				scores.Luck = value;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(ability));
		}
	}
}
