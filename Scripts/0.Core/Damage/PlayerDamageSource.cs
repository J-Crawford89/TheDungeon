public sealed class PlayerDamageSource
{
	public DamageSourceType Type { get; set; }
	public string DisplayName { get; set; } = string.Empty;
	public string? DefinitionId { get; set; }
}
