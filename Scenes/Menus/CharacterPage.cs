#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CharacterPage : MarginContainer
{
	[Export] private VBoxContainer _identityList = null!;
	[Export] private VBoxContainer _grantedAbilitiesList = null!;
	[Export] private VBoxContainer _futureAbilitiesList = null!;
	[Export] private PackedScene _collapsibleInfoRowScene = null!;

	[Export] private GridContainer _damageReductionsGrid = null!;

	[Export] private Label _hpLabel = null!;
	[Export] private Label _ecLabel = null!;
	[Export] private Label _armorBonusLabel = null!;
	[Export] private Label _levelLabel = null!;
	[Export] private Label _experienceLabel = null!;
	[Export] private Label _spLabel = null!;
	[Export] private Label _mightLabel = null!;
	[Export] private Label _constitutionLabel = null!;
	[Export] private Label _dexterityLabel = null!;
	[Export] private Label _agilityLabel = null!;
	[Export] private Label _intelligenceLabel = null!;
	[Export] private Label _wisdomLabel = null!;
	[Export] private Label _gravitasLabel = null!;
	[Export] private Label _luckLabel = null!;

	public void Populate(GameRunContext context)
	{
		ArgumentNullException.ThrowIfNull(context);

		ClearChildren(_identityList);
		ClearChildren(_grantedAbilitiesList);
		ClearChildren(_futureAbilitiesList);
		ClearChildren(_damageReductionsGrid);

		var player = context.Session.Player;
		PlayerDefenseAggregationHelper.RecomputeFromEquippedArmor(player);
		var classes = context.CharacterClasses;
		var races = context.CharacterRaces;
		var backgrounds = context.CharacterBackgrounds;
		var abilities = context.AbilityDefinitions;
		var scores = player.AbilityScores;

		_hpLabel.Text = $"HP: {player.CurrentHp} / {player.MaxHp}";
		var effectiveAgility = CombatFormulas.PlayerEffectiveAgility(scores.Agility, player.TotalAgilityPenalty);
		_ecLabel.Text = $"EC: {CombatFormulas.PlayerEvasionClass(effectiveAgility)}";
		_armorBonusLabel.Text = $"Armor Bonus: {player.TotalArmorBonus}";
		_levelLabel.Text = $"Level: {player.Level}";
		_experienceLabel.Text = $"XP: {player.Experience}";
		if (player.CurrentSpellPoints is { } curSp && player.MaxSpellPoints is { } maxSp)
		{
			_spLabel.Visible = true;
			_spLabel.Text = $"SP: {curSp} / {maxSp}";
		}
		else
			_spLabel.Visible = false;

		_mightLabel.Text = $"Might: {scores.Might}";
		_constitutionLabel.Text = $"Constitution: {scores.Constitution}";
		_dexterityLabel.Text = $"Dexterity: {scores.Dexterity}";
		_agilityLabel.Text = $"Agility: {scores.Agility}";
		_intelligenceLabel.Text = $"Intelligence: {scores.Intelligence}";
		_wisdomLabel.Text = $"Wisdom: {scores.Wisdom}";
		_gravitasLabel.Text = $"Gravitas: {scores.Gravitas}";
		_luckLabel.Text = $"Luck: {scores.Luck}";

		AddDefinitionIdentity(_identityList, "Race", player.CharacterRaceId, TryRace(races, player.CharacterRaceId));
		AddDefinitionIdentity(_identityList, "Class", player.CharacterClassId, TryClass(classes, player.CharacterClassId));
		AddDefinitionIdentity(_identityList, "Background", player.CharacterBackgroundId, TryBackground(backgrounds, player.CharacterBackgroundId));

		foreach (var ga in player.GrantedAbilities.OrderBy(g => g.AbilityId, StringComparer.Ordinal))
		{
			var def = abilities.TryGetById(ga.AbilityId);
			var title = def?.Name ?? ga.AbilityId;
			var desc = def?.Description ?? "";
			if (ga.LevelGranted > 0)
				desc = string.IsNullOrEmpty(desc) ? $"Granted at level {ga.LevelGranted}." : $"{desc}\n\nGranted at level {ga.LevelGranted}.";
			AddRow(_grantedAbilitiesList, title, desc);
		}

		var futureRows = PlayerAbilityGrantBuilder.EnumerateFutureAbilitiesNotYetGranted(new FutureAbilityEnumerationRequest
		{
			ClassDefinitionId = player.CharacterClassId,
			RaceDefinitionId = player.CharacterRaceId,
			BackgroundDefinitionId = player.CharacterBackgroundId,
			CharacterClasses = classes,
			CharacterRaces = races,
			CharacterBackgrounds = backgrounds,
			GrantedAbilityIds = player.GrantedAbilities.Select(g => g.AbilityId).ToList(),
			AbilityDefinitions = abilities
		});

		foreach (var (abilityId, minLevel) in futureRows)
		{
			var def = abilities.TryGetById(abilityId);
			var title = def == null ? abilityId : $"{def.Name} (level {minLevel})";
			var desc = def?.Description ?? "";
			AddRow(_futureAbilitiesList, title, desc);
		}

		foreach (var familyReduction in player.DamageReductionByDamageFamily)
		{
			AddDamageReductionLabel($"{familyReduction.Key}: {familyReduction.Value}");
		}

		foreach (var typeReduction in player.DamageReductionByDamageTypeId)
		{
			AddDamageReductionLabel($"{typeReduction.Key}: {typeReduction.Value}");
		}
	}

	private void AddDamageReductionLabel(string text)
	{
		var label = new Label()
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Left,
			SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand
        };
		_damageReductionsGrid.AddChild(label);
    }

    private static void ClearChildren(Node parent)
	{
		foreach (var child in parent.GetChildren())
			child.QueueFree();
	}

	private void AddDefinitionIdentity(VBoxContainer parent, string label, string definitionId, object? definition)
	{
		if (string.IsNullOrWhiteSpace(definitionId))
		{
			AddRow(parent, $"{label}: (not set)", "");
			return;
		}

		switch (definition)
		{
			case CharacterRaceDefinition r:
				AddRow(parent, $"{label}: {r.Name}", r.Description);
				break;
			case CharacterClassDefinition c:
				AddRow(parent, $"{label}: {c.Name}", c.Description);
				break;
			case CharacterBackgroundDefinition b:
				AddRow(parent, $"{label}: {b.Name}", b.Description);
				break;
			default:
				AddRow(parent, $"{label}: (unknown id: {definitionId.Trim()})", "");
				break;
		}
	}

	private void AddRow(VBoxContainer parent, string title, string description)
	{
		var row = _collapsibleInfoRowScene.Instantiate<CollapsibleInfoRow>();
		row.SetContent(title, description);
		parent.AddChild(row);
	}

	private static CharacterRaceDefinition? TryRace(ICharacterRaceDefinitionRepository repo, string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		var key = id.Trim();
		foreach (var d in repo.All)
		{
			if (d.Id == key)
				return d;
		}

		return null;
	}

	private static CharacterClassDefinition? TryClass(ICharacterClassDefinitionRepository repo, string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		var key = id.Trim();
		foreach (var d in repo.All)
		{
			if (d.Id == key)
				return d;
		}

		return null;
	}

	private static CharacterBackgroundDefinition? TryBackground(ICharacterBackgroundDefinitionRepository repo, string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		var key = id.Trim();
		foreach (var d in repo.All)
		{
			if (d.Id == key)
				return d;
		}

		return null;
	}
}
