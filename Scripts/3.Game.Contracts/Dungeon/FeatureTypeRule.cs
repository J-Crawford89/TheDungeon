using System.Collections.Generic;

public sealed class FeatureTypeRule
{
	public PopulateableFeatureKind Kind { get; init; }
	public double Weight { get; init; }
	public int MinPerRoom { get; init; }
	public int MaxPerRoom { get; init; }
	public bool AllowedInEntrance { get; init; } = true;
	public bool AllowedInExit { get; init; } = true;
	public IReadOnlyList<PopulateableFeatureKind> CannotCoexistWith { get; init; } = System.Array.Empty<PopulateableFeatureKind>();
}
