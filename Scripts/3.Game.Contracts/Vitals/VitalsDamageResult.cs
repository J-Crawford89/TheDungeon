public sealed class VitalsDamageResult
{
	public int HpBefore { get; set; }
	public int DamageRequested { get; set; }
	public int HpAfterClamped { get; set; }
	public int HypotheticalHpAfter { get; set; }
	public int OverkillMagnitude { get; set; }
}
