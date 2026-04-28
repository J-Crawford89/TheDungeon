using System;
using System.Diagnostics;

public class DungeonBootstrap
{
	private readonly RoomFeaturePopulationService _roomFeaturePopulation;
	private readonly RoomFeaturePopulationParameters _handBuiltPopulationParameters;
	private readonly Func<FloorGenerationParameters> _createProceduralParameters;

	public DungeonBootstrap(
		RoomFeaturePopulationService roomFeaturePopulation,
		RoomFeaturePopulationParameters handBuiltPopulationParameters,
		Func<FloorGenerationParameters> createProceduralParameters)
	{
		_roomFeaturePopulation = roomFeaturePopulation;
		_handBuiltPopulationParameters = handBuiltPopulationParameters;
		_createProceduralParameters = createProceduralParameters;
	}

	public DungeonFloor CreateInitialFloor(bool useProceduralFloor = false)
	{
		if (useProceduralFloor)
			return new FloorGenerator(_roomFeaturePopulation).Generate(
				_createProceduralParameters(),
				logDiagnostic: static message => Debug.WriteLine(message));

		return HandBuiltDungeonFloor.CreateSample(_roomFeaturePopulation, _handBuiltPopulationParameters);
	}
}
