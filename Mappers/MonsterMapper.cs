public static class MonsterMapper
{
	public static MonsterDefinition ToDomain(MonsterResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			MaxHp = resource.MaxHp,
			Attack = resource.Attack,
			Defense = resource.Defense,
			IsBoss = resource.IsBoss,
			RandomizerWeight = resource.RandomizerWeight
		};
}
