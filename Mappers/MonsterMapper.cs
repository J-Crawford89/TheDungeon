using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public static class MonsterMapper
{
	public static MonsterDefinition ToDomain(MonsterResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			MaxHp = resource.MaxHp,
			Attacks = MapAttacks(resource),
			Defense = resource.Defense,
			ExperienceReward = resource.ExperienceReward,
			IsBoss = resource.IsBoss,
			RandomizerWeight = resource.RandomizerWeight
		};

	private static List<AttackDefinition> MapAttacks(MonsterResource resource)
	{
		var list = new List<AttackDefinition>();
		if (resource.Attacks == null)
			return list;

		foreach (var attack in resource.Attacks)
		{
			if (attack == null)
				continue;

			var mapped = ToAttackDefinition(attack);
			if (mapped == null)
			{
				GD.PushWarning($"MonsterMapper: monster '{resource.Id}' has an attack with no valid damage type; skipped.");
				continue;
			}

			list.Add(mapped);
		}

		return list;
	}

	private static AttackDefinition? ToAttackDefinition(AttackResource resource)
	{
		if (resource.Damage?.DamageType == null)
			return null;

		var type = DamageTypeMapper.ToDomain(resource.Damage.DamageType);
		if (string.IsNullOrWhiteSpace(type.Id))
			return null;

		var damageDice = new DiceExpression
		{
			NumberOfDice = Math.Max(0, resource.Damage.NumberOfDice),
			DieType = resource.Damage.NumberOfDice > 0 ? resource.Damage.DiceType : DieType.d6,
			InD20CheckPool = resource.Damage.InD20CheckPool
		};

		return new AttackDefinition
		{
			Name = resource.Name,
			AttackModifier = resource.AttackModifier,
			AttackItem = resource.AttackItem == null ? null : ItemMapper.ToDomain(resource.AttackItem),
			Damage = new DamageComponent(damageDice, resource.Damage.FlatAmount, type)
		};
	}
}
