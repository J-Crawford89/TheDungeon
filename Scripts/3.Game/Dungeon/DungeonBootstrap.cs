using System;
using System.Diagnostics;

public class DungeonBootstrap
{
	private readonly RoomFeaturePopulationService _roomFeaturePopulation;

	public DungeonBootstrap(RoomFeaturePopulationService roomFeaturePopulation) =>
		_roomFeaturePopulation = roomFeaturePopulation;

	public DungeonFloor CreateInitialFloor(bool useProceduralFloor = false)
	{
		if (useProceduralFloor)
		{
			return new FloorGenerator(_roomFeaturePopulation).Generate(
				new FloorGenerationParameters
				{
					MinRooms = 6,
					MaxRooms = 12,
					Seed = Random.Shared.Next(),
					CurrentFloorCount = 0,
				},
				logDiagnostic: static message => Debug.WriteLine(message));
		}

		return HandBuiltDungeonFloor.CreateSample(_roomFeaturePopulation);
	}
}
