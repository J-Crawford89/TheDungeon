/// <summary>Loot staged after disarming a trap (or similar).</summary>
public sealed class SalvageFeature : ContainerFeature
{
	public SalvageFeature()
	{
		RemoveFeatureWhenEmpty = true;
	}

	/// <summary>Trap definition id that produced this salvage; used for loot panel subtitle.</summary>
	public string SourceTrapDefinitionId { get; set; } = string.Empty;
}
