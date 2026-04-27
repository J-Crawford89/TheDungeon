/// <summary>Loot on a slain creature; often left visible in the room even when empty.</summary>
public sealed class CorpseFeature : ContainerFeature
{
	public CorpseFeature()
	{
		RemoveFeatureWhenEmpty = false;
	}

	/// <summary>Optional skill check DC for harvesting (e.g. venom); 0 = none.</summary>
	public int HarvestDc { get; set; }
}
