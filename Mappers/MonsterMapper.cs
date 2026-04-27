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
			AbilityScores = MapAbilityScores(resource),
			DefaultAttackProficiencyRank = resource.DefaultAttackProficiencyRank,
			Attacks = MapAttacks(resource),
			Defense = resource.Defense,
			ExperienceReward = resource.ExperienceReward,
			IsBoss = resource.IsBoss,
			RandomizerWeight = resource.RandomizerWeight
		};

	private static AbilityScores MapAbilityScores(MonsterResource resource) =>
		new()
		{
			Might = resource.Might,
			Constitution = resource.Constitution,
			Dexterity = resource.Dexterity,
			Agility = resource.Agility,
			Intelligence = resource.Intelligence,
			Wisdom = resource.Wisdom,
			Gravitas = resource.Gravitas,
			Luck = resource.Luck,
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
			if (attack is MonsterAttackResource mar)
			{
				var mapped = MonsterAttackResourceMapper.ToDomain(mar);
				if (mapped == null)
				{
					GD.PushWarning($"MonsterMapper: monster '{resource.Id}' has an attack with no valid damage components; skipped.");
					continue;
				}

				list.Add(mapped);
				continue;
			}
			GD.PushWarning($"MonsterMapper: monster '{resource.Id}' attack must be a MonsterAttackResource; skipped.");
		}
		return list;
	}
}