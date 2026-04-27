/// <summary>Container such as a chest or cache.</summary>
public sealed class ChestFeature : ContainerFeature
{
	public ChestFeature()
	{
		RemoveFeatureWhenEmpty = false;
	}

	public bool Locked { get; set; }
}
