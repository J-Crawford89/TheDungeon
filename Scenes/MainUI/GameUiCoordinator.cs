#nullable enable
using System;
using System.Collections.Generic;

public sealed class GameUiCoordinator
{
	private enum TargetingKind
	{
		Attack,
		AttackWeapon,
		Take,
		OpenContainer,
		Disarm,
	}

	private sealed class ActiveTargeting
	{
		public required TargetingKind Kind { get; init; }
		public required IReadOnlyList<TargetDescriptor> Descriptors { get; init; }
	}

	private readonly GameSessionState _session;
	private readonly ICombatService _combatService;
	private readonly INarrativeTextProvider _takeNarrative;
	private readonly ExplorationUiPresenter _exploration;
	private readonly CombatUiPresenter _combat;
	private readonly MainViewPanel _mainViewPanel;
	private readonly CharacterPanel _characterPanel;
	private readonly LogPanel _logPanel;
	private readonly CommandPanel _commandPanel;
	private readonly MapPanel _mapPanel;

	private ActiveTargeting? _targeting;
	private int? _targetHoverDescriptorIndex;
	private PlayerAttackChoice? _pendingAttackChoice;

	public GameUiCoordinator(
		GameSessionState session,
		ICombatService combatService,
		INarrativeTextProvider takeNarrative,
		ExplorationUiPresenter exploration,
		CombatUiPresenter combat,
		MainViewPanel mainViewPanel,
		CharacterPanel characterPanel,
		LogPanel logPanel,
		CommandPanel commandPanel,
		MapPanel mapPanel)
	{
		_session = session;
		_combatService = combatService;
		_takeNarrative = takeNarrative;
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

	private bool IsTargetingActive => _targeting != null;

	public void OnTargetSelectionCanceled()
	{
		_pendingAttackChoice = null;
		ClearTargetingSession();
		RefreshHud(UiRefreshFlags.Command | UiRefreshFlags.MainView);
	}

	public void OnTargetSelectionPicked(int descriptorIndex)
	{
		if (_targeting == null || descriptorIndex < 0 || descriptorIndex >= _targeting.Descriptors.Count)
			return;
		var d = _targeting.Descriptors[descriptorIndex];
		var kind = _targeting.Kind;
		ClearTargetingSession();
		switch (kind)
		{
			case TargetingKind.AttackWeapon:
				OnAttackWeaponPicked(d.Payload);
				break;
			case TargetingKind.Attack:
			{
				var choice = _pendingAttackChoice ?? PlayerAttackChoice.Unarmed;
				_pendingAttackChoice = null;
				_combat.OnAttackWithTarget(d.Payload.LivingMonsterOrdinal, choice);
				break;
			}
			case TargetingKind.Take:
				if (_session.Dungeon.DungeonMode == DungeonMode.Exploration)
					_exploration.OnTakeWithTarget(d.Payload);
				else
					_combat.OnTakeWithTarget(d.Payload);
				break;
			case TargetingKind.OpenContainer:
				if (_session.Dungeon.DungeonMode == DungeonMode.Exploration)
					_exploration.OnOpenContainerWithTarget(d.Payload);
				break;
			case TargetingKind.Disarm:
				if (_session.Dungeon.DungeonMode == DungeonMode.Exploration)
					_exploration.OnDisarmWithTarget(d.Payload);
				else
					_combat.OnDisarmWithTarget(d.Payload);
				break;
		}
	}

	public void OnTargetSelectionHoverChanged(int? descriptorIndex)
	{
		_targetHoverDescriptorIndex = descriptorIndex;
		ApplyTargetHoverToMainView();
	}

	private void ClearTargetingSession()
	{
		_targeting = null;
		_targetHoverDescriptorIndex = null;
		_commandPanel.ExitTargetSelection();
		_mainViewPanel.ClearTargetingHighlight();
	}

	private void ApplyTargetHoverToMainView()
	{
		if (_targetHoverDescriptorIndex is not { } idx || _targeting == null ||
		    idx < 0 || idx >= _targeting.Descriptors.Count)
		{
			_mainViewPanel.ClearTargetingHighlight();
			return;
		}

		var d = _targeting.Descriptors[idx];
		if (d.HighlightKey != null)
			_mainViewPanel.SetTargetingHighlight(d.HighlightKey);
		else
			_mainViewPanel.SetTargetingHighlight(d.TakeAllHighlightKeys);
	}

	private static Dictionary<string, string> BuildTargetingLabels(ActiveTargeting targeting)
	{
		var d = new Dictionary<string, string>();
		foreach (var t in targeting.Descriptors)
		{
			if (t.HighlightKey != null)
				d[t.HighlightKey] = t.Label;
			foreach (var k in t.TakeAllHighlightKeys)
				d[k] = t.Label;
		}

		return d;
	}

	public void RefreshHud(UiRefreshFlags flags)
	{
		if (_session.Phase != GamePlayPhase.InProgress)
		{
			_pendingAttackChoice = null;
			ClearTargetingSession();
		}

		if (flags.HasFlag(UiRefreshFlags.Command) && _session.Phase == GamePlayPhase.GameOver)
		{
			if (IsTargetingActive)
				ClearTargetingSession();
			_commandPanel.SetAllCommandButtonsDisabled(true);
			_commandPanel.HideAllGameplayCommands();
		}
		else if (flags.HasFlag(UiRefreshFlags.Command))
			_commandPanel.SetAllCommandButtonsDisabled(false);

		if (flags.HasFlag(UiRefreshFlags.MainView))
		{
			var verticalConnection = FloorConnectionType.None;
			if (_session.Dungeon.CurrentRoom is { } currentRoom)
				verticalConnection = GetExitType(currentRoom);
			IReadOnlyDictionary<string, string>? labels = IsTargetingActive
				? BuildTargetingLabels(_targeting!)
				: null;
			var mainViewModel = MainViewPresentationBuilder.Build(_session, verticalConnection, labels);
			_mainViewPanel.Render(mainViewModel);
			if (IsTargetingActive)
				ApplyTargetHoverToMainView();
			else
				_mainViewPanel.ClearTargetingHighlight();
		}

		if (flags.HasFlag(UiRefreshFlags.Character))
			_characterPanel.Render(_session.Player, _session);

		if (flags.HasFlag(UiRefreshFlags.Log))
			_logPanel.SyncFromSession(_session);

		if (flags.HasFlag(UiRefreshFlags.Command) && _session.Phase == GamePlayPhase.InProgress && !IsTargetingActive)
		{
			_commandPanel.ApplyDungeonMode(_session.Dungeon.DungeonMode);
			var mode = _session.Dungeon.DungeonMode;
			if (mode == DungeonMode.Exploration)
			{
				_commandPanel.RenderFloorExitButtons(_session.Dungeon.CurrentRoom, _session.Player);
				_commandPanel.ApplyPotionButtonState(_session.Player);
			}

			if (mode == DungeonMode.Combat)
			{
				_commandPanel.ApplyPotionButtonState(_session.Player);
				_commandPanel.ApplyCombatAbilityButtons(_session.Player, _session, _combatService);
			}

			var inPlay = mode == DungeonMode.Exploration || mode == DungeonMode.Combat;
			_commandPanel.ApplyTakeButtonVisible(inPlay &&
			                                     TreasurePickupService.HasTakeableLootInCurrentRoom(_session));
			_commandPanel.ApplyOpenButtonVisible(mode == DungeonMode.Exploration &&
			                                     RoomContainerLocator.CurrentRoomHasAnyContainer(_session));
			_commandPanel.ApplyDisarmButtonVisible(inPlay && TrapService.CurrentRoomHasTrap(_session));
		}

		if (flags.HasFlag(UiRefreshFlags.Map))
			_mapPanel.RefreshMap(_session);
	}

	public void OnForwardPressed()
	{
		if (!CanUseExplorationCommands())
			return;
		_exploration.OnForwardPressed();
	}

	public void OnBackwardPressed()
	{
		if (!CanUseExplorationCommands())
			return;
		_exploration.OnBackwardPressed();
	}

	public void OnTurn(DirectionTurned direction)
	{
		if (!CanUseExplorationCommands())
			return;
		_exploration.OnTurn(direction);
	}

	public void OnInspectPressed()
	{
		if (!CanUseExplorationCommands())
			return;
		_exploration.OnInspectPressed();
	}

	public void OnFloorUpPressed()
	{
		if (!CanUseExplorationCommands())
			return;
		_exploration.OnFloorUpPressed();
	}

	public void OnFloorDownPressed()
	{
		if (!CanUseExplorationCommands())
			return;
		_exploration.OnFloorDownPressed();
	}

	private bool CanUseExplorationCommands()
	{
		if (_session.Phase != GamePlayPhase.InProgress)
			return false;
		if (IsTargetingActive)
			return false;
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return false;
		return true;
	}

	public void OnAttackPressed()
	{
		if (_session.Phase != GamePlayPhase.InProgress)
			return;
		if (IsTargetingActive)
			return;
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.IsAwaitingPlayerAction(_session))
			return;

		var monsters = PlayerActionTargetResolvers.ResolveAttackTargets(_session);
		if (monsters.Count == 0)
		{
			_session.AppendGameLog("There is nothing you can attack.");
			return;
		}

		var weaponChoices = PlayerAttackOptionsResolver.ResolveWeaponChoiceDescriptors(_session.Player.InventoryState);
		var needsWeaponPick = weaponChoices.Count > 1;

		if (!needsWeaponPick)
		{
			if (monsters.Count == 1)
			{
				_combat.OnAttackWithTarget(monsters[0].Payload.LivingMonsterOrdinal, PlayerAttackChoice.Unarmed);
				return;
			}

			_pendingAttackChoice = null;
			_targeting = new ActiveTargeting { Kind = TargetingKind.Attack, Descriptors = monsters };
			_targetHoverDescriptorIndex = null;
			_commandPanel.EnterTargetSelection(monsters);
			RefreshHud(UiRefreshFlags.Command | UiRefreshFlags.MainView);
			return;
		}

		if (monsters.Count == 1)
		{
			_targeting = new ActiveTargeting { Kind = TargetingKind.AttackWeapon, Descriptors = weaponChoices };
			_targetHoverDescriptorIndex = null;
			_commandPanel.EnterTargetSelection(weaponChoices);
			RefreshHud(UiRefreshFlags.Command | UiRefreshFlags.MainView);
			return;
		}

		_pendingAttackChoice = null;
		_targeting = new ActiveTargeting { Kind = TargetingKind.AttackWeapon, Descriptors = weaponChoices };
		_targetHoverDescriptorIndex = null;
		_commandPanel.EnterTargetSelection(weaponChoices);
		RefreshHud(UiRefreshFlags.Command | UiRefreshFlags.MainView);
	}

	private void OnAttackWeaponPicked(TargetPayload payload)
	{
		if (payload.Kind != TargetPayloadKind.PlayerAttackWeaponPick)
			return;

		_pendingAttackChoice = payload.AttackChoice;
		var monsters = PlayerActionTargetResolvers.ResolveAttackTargets(_session);
		if (monsters.Count == 0)
		{
			_session.AppendGameLog("There is nothing you can attack.");
			_pendingAttackChoice = null;
			return;
		}

		if (monsters.Count == 1)
		{
			var choice = _pendingAttackChoice ?? PlayerAttackChoice.Unarmed;
			_pendingAttackChoice = null;
			_combat.OnAttackWithTarget(monsters[0].Payload.LivingMonsterOrdinal, choice);
			return;
		}

		_targeting = new ActiveTargeting { Kind = TargetingKind.Attack, Descriptors = monsters };
		_targetHoverDescriptorIndex = null;
		_commandPanel.EnterTargetSelection(monsters);
		RefreshHud(UiRefreshFlags.Command | UiRefreshFlags.MainView);
	}

	public void OnFleePressed()
	{
		if (_session.Phase != GamePlayPhase.InProgress)
			return;
		if (IsTargetingActive)
			return;
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		_combat.OnFleePressed();
	}

	public void OnTakePressed()
	{
		if (_session.Phase != GamePlayPhase.InProgress)
			return;
		if (IsTargetingActive)
			return;
		var mode = _session.Dungeon.DungeonMode;
		if (mode != DungeonMode.Exploration && mode != DungeonMode.Combat)
			return;

		if (mode == DungeonMode.Combat && !_combatService.IsAwaitingPlayerAction(_session))
			return;

		var list = PlayerActionTargetResolvers.ResolveTakeTargets(_session, mode);
		if (list.Count == 0)
		{
			_session.AppendGameLog(_takeNarrative.ForTakeNothingHere());
			return;
		}

		if (list.Count == 1)
		{
			if (mode == DungeonMode.Exploration)
				_exploration.OnTakeWithTarget(list[0].Payload);
			else
				_combat.OnTakeWithTarget(list[0].Payload);
			return;
		}

		_targeting = new ActiveTargeting { Kind = TargetingKind.Take, Descriptors = list };
		_targetHoverDescriptorIndex = null;
		_commandPanel.EnterTargetSelection(list);
		RefreshHud(UiRefreshFlags.Command | UiRefreshFlags.MainView);
	}

	public void OnOpenPressed()
	{
		if (_session.Phase != GamePlayPhase.InProgress)
			return;
		if (IsTargetingActive)
			return;
		if (_session.Dungeon.DungeonMode != DungeonMode.Exploration)
			return;

		var list = PlayerActionTargetResolvers.ResolveOpenContainerTargets(_session, _session.Dungeon.DungeonMode);
		if (list.Count == 0)
		{
			_session.AppendGameLog("There is nothing here to open.");
			return;
		}

		if (list.Count == 1)
		{
			_exploration.OnOpenContainerWithTarget(list[0].Payload);
			return;
		}

		_targeting = new ActiveTargeting { Kind = TargetingKind.OpenContainer, Descriptors = list };
		_targetHoverDescriptorIndex = null;
		_commandPanel.EnterTargetSelection(list);
		RefreshHud(UiRefreshFlags.Command | UiRefreshFlags.MainView);
	}

	public void OnPotionPressed()
	{
		if (_session.Phase != GamePlayPhase.InProgress)
			return;
		if (IsTargetingActive)
			return;
		if (_session.Dungeon.DungeonMode == DungeonMode.Exploration)
			_exploration.OnPotionPressed();
		else if (_session.Dungeon.DungeonMode == DungeonMode.Combat)
			_combat.OnPotionPressed();
	}

	public void OnDefendPressed()
	{
		if (_session.Phase != GamePlayPhase.InProgress)
			return;
		if (IsTargetingActive)
			return;
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		_combat.OnDefendPressed();
	}

	public void OnDisarmPressed()
	{
		if (_session.Phase != GamePlayPhase.InProgress)
			return;
		if (IsTargetingActive)
			return;
		var mode = _session.Dungeon.DungeonMode;
		if (mode != DungeonMode.Exploration && mode != DungeonMode.Combat)
			return;

		if (mode == DungeonMode.Combat && !_combatService.IsAwaitingPlayerAction(_session))
			return;

		var list = PlayerActionTargetResolvers.ResolveDisarmTargets(_session);
		if (list.Count == 0)
		{
			_session.AppendGameLog("There is nothing here to disarm.");
			return;
		}

		if (list.Count == 1)
		{
			if (mode == DungeonMode.Exploration)
				_exploration.OnDisarmWithTarget(list[0].Payload);
			else
				_combat.OnDisarmWithTarget(list[0].Payload);
			return;
		}

		_targeting = new ActiveTargeting { Kind = TargetingKind.Disarm, Descriptors = list };
		_targetHoverDescriptorIndex = null;
		_commandPanel.EnterTargetSelection(list);
		RefreshHud(UiRefreshFlags.Command | UiRefreshFlags.MainView);
	}

	private static FloorConnectionType GetExitType(DungeonRoom room)
	{
		if (RoomFeatureHelper.HasFeature<FloorExitFeature>(room) &&
		    RoomFeatureHelper.GetFeature<FloorExitFeature>(room) is { } exit)
			return exit.ExitType;

		return FloorConnectionType.None;
	}
}
