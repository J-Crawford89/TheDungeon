#nullable enable
using System;
using System.Collections.Generic;

public sealed class PlayerProficiencyAggregationService
{
	private readonly ICharacterRaceDefinitionRepository _races;
	private readonly ICharacterClassDefinitionRepository _classes;
	private readonly ICharacterBackgroundDefinitionRepository _backgrounds;

	public PlayerProficiencyAggregationService(
		ICharacterRaceDefinitionRepository races,
		ICharacterClassDefinitionRepository classes,
		ICharacterBackgroundDefinitionRepository backgrounds)
	{
		_races = races;
		_classes = classes;
		_backgrounds = backgrounds;
	}

	public void Recompute(PlayerState player)
	{
		var merged = new Dictionary<ProficiencyKey, ProficiencyRank>();

		void Consider(ProficiencyGrant grant)
		{
			if (grant.Rank == ProficiencyRank.Untrained)
				return;
			var id = grant.Key.TargetId?.Trim() ?? "";
			if (id.Length == 0)
				return;

			var key = new ProficiencyKey(grant.Key.TargetType, id);
			if (!merged.TryGetValue(key, out var existing) || (int)grant.Rank > (int)existing)
				merged[key] = grant.Rank;
		}

		var cls = FindById(player.CharacterClassId, _classes.All);
		var race = FindById(player.CharacterRaceId, _races.All);
		var bg = FindById(player.CharacterBackgroundId, _backgrounds.All);

		if (cls != null)
		{
			foreach (var g in cls.ProficiencyGrants)
				Consider(g);
		}

		if (race != null)
		{
			foreach (var g in race.ProficiencyGrants)
				Consider(g);
		}

		if (bg != null)
		{
			foreach (var g in bg.ProficiencyGrants)
				Consider(g);
		}

		foreach (var inst in EnumerateDistinctWeaponInstances(player.InventoryState))
		{
			if (inst.Definition is not WeaponDefinition w)
				continue;
			if (w.OwnershipProficiencyRank == ProficiencyRank.Untrained)
				continue;
			var wid = w.Id?.Trim() ?? "";
			if (wid.Length == 0)
				continue;

			var key = new ProficiencyKey(ProficiencyTargetType.Weapon, wid);
			if (!merged.TryGetValue(key, out var existing) || (int)w.OwnershipProficiencyRank > (int)existing)
				merged[key] = w.OwnershipProficiencyRank;
		}

		player.Proficiencies = merged;
	}

	private static CharacterClassDefinition? FindById(string? id, IReadOnlyList<CharacterClassDefinition> all)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		var t = id.Trim();
		foreach (var d in all)
		{
			if (string.Equals(d.Id, t, StringComparison.Ordinal))
				return d;
		}

		return null;
	}

	private static CharacterRaceDefinition? FindById(string? id, IReadOnlyList<CharacterRaceDefinition> all)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		var t = id.Trim();
		foreach (var d in all)
		{
			if (string.Equals(d.Id, t, StringComparison.Ordinal))
				return d;
		}

		return null;
	}

	private static CharacterBackgroundDefinition? FindById(string? id, IReadOnlyList<CharacterBackgroundDefinition> all)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		var t = id.Trim();
		foreach (var d in all)
		{
			if (string.Equals(d.Id, t, StringComparison.Ordinal))
				return d;
		}

		return null;
	}

	private static IEnumerable<ItemInstance> EnumerateDistinctWeaponInstances(InventoryState inventory)
	{
		var seen = new HashSet<Guid>();
		foreach (var inst in inventory.Items)
		{
			if (inst.Quantity <= 0 || inst.Definition is not WeaponDefinition)
				continue;
			if (!seen.Add(inst.InstanceId))
				continue;
			yield return inst;
		}

		foreach (var inst in inventory.EquippedBySlot.Values)
		{
			if (inst == null || inst.Quantity <= 0 || inst.Definition is not WeaponDefinition)
				continue;
			if (!seen.Add(inst.InstanceId))
				continue;
			yield return inst;
		}
	}
}
