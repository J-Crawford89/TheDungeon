using System.Collections.Generic;

public sealed class FloorGenerationParameters
{
	public int MinRooms { get; init; } = 8;
	public int MaxRooms { get; init; } = 14;
	public int Seed { get; init; }
	public double DoorChance { get; init; } = 0.15;
	public int CurrentFloorCount { get; init; } = 0;
	public FloorConnectionType PreviousFloorConnectionType { get; init; } = FloorConnectionType.None;

	public IReadOnlyDictionary<FloorConnectionType, double>? VerticalConnectionWeights { get; init; }

	public RoomFeaturePopulationParameters? RoomFeatures { get; init; }
}