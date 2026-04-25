public static class TrapMapper
{
	public static TrapDefinition ToDomain(TrapResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			DiscoverDc = resource.DiscoverDc,
			DisarmDc = resource.DisarmDc,
			ExperienceReward = resource.ExperienceReward,
			Damage = resource.Damage,
			Effect = resource.Effect,
			IsRemovedAfterTripped = resource.IsRemovedAfterTripped,
		};
}
