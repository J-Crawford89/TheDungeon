using System;
using System.Threading.Tasks;

public sealed class CombatUiPresenter
{
	private readonly GameSessionState _session;
	private readonly ICombatService _combatService;
	private bool _isBusyResolvingAction;

	public CombatUiPresenter(GameSessionState session, ICombatService combatService)
	{
		_session = session;
		_combatService = combatService;
	}

	public event Action? StateChanged;

	public async Task OnAttackWithTargetAsync(int livingMonsterOrdinal, PlayerAttackChoice attackChoice = default)
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.CanAcceptPlayerAction(_session))
			return;
		await ExecuteAsync(() => _combatService.ExecutePlayerAttackAsync(_session, livingMonsterOrdinal, attackChoice));
	}

	public async Task OnFleePressedAsync()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.CanAcceptPlayerAction(_session))
			return;
		await ExecuteAsync(() => _combatService.ExecutePlayerFleeAsync(_session));
	}

	public async Task OnTakeWithTargetAsync(TargetPayload payload)
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.CanAcceptPlayerAction(_session))
			return;
		await ExecuteAsync(() => _combatService.ExecutePlayerTakeTreasureAsync(_session, payload));
	}

	public async Task OnPotionPressedAsync()
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.CanAcceptPlayerAction(_session))
			return;
		await ExecuteAsync(() => _combatService.ExecutePlayerUseHealthPotionAsync(_session));
	}

	public async Task OnCombatAbilityPressedAsync(string abilityId)
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.CanAcceptPlayerAction(_session))
			return;
		await ExecuteAsync(() => _combatService.ExecutePlayerCombatAbilityAsync(_session, abilityId));
	}

	public async Task OnDisarmWithTargetAsync(TargetPayload payload)
	{
		if (_session.Dungeon.DungeonMode != DungeonMode.Combat)
			return;
		if (!_combatService.CanAcceptPlayerAction(_session))
			return;
		await ExecuteAsync(() => _combatService.ExecutePlayerDisarmTrapAsync(_session, payload));
	}

	private async Task ExecuteAsync(Func<Task> action)
	{
		if (_isBusyResolvingAction)
			return;
		_isBusyResolvingAction = true;
		try
		{
			await action();
		}
		finally
		{
			_isBusyResolvingAction = false;
			StateChanged?.Invoke();
		}
	}
}
