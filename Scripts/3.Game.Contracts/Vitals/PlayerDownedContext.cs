public sealed class PlayerDownedContext
{
	public VitalsDamageResult Vitals { get; set; } = null!;
	public PlayerDamageSource DamageSource { get; set; } = null!;
}
