using System;
using System.Diagnostics;

public class DungeonBootstrap
{
	private readonly FloorGenerator _floorGenerator;
	private readonly RoomFeaturePopulationService _roomFeaturePopulation;
	private readonly RoomFeaturePopulationParameters _handBuiltPopulationParameters;
	private readonly Func<FloorGenerationParameters> _createProceduralParameters;

	public DungeonBootstrap(
		FloorGenerator floorGenerator,
		RoomFeaturePopulationService roomFeaturePopulation,
		RoomFeaturePopulationParameters handBuiltPopulationParameters,
		Func<FloorGenerationParameters> createProceduralParameters)
	{
		_floorGenerator = floorGenerator;
		_roomFeaturePopulation = roomFeaturePopulation;
		_handBuiltPopulationParameters = handBuiltPopulationParameters;
		_createProceduralParameters = createProceduralParameters;
	}

	/// <param name="useHandBuiltDebugFloor">
	/// When true (debug tools only), uses the prototype hand-built floor. Production always generates procedurally.
	/// </param>
	public DungeonFloor CreateInitialFloor(bool useHandBuiltDebugFloor = false)
	{
		if (useHandBuiltDebugFloor)
			return HandBuiltDungeonFloor.CreateSample(_roomFeaturePopulation, _handBuiltPopulationParameters);

		return _floorGenerator.Generate(
			_createProceduralParameters(),
			logDiagnostic: static message => Debug.WriteLine(message));
	}
}
