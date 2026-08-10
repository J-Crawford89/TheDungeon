using System;
using System.Threading.Tasks;

public sealed class CombatUiPresenter
{
	private readonly GameSessionState _session;
	private readonly ICombatService _combatService;
	private readonly Action<UiRefreshFlags> _refreshHud;
	private bool _isResolvingAction;

	public CombatUiPresenter(GameSessionState session, ICombatService combatService, Action<UiRefreshFlags> refreshHud)
	{
		_session = session;
		_combatService = combatService;
		_refreshHud = refreshHud;
	}

	public void OnAttackWithTarget(int livingMonsterOrdinal, PlayerAttackChoice attackChoice = default)
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.IsAwaitingPlayerAction(_session))
			return;
		ExecuteAsync(() => _combatService.ExecutePlayerAttackAsync(_session, livingMonsterOrdinal, attackChoice));
	}

	public void OnFleePressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.IsAwaitingPlayerAction(_session))
			return;
		ExecuteAsync(() => _combatService.ExecutePlayerFleeAsync(_session));
	}

	public void OnTakeWithTarget(TargetPayload payload)
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.IsAwaitingPlayerAction(_session))
			return;
		ExecuteAsync(() => _combatService.ExecutePlayerTakeTreasureAsync(_session, payload));
	}

	public void OnPotionPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.IsAwaitingPlayerAction(_session))
			return;
		ExecuteAsync(() => _combatService.ExecutePlayerUseHealthPotionAsync(_session));
	}

	public void OnDefendPressed()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.IsAwaitingPlayerAction(_session))
			return;
		ExecuteAsync(() => _combatService.ExecutePlayerDefendAsync(_session));
	}

	public void OnDisarmWithTarget(TargetPayload payload)
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.IsAwaitingPlayerAction(_session))
			return;
		ExecuteAsync(() => _combatService.ExecutePlayerDisarmTrapAsync(_session, payload));
	}

	private async void ExecuteAsync(Func<Task> action)
	{
		if (_isResolvingAction)
			return;
		_isResolvingAction = true;
		try
		{
			await action();
		}
		finally
		{
			_isResolvingAction = false;
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Command | UiRefreshFlags.Character | UiRefreshFlags.MainView);
		}
	}
}
