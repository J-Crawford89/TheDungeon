using System;

public sealed class CombatUiPresenter
{
	private readonly GameSessionState _session;
	private readonly ICombatService _combatService;
	private readonly Action<UiRefreshFlags> _refreshHud;

	public CombatUiPresenter(GameSessionState session, ICombatService combatService, Action<UiRefreshFlags> refreshHud)
	{
		_session = session;
		_combatService = combatService;
		_refreshHud = refreshHud;
	}

	public void OnAttackPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.IsAwaitingPlayerAction(_session))
			return;
		_combatService.ExecutePlayerAttack(_session);
		_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Command | UiRefreshFlags.Character | UiRefreshFlags.MainView);
	}

	public void OnFleePressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.IsAwaitingPlayerAction(_session))
			return;
		_combatService.ExecutePlayerFlee(_session);
		_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Command | UiRefreshFlags.Character | UiRefreshFlags.MainView);
	}

	public void OnTakePressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.IsAwaitingPlayerAction(_session))
			return;
		_combatService.ExecutePlayerTakeTreasure(_session);
		_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Command | UiRefreshFlags.Character | UiRefreshFlags.MainView);
	}

	public void OnPotionPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.IsAwaitingPlayerAction(_session))
			return;
		_combatService.ExecutePlayerUseHealthPotion(_session);
		_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Command | UiRefreshFlags.Character | UiRefreshFlags.MainView);
	}

	public void OnDefendPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.IsAwaitingPlayerAction(_session))
			return;
		_combatService.ExecutePlayerDefend(_session);
		_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Command | UiRefreshFlags.Character | UiRefreshFlags.MainView);
	}
}
