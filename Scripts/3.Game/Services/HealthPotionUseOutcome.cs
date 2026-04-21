/// <summary>Outcome of attempting to drink a health potion from inventory (any layer).
/// </summary>
public enum HealthPotionUseOutcome
{
	Applied,
	NoneLeft,
	AtFullHealth,
	CannotResolveDefinition
}
