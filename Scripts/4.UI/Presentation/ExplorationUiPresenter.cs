using System;
using System.Collections.Generic;

public sealed class ExplorationUiPresenter
{
	private readonly GameSessionState _session;
	private readonly ExplorationService _explorationService;
	private readonly NarrativeService _narrativeService;
	private readonly DungeonBootstrap _dungeonBootstrap;
	private readonly TreasurePickupService _treasurePickup;
	private readonly ContainerLootInteractionService _containerLoot;
	private readonly TrapService _trapService;
	private readonly PotionEffectApplicationService _potionEffects;
	private readonly Action<UiRefreshFlags> _refreshHud;

	private readonly bool _useProceduralFloor = true; //Set this bool to true in order to use procedural generation, or false to use prototype hand built floor.

	public ExplorationUiPresenter(
		GameSessionState session,
		ExplorationService explorationService,
		NarrativeService narrativeService,
		TreasurePickupService treasurePickup,
		ContainerLootInteractionService containerLoot,
		TrapService trapService,
		PotionEffectApplicationService potionEffects,
		DungeonBootstrap dungeonBootstrap,
		Action<UiRefreshFlags> refreshHud)
	{
		_session = session;
		_explorationService = explorationService;
		_narrativeService = narrativeService;
		_treasurePickup = treasurePickup;
		_containerLoot = containerLoot;
		_trapService = trapService;
		_potionEffects = potionEffects;
		_dungeonBootstrap = dungeonBootstrap;
		_refreshHud = refreshHud;
	}

	public void OnPotionPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;
		_potionEffects.TryUseHealthPotion(_session);
		_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Character | UiRefreshFlags.Command);
	}

	public void OnDisarmWithTarget(TargetPayload payload)
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;
		if (payload.Kind != TargetPayloadKind.DisarmTrapInstance)
			return;
		_trapService.TryDisarmAtSlot(_session, payload.TrapFeatureOrdinal, payload.TrapIndexInFeature);
		_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Character | UiRefreshFlags.Command | UiRefreshFlags.MainView);
	}

	public void OnTakeWithTarget(TargetPayload payload)
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;
		switch (payload.Kind)
		{
			case TargetPayloadKind.TakeTreasureItem:
				_treasurePickup.TakeTreasureInstanceAtSlot(_session, payload.TreasureFeatureOrdinal,
					payload.TreasureItemIndexInFeature);
				break;
			case TargetPayloadKind.TakeAllEligibleTreasure:
				_treasurePickup.TakeAllEligibleFromCurrentRoom(_session);
				break;
			case TargetPayloadKind.LootContainerAll:
				_containerLoot.TryLootAll(_session, payload.ContainerOrdinal);
				break;
			default:
				return;
		}

		_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Character | UiRefreshFlags.Command | UiRefreshFlags.MainView);
	}

	public void BootstrapDungeon()
	{
		var initialFloor = _dungeonBootstrap.CreateInitialFloor(_useProceduralFloor);
		_session.Dungeon.Floors.Clear();
		_session.Dungeon.Floors.Add(initialFloor);
		_session.Dungeon.DiscoveredRoomsByFloor.Clear();
		_session.Dungeon.DiscoveredRoomsByFloor.Add(initialFloor.Level, new HashSet<RoomCoord>([initialFloor.Entrance]));
		_session.Dungeon.CurrentFloor = initialFloor;
		_session.Dungeon.PlayerCoord = initialFloor.Entrance;
		_session.Dungeon.ClearRoomIngress();

		_session.AppendGameLog(_narrativeService.ForEnterDungeon());
		_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.MainView | UiRefreshFlags.Command | UiRefreshFlags.Map | UiRefreshFlags.Character);
	}

	public void OnForwardPressed()
	{
		var previousCoord = _session.Dungeon.PlayerCoord;
		var floorLevel = _session.Dungeon.CurrentFloor?.Level ?? 0;
		var result = _explorationService.MoveForward(_session);
		_session.AppendGameLog(_narrativeService.ForMoveForward(result));
		if (result.Success)
			_explorationService.TryBeginCombatIfHostile(_session, previousCoord, floorLevel);
		if (!TryReportDiagnosticAndRefreshAll(result))
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.MainView | UiRefreshFlags.Command | UiRefreshFlags.Map | UiRefreshFlags.Character);
	}

	public void OnBackwardPressed()
	{
		var result = _explorationService.AboutFace(_session.Player);
		var line = _narrativeService.ForAboutFace(result);
		if (!string.IsNullOrEmpty(line))
			_session.AppendGameLog(line);
		if (!TryReportDiagnosticAndRefreshAll(result) && !string.IsNullOrEmpty(line))
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.MainView);
	}

	public void OnTurn(DirectionTurned direction)
	{
		var result = _explorationService.Turn(_session.Player, direction);
		var line = _narrativeService.ForTurn(result, direction);
		if (!string.IsNullOrEmpty(line))
			_session.AppendGameLog(line);
		if (!TryReportDiagnosticAndRefreshAll(result) && !string.IsNullOrEmpty(line))
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.MainView);
	}

	public void OnInspectPressed()
	{
		var result = _explorationService.Inspect(_session);
		_session.AppendGameLog(_narrativeService.ForInspect(result));
		if (result.Success && result.InspectData is { } inspectData)
		{
			foreach (var line in _narrativeService.GetInspectImportantLogLines(inspectData))
				_session.AppendLog(new LogEntry { Kind = LogEntryKind.Important, Text = line });
		}

		if (!TryReportDiagnosticAndRefreshAll(result))
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Map | UiRefreshFlags.MainView | UiRefreshFlags.Command);
	}

	public void OnFloorUpPressed()
	{
		var result = _explorationService.MoveUpAFloor(_session);
		_session.AppendGameLog(_narrativeService.ForMoveUpFloor(result));
		if (!TryReportDiagnosticAndRefreshAll(result))
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.MainView | UiRefreshFlags.Command | UiRefreshFlags.Map | UiRefreshFlags.Character);
	}

	public void OnFloorDownPressed()
	{
		var previousCoord = _session.Dungeon.PlayerCoord;
		var result = _explorationService.MoveDownAFloor(_session);
		_session.AppendGameLog(_narrativeService.ForMoveDownFloor(result));
		if (result.Success && result.FloorAfterMove is { } floorLevel)
			_explorationService.TryBeginCombatIfHostile(_session, previousCoord, floorLevel);
		if (!TryReportDiagnosticAndRefreshAll(result))
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.MainView | UiRefreshFlags.Command | UiRefreshFlags.Map | UiRefreshFlags.Character);
	}

	private bool TryReportDiagnosticAndRefreshAll(ExplorationServiceResult result)
	{
		if (string.IsNullOrWhiteSpace(result.DiagnosticDetail))
			return false;
		_session.Debug.Report(_session, result.DiagnosticDetail);
		if (!_session.Debug.IsCaptureEnabled)
			return false;
		_refreshHud(UiRefreshFlags.All);
		return true;
	}
}
