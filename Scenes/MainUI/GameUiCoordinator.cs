using System;

public sealed class GameUiCoordinator
{
	private readonly GameSessionState _session;
	private readonly ExplorationUiPresenter _exploration;
	private readonly CombatUiPresenter _combat;
	private readonly MainViewPanel _mainViewPanel;
	private readonly CharacterPanel _characterPanel;
	private readonly LogPanel _logPanel;
	private readonly CommandPanel _commandPanel;
	private readonly MapPanel _mapPanel;

	public GameUiCoordinator(
		GameSessionState session,
		ExplorationUiPresenter exploration,
		CombatUiPresenter combat,
		MainViewPanel mainViewPanel,
		CharacterPanel characterPanel,
		LogPanel logPanel,
		CommandPanel commandPanel,
		MapPanel mapPanel)
	{
		_session = session;
		_exploration = exploration;
		_combat = combat;
		_mainViewPanel = mainViewPanel;
		_characterPanel = characterPanel;
		_logPanel = logPanel;
		_commandPanel = commandPanel;
		_mapPanel = mapPanel;

		_session.Dungeon.PlayerMoved += OnDungeonMapViewInvalidated;
	}

	private void OnDungeonMapViewInvalidated() =>
		RefreshHud(UiRefreshFlags.Map);

	public void RefreshHud(UiRefreshFlags flags)
	{
		if (flags.HasFlag(UiRefreshFlags.MainView))
		{
			var verticalConnection = FloorConnectionType.None;
			if (_session.Dungeon.CurrentRoom is { } currentRoom)
				verticalConnection = GetExitType(currentRoom);
			var mainViewModel = MainViewPresentationBuilder.Build(_session, verticalConnection);
			_mainViewPanel.Render(mainViewModel);
		}

		if (flags.HasFlag(UiRefreshFlags.Character))
			_characterPanel.Render(_session.Player);

		if (flags.HasFlag(UiRefreshFlags.Log))
			_logPanel.SyncFromSession(_session);

		if (flags.HasFlag(UiRefreshFlags.Command))
		{
			_commandPanel.ApplyDungeonMode(_session.Dungeon.DungeonMode);
			var mode = _session.Dungeon.DungeonMode;
			if (mode == DungeonMode.Exploration)
				_commandPanel.RenderFloorExitButtons(_session.Dungeon.CurrentRoom);
			if (mode == DungeonMode.Combat)
				_commandPanel.ApplyCombatItemButtons(_session.Player);
			var inPlay = mode == DungeonMode.Exploration || mode == DungeonMode.Combat;
			_commandPanel.ApplyTakeButtonVisible(inPlay && TreasurePickupService.HasTakeableLootInCurrentRoom(_session));
		}

		if (flags.HasFlag(UiRefreshFlags.Map))
			_mapPanel.RefreshMap(_session);
	}

	public void OnForwardPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;
		_exploration.OnForwardPressed();
	}

	public void OnBackwardPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;
		_exploration.OnBackwardPressed();
	}

	public void OnTurn(DirectionTurned direction)
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;
		_exploration.OnTurn(direction);
	}

	public void OnInspectPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;
		_exploration.OnInspectPressed();
	}

	public void OnFloorUpPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;
		_exploration.OnFloorUpPressed();
	}

	public void OnFloorDownPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;
		_exploration.OnFloorDownPressed();
	}

	public void OnAttackPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		_combat.OnAttackPressed();
	}

	public void OnFleePressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		_combat.OnFleePressed();
	}

	public void OnTakePressed()
	{
		if (_session.Dungeon.DungeonMode == DungeonMode.Exploration)
			_exploration.OnTakePressed();
		else if (_session.Dungeon.DungeonMode == DungeonMode.Combat)
			_combat.OnTakePressed();
	}

	public void OnPotionPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		_combat.OnPotionPressed();
	}

	private static FloorConnectionType GetExitType(DungeonRoom room)
	{
		if (RoomFeatureHelper.HasFeature<FloorExitFeature>(room))
			return RoomFeatureHelper.GetFeature<FloorExitFeature>(room).ExitType;

		return FloorConnectionType.None;
	}
}
