using System.Collections.Generic;

public sealed class MainViewRenderModel
{
	public required string Title { get; init; }
	public RoomConnectionType LeftConnection { get; init; }
	public RoomConnectionType FrontConnection { get; init; }
	public RoomConnectionType RightConnection { get; init; }
	public required IReadOnlyList<MainViewFeatureIconKind> FeatureIcons { get; init; }
}
