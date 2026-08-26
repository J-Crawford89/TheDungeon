using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
	private readonly IContainerLootOverlayOpener? _lootOverlay;
	private readonly bool _useHandBuiltDebugFloor;
	private bool _isResolvingAction;

	public ExplorationUiPresenter(
		GameSessionState session,
		ExplorationService explorationService,
		NarrativeService narrativeService,
		TreasurePickupService treasurePickup,
		ContainerLootInteractionService containerLoot,
		TrapService trapService,
		PotionEffectApplicationService potionEffects,
		DungeonBootstrap dungeonBootstrap,
		Action<UiRefreshFlags> refreshHud,
		IContainerLootOverlayOpener? lootOverlay = null,
		bool useHandBuiltDebugFloor = false)
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
		_lootOverlay = lootOverlay;
		_useHandBuiltDebugFloor = useHandBuiltDebugFloor;
	}

	public async Task OnPotionPressedAsync()
	{
		if (_isResolvingAction)
			return;
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;
		_isResolvingAction = true;
		try
		{
			await _potionEffects.TryUseHealthPotionAsync(_session);
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Character | UiRefreshFlags.Command);
		}
		finally
		{
			_isResolvingAction = false;
		}
	}

	public async Task OnDisarmWithTargetAsync(TargetPayload payload)
	{
		if (_isResolvingAction)
			return;
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;
		if (payload.Kind != TargetPayloadKind.DisarmTrapInstance)
			return;
		_isResolvingAction = true;
		try
		{
			await _trapService.TryDisarmAtSlotAsync(_session, payload.TrapFeatureOrdinal, payload.TrapIndexInFeature);
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Character | UiRefreshFlags.Command | UiRefreshFlags.MainView);
		}
		finally
		{
			_isResolvingAction = false;
		}
	}

	public void OnTakeWithTarget(TargetPayload payload)
	{
		if (_isResolvingAction)
			return;
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
			default:
				return;
		}

		_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Character | UiRefreshFlags.Command | UiRefreshFlags.MainView);
	}

	public async Task OnOpenContainerWithTargetAsync(TargetPayload payload)
	{
		if (_isResolvingAction)
			return;
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;
		if (payload.Kind != TargetPayloadKind.LootContainerAll)
			return;
		if (_lootOverlay != null)
		{
			_lootOverlay.OpenLootPanel(payload.ContainerOrdinal);
			return;
		}

		_isResolvingAction = true;
		try
		{
			await _containerLoot.TryLootAllAsync(_session, payload.ContainerOrdinal);
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Character | UiRefreshFlags.Command | UiRefreshFlags.MainView);
		}
		finally
		{
			_isResolvingAction = false;
		}
	}

	public void BootstrapDungeon()
	{
		if (_isResolvingAction)
			return;
		var initialFloor = _dungeonBootstrap.CreateInitialFloor(_useHandBuiltDebugFloor);
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

	public async Task OnForwardPressedAsync()
	{
		if (_isResolvingAction)
			return;
		_isResolvingAction = true;
		try
		{
			var previousCoord = _session.Dungeon.PlayerCoord;
			var floorLevel = _session.Dungeon.CurrentFloor?.Level ?? 0;
			var result = _explorationService.MoveForward(_session);
			_session.AppendGameLog(_narrativeService.ForMoveForward(result));
			await RefreshThenBeginCombatIfNeededAsync(result, previousCoord, floorLevel);
		}
		finally
		{
			_isResolvingAction = false;
		}
	}

	public void OnBackwardPressed()
	{
		if (_isResolvingAction)
			return;
		var result = _explorationService.AboutFace(_session.Player);
		var line = _narrativeService.ForAboutFace(result);
		if (!string.IsNullOrEmpty(line))
			_session.AppendGameLog(line);
		if (!TryReportDiagnosticAndRefreshAll(result) && !string.IsNullOrEmpty(line))
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.MainView);
	}

	public void OnTurn(DirectionTurned direction)
	{
		if (_isResolvingAction)
			return;
		var result = _explorationService.Turn(_session.Player, direction);
		var line = _narrativeService.ForTurn(result, direction);
		if (!string.IsNullOrEmpty(line))
			_session.AppendGameLog(line);
		if (!TryReportDiagnosticAndRefreshAll(result) && !string.IsNullOrEmpty(line))
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.MainView);
	}

	public async Task OnInspectPressedAsync()
	{
		if (_isResolvingAction)
			return;
		_isResolvingAction = true;
		try
		{
			var result = await _explorationService.InspectAsync(_session);
			_session.AppendGameLog(_narrativeService.ForInspect(result));
			if (result.Success && result.InspectData is { } inspectData)
			{
				foreach (var line in _narrativeService.GetInspectImportantLogLines(inspectData))
					_session.AppendLog(new LogEntry { Kind = LogEntryKind.Important, Text = line });
			}

			if (!TryReportDiagnosticAndRefreshAll(result))
				_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Map | UiRefreshFlags.MainView | UiRefreshFlags.Command);
		}
		finally
		{
			_isResolvingAction = false;
		}
	}

	public void OnFloorUpPressed()
	{
		if (_isResolvingAction)
			return;
		var result = _explorationService.MoveUpAFloor(_session);
		_session.AppendGameLog(_narrativeService.ForMoveUpFloor(result));
		if (!TryReportDiagnosticAndRefreshAll(result))
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.MainView | UiRefreshFlags.Command | UiRefreshFlags.Map | UiRefreshFlags.Character);
	}

	public async Task OnFloorDownPressedAsync()
	{
		if (_isResolvingAction)
			return;
		_isResolvingAction = true;
		try
		{
			var previousCoord = _session.Dungeon.PlayerCoord;
			var result = _explorationService.MoveDownAFloor(_session);
			_session.AppendGameLog(_narrativeService.ForMoveDownFloor(result));
			var floorLevel = result.FloorAfterMove ?? (_session.Dungeon.CurrentFloor?.Level ?? 0);
			await RefreshThenBeginCombatIfNeededAsync(result, previousCoord, floorLevel);
		}
		finally
		{
			_isResolvingAction = false;
		}
	}

	private async Task RefreshThenBeginCombatIfNeededAsync(
		ExplorationServiceResult result,
		RoomCoord previousCoord,
		int floorLevel)
	{
		var diagnosticRefresh = TryReportDiagnosticAndRefreshAll(result);
		if (!diagnosticRefresh)
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.MainView | UiRefreshFlags.Command | UiRefreshFlags.Map | UiRefreshFlags.Character);
		if (!result.Success)
			return;
		await _explorationService.TryBeginCombatIfHostileAsync(_session, previousCoord, floorLevel);
		if (!diagnosticRefresh)
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
