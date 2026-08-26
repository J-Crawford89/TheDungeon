using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public sealed class CombatUiPresenterTests
{
	private sealed class RecordingCombatService : ICombatService
	{
		public bool AwaitingPlayerAction { get; set; } = true;
		public string? LastMethod { get; private set; }
		public int? LastAttackOrdinal { get; private set; }
		public PlayerAttackChoice LastAttackChoice { get; private set; }
		public TargetPayload? LastTakePayload { get; private set; }
		public TargetPayload? LastDisarmPayload { get; private set; }
		public string? LastCombatAbilityId { get; private set; }
		public Func<Task>? AttackOperation { get; set; }
		public Func<Task>? CombatAbilityOperation { get; set; }

		public Task<bool> TryBeginCombatIfHostileAsync(GameSessionState session, RoomCoord previousCoord, int floorLevel) =>
			Task.FromResult(false);

		public bool CanAcceptPlayerAction(GameSessionState session) => AwaitingPlayerAction;

		public bool IsCombatAbilityVisible(GameSessionState session, string abilityId) => false;

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

		public Task ExecutePlayerCombatAbilityAsync(GameSessionState session, string abilityId)
		{
			LastMethod = nameof(ICombatService.ExecutePlayerCombatAbilityAsync);
			LastCombatAbilityId = abilityId;
			return CombatAbilityOperation?.Invoke() ?? Task.CompletedTask;
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
		Action stateChanged)
	{
		var presenter = new CombatUiPresenter(session, combat);
		presenter.StateChanged += stateChanged;
		return presenter;
	}

	[Fact]
	public async Task Guards_WhenNotInCombat_NoServiceCallsAndNoStateChanged()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
		var combat = new RecordingCombatService();
		var stateChanges = 0;
		var presenter = CreatePresenter(session, combat, () => stateChanges++);

		await presenter.OnAttackWithTargetAsync(0);
		await presenter.OnFleePressedAsync();
		await presenter.OnPotionPressedAsync();

		Assert.Null(combat.LastMethod);
		Assert.Equal(0, stateChanges);
	}

	[Fact]
	public async Task Guards_WhenCombatButNotAwaiting_NoExecuteAndNoStateChanged()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService { AwaitingPlayerAction = false };
		var stateChanges = 0;
		var presenter = CreatePresenter(session, combat, () => stateChanges++);

		await presenter.OnPotionPressedAsync();

		Assert.Null(combat.LastMethod);
		Assert.Equal(0, stateChanges);
	}

	[Fact]
	public async Task OnAttackWithTarget_WhenAwaiting_CallsCombatAndRaisesStateChanged()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var stateChanges = 0;
		var presenter = CreatePresenter(session, combat, () => stateChanges++);

		await presenter.OnAttackWithTargetAsync(2, PlayerAttackChoice.Unarmed);

		Assert.Equal(nameof(ICombatService.ExecutePlayerAttackAsync), combat.LastMethod);
		Assert.Equal(2, combat.LastAttackOrdinal);
		Assert.True(combat.LastAttackChoice.IsUnarmed);
		Assert.Equal(1, stateChanges);
	}

	[Fact]
	public async Task OnFleePressed_WhenAwaiting_CallsCombatAndRaisesStateChanged()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var stateChanges = 0;
		var presenter = CreatePresenter(session, combat, () => stateChanges++);

		await presenter.OnFleePressedAsync();

		Assert.Equal(nameof(ICombatService.ExecutePlayerFleeAsync), combat.LastMethod);
		Assert.Equal(1, stateChanges);
	}

	[Fact]
	public async Task OnPotionPressed_WhenAwaiting_CallsCombatAndRaisesStateChanged()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var stateChanges = 0;
		var presenter = CreatePresenter(session, combat, () => stateChanges++);

		await presenter.OnPotionPressedAsync();

		Assert.Equal(nameof(ICombatService.ExecutePlayerUseHealthPotionAsync), combat.LastMethod);
		Assert.Equal(1, stateChanges);
	}

	[Fact]
	public async Task OnTakeWithTarget_WhenAwaiting_CallsCombatAndRaisesStateChanged()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var stateChanges = 0;
		var presenter = CreatePresenter(session, combat, () => stateChanges++);
		var payload = new TargetPayload { Kind = TargetPayloadKind.TakeTreasureItem };

		await presenter.OnTakeWithTargetAsync(payload);

		Assert.Equal(nameof(ICombatService.ExecutePlayerTakeTreasureAsync), combat.LastMethod);
		Assert.Equal(TargetPayloadKind.TakeTreasureItem, combat.LastTakePayload!.Kind);
		Assert.Equal(1, stateChanges);
	}

	[Fact]
	public async Task OnCombatAbilityPressed_WhenAwaiting_ForwardsAbilityIdAndRaisesStateChanged()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var stateChanges = 0;
		var presenter = CreatePresenter(session, combat, () => stateChanges++);

		await presenter.OnCombatAbilityPressedAsync(AbilityIds.Defend);

		Assert.Equal(nameof(ICombatService.ExecutePlayerCombatAbilityAsync), combat.LastMethod);
		Assert.Equal(AbilityIds.Defend, combat.LastCombatAbilityId);
		Assert.Equal(1, stateChanges);
	}

	[Fact]
	public async Task OnCombatAbilityPressed_WhenServiceIsCanceled_NotifiesAndReleasesBusyState()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService
		{
			CombatAbilityOperation = () => Task.FromCanceled(new CancellationToken(canceled: true)),
		};
		var stateChanges = 0;
		var presenter = CreatePresenter(session, combat, () => stateChanges++);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			presenter.OnCombatAbilityPressedAsync(AbilityIds.Defend));
		combat.CombatAbilityOperation = null;
		await presenter.OnCombatAbilityPressedAsync("test.second-ability");

		Assert.Equal("test.second-ability", combat.LastCombatAbilityId);
		Assert.Equal(2, stateChanges);
	}

	[Fact]
	public async Task OnDisarmWithTarget_WhenAwaiting_CallsCombatAndRaisesStateChanged()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var stateChanges = 0;
		var presenter = CreatePresenter(session, combat, () => stateChanges++);
		var payload = new TargetPayload { Kind = TargetPayloadKind.DisarmTrapInstance };

		await presenter.OnDisarmWithTargetAsync(payload);

		Assert.Equal(nameof(ICombatService.ExecutePlayerDisarmTrapAsync), combat.LastMethod);
		Assert.Equal(TargetPayloadKind.DisarmTrapInstance, combat.LastDisarmPayload!.Kind);
		Assert.Equal(1, stateChanges);
	}

	[Fact]
	public async Task Action_RemainsBusyUntilAwaitedOperationCompletes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var combat = new RecordingCombatService { AttackOperation = () => completion.Task };
		var stateChanges = 0;
		var presenter = CreatePresenter(session, combat, () => stateChanges++);

		var attack = presenter.OnAttackWithTargetAsync(0);
		await presenter.OnFleePressedAsync();

		Assert.Equal(nameof(ICombatService.ExecutePlayerAttackAsync), combat.LastMethod);
		Assert.Equal(0, stateChanges);

		completion.SetResult();
		await attack;

		Assert.Equal(1, stateChanges);
	}

	[Fact]
	public async Task Action_WhenServiceFaults_RaisesStateChangedAndReleasesBusyState()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService
		{
			AttackOperation = () => Task.FromException(new InvalidOperationException("test fault")),
		};
		var stateChanges = 0;
		var presenter = CreatePresenter(session, combat, () => stateChanges++);

		await Assert.ThrowsAsync<InvalidOperationException>(() => presenter.OnAttackWithTargetAsync(0));
		await presenter.OnFleePressedAsync();

		Assert.Equal(nameof(ICombatService.ExecutePlayerFleeAsync), combat.LastMethod);
		Assert.Equal(2, stateChanges);
	}

	[Fact]
	public async Task Action_WhenServiceIsCanceled_NotifiesAndReleasesBusyState()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService
		{
			AttackOperation = () => Task.FromCanceled(new CancellationToken(canceled: true)),
		};
		var stateChanges = 0;
		var presenter = CreatePresenter(session, combat, () => stateChanges++);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => presenter.OnAttackWithTargetAsync(0));
		await presenter.OnFleePressedAsync();

		Assert.Equal(nameof(ICombatService.ExecutePlayerFleeAsync), combat.LastMethod);
		Assert.Equal(2, stateChanges);
	}
}
