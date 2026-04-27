#nullable enable

public static class MonsterAttackProficiencyRank
{
	public static ProficiencyRank Resolve(MonsterDefinition definition, AttackDefinition attack)
	{
		if (attack is MonsterAttackDefinition mad)
		{
			if (mad.AttackProficiencyRank == ProficiencyRank.Untrained)
				return definition.DefaultAttackProficiencyRank;
			return mad.AttackProficiencyRank;
		}

		return definition.DefaultAttackProficiencyRank;
	}
}
