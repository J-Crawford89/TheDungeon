/// <summary>Optional DC + ability for extracting this loot stack (e.g. monster parts).</summary>
public sealed class HarvestRequirement
{
	public int HarvestDc { get; set; }

	public AbilityScore HarvestAbility { get; set; }
}
