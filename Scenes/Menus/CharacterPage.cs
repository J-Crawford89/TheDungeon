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

	public void Populate(GameRunContext context)
	{
		ArgumentNullException.ThrowIfNull(context);

		ClearChildren(_identityList);
		ClearChildren(_grantedAbilitiesList);
		ClearChildren(_futureAbilitiesList);

		var player = context.Session.Player;
		var classes = context.CharacterClasses;
		var races = context.CharacterRaces;
		var backgrounds = context.CharacterBackgrounds;
		var abilities = context.AbilityDefinitions;

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
	}

	private static void ClearChildren(VBoxContainer box)
	{
		foreach (var child in box.GetChildren())
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
