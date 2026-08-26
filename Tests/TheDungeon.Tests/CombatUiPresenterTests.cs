using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public sealed class CombatUiPresenterTests
{
	private const UiRefreshFlags ExpectedCombatSuccessFlags =
		UiRefreshFlags.Log | UiRefreshFlags.Command | UiRefreshFlags.Character | UiRefreshFlags.MainView;

	private sealed class RecordingCombatService : ICombatService
	{
		public bool AwaitingPlayerAction { get; set; } = true;
		public string? LastMethod { get; private set; }
		public int? LastAttackOrdinal { get; private set; }
		public PlayerAttackChoice LastAttackChoice { get; private set; }
		public TargetPayload? LastTakePayload { get; private set; }
		public TargetPayload? LastDisarmPayload { get; private set; }
		public Func<Task>? AttackOperation { get; set; }

		public Task<bool> TryBeginCombatIfHostileAsync(GameSessionState session, RoomCoord previousCoord, int floorLevel) =>
			Task.FromResult(false);

		public bool CanAcceptPlayerAction(GameSessionState session) => AwaitingPlayerAction;

		public bool CanExecuteCombatAbility(GameSessionState session, string abilityId) => false;

		public Task ExecutePlayerAttackAsync(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice)
		{
			LastMethod = nameof(ICombatService.ExecutePlayerAttackAsync);
			LastAttackOrdinal = livingMonsterOrdinal;
			LastAttackChoice = attackChoice;
			return AttackOperation?.Invoke() ?? Task.CompletedTask;
		}

		public Task ExecutePlayerFleeAsync(GameSessionState session)
		{
			LastMethod = nameof(ICombatService.ExecutePlayerFleeAsync);
			return Task.CompletedTask;
		}

		public Task ExecutePlayerTakeTreasureAsync(GameSessionState session, TargetPayload payload)
		{
			LastMethod = nameof(ICombatService.ExecutePlayerTakeTreasureAsync);
			LastTakePayload = payload;
			return Task.CompletedTask;
		}

		public Task ExecutePlayerUseHealthPotionAsync(GameSessionState session)
		{
			LastMethod = nameof(ICombatService.ExecutePlayerUseHealthPotionAsync);
			return Task.CompletedTask;
		}

		public Task ExecutePlayerDefendAsync(GameSessionState session)
		{
			LastMethod = nameof(ICombatService.ExecutePlayerDefendAsync);
			return Task.CompletedTask;
		}

		public Task ExecutePlayerDisarmTrapAsync(GameSessionState session, TargetPayload payload)
		{
			LastMethod = nameof(ICombatService.ExecutePlayerDisarmTrapAsync);
			LastDisarmPayload = payload;
			return Task.CompletedTask;
		}
	}

	private static CombatUiPresenter CreatePresenter(
		GameSessionState session,
		RecordingCombatService combat,
		Action<UiRefreshFlags> refresh)
	{
		return new CombatUiPresenter(session, combat, refresh);
	}

	[Fact]
	public async Task Guards_WhenNotInCombat_NoServiceCallsAndNoRefresh()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));

		await presenter.OnAttackWithTargetAsync(0);
		await presenter.OnFleePressedAsync();
		await presenter.OnPotionPressedAsync();

		Assert.Null(combat.LastMethod);
		Assert.Empty(refreshes);
	}

	[Fact]
	public async Task Guards_WhenCombatButNotAwaiting_NoExecuteAndNoRefresh()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService { AwaitingPlayerAction = false };
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));

		await presenter.OnPotionPressedAsync();

		Assert.Null(combat.LastMethod);
		Assert.Empty(refreshes);
	}

	[Fact]
	public async Task OnAttackWithTarget_WhenAwaiting_CallsCombatAndRefreshes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));

		await presenter.OnAttackWithTargetAsync(2, PlayerAttackChoice.Unarmed);

		Assert.Equal(nameof(ICombatService.ExecutePlayerAttackAsync), combat.LastMethod);
		Assert.Equal(2, combat.LastAttackOrdinal);
		Assert.True(combat.LastAttackChoice.IsUnarmed);
		Assert.Single(refreshes);
		Assert.Equal(ExpectedCombatSuccessFlags, refreshes[0]);
	}

	[Fact]
	public async Task OnFleePressed_WhenAwaiting_CallsCombatAndRefreshes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));

		await presenter.OnFleePressedAsync();

		Assert.Equal(nameof(ICombatService.ExecutePlayerFleeAsync), combat.LastMethod);
		Assert.Single(refreshes);
		Assert.Equal(ExpectedCombatSuccessFlags, refreshes[0]);
	}

	[Fact]
	public async Task OnPotionPressed_WhenAwaiting_CallsCombatAndRefreshes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));

		await presenter.OnPotionPressedAsync();

		Assert.Equal(nameof(ICombatService.ExecutePlayerUseHealthPotionAsync), combat.LastMethod);
		Assert.Single(refreshes);
		Assert.Equal(ExpectedCombatSuccessFlags, refreshes[0]);
	}

	[Fact]
	public async Task OnTakeWithTarget_WhenAwaiting_CallsCombatAndRefreshes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));
		var payload = new TargetPayload { Kind = TargetPayloadKind.TakeTreasureItem };

		await presenter.OnTakeWithTargetAsync(payload);

		Assert.Equal(nameof(ICombatService.ExecutePlayerTakeTreasureAsync), combat.LastMethod);
		Assert.Equal(TargetPayloadKind.TakeTreasureItem, combat.LastTakePayload!.Kind);
		Assert.Single(refreshes);
		Assert.Equal(ExpectedCombatSuccessFlags, refreshes[0]);
	}

	[Fact]
	public async Task OnDefendPressed_WhenAwaiting_CallsCombatAndRefreshes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));

		await presenter.OnDefendPressedAsync();

		Assert.Equal(nameof(ICombatService.ExecutePlayerDefendAsync), combat.LastMethod);
		Assert.Single(refreshes);
		Assert.Equal(ExpectedCombatSuccessFlags, refreshes[0]);
	}

	[Fact]
	public async Task OnDisarmWithTarget_WhenAwaiting_CallsCombatAndRefreshes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));
		var payload = new TargetPayload { Kind = TargetPayloadKind.DisarmTrapInstance };

		await presenter.OnDisarmWithTargetAsync(payload);

		Assert.Equal(nameof(ICombatService.ExecutePlayerDisarmTrapAsync), combat.LastMethod);
		Assert.Equal(TargetPayloadKind.DisarmTrapInstance, combat.LastDisarmPayload!.Kind);
		Assert.Single(refreshes);
		Assert.Equal(ExpectedCombatSuccessFlags, refreshes[0]);
	}

	[Fact]
	public async Task Action_RemainsBusyUntilAwaitedOperationCompletes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var combat = new RecordingCombatService { AttackOperation = () => completion.Task };
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, refreshes.Add);

		var attack = presenter.OnAttackWithTargetAsync(0);
		await presenter.OnFleePressedAsync();

		Assert.Equal(nameof(ICombatService.ExecutePlayerAttackAsync), combat.LastMethod);
		Assert.Empty(refreshes);

		completion.SetResult();
		await attack;

		Assert.Single(refreshes);
	}

	[Fact]
	public async Task Action_WhenServiceFaults_RefreshesAndReleasesBusyState()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService
		{
			AttackOperation = () => Task.FromException(new InvalidOperationException("test fault")),
		};
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, refreshes.Add);

		await Assert.ThrowsAsync<InvalidOperationException>(() => presenter.OnAttackWithTargetAsync(0));
		await presenter.OnFleePressedAsync();

		Assert.Equal(nameof(ICombatService.ExecutePlayerFleeAsync), combat.LastMethod);
		Assert.Equal(2, refreshes.Count);
	}
}
