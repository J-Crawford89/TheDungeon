using System;
using System.Collections.Generic;
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

		public bool TryBeginCombatIfHostile(GameSessionState session, RoomCoord previousCoord, int floorLevel) =>
			false;

		public bool CanAcceptPlayerAction(GameSessionState session) => AwaitingPlayerAction;

		public bool CanExecuteCombatAbility(GameSessionState session, string abilityId) => false;

		public void ExecutePlayerAttack(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice)
		{
			LastMethod = nameof(ICombatService.ExecutePlayerAttack);
			LastAttackOrdinal = livingMonsterOrdinal;
			LastAttackChoice = attackChoice;
		}

		public void ExecutePlayerFlee(GameSessionState session) =>
			LastMethod = nameof(ICombatService.ExecutePlayerFlee);

		public void ExecutePlayerTakeTreasure(GameSessionState session, TargetPayload payload)
		{
			LastMethod = nameof(ICombatService.ExecutePlayerTakeTreasure);
			LastTakePayload = payload;
		}

		public void ExecutePlayerUseHealthPotion(GameSessionState session) =>
			LastMethod = nameof(ICombatService.ExecutePlayerUseHealthPotion);

		public void ExecutePlayerDefend(GameSessionState session) =>
			LastMethod = nameof(ICombatService.ExecutePlayerDefend);

		public void ExecutePlayerDisarmTrap(GameSessionState session, TargetPayload payload)
		{
			LastMethod = nameof(ICombatService.ExecutePlayerDisarmTrap);
			LastDisarmPayload = payload;
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
	public void Guards_WhenNotInCombat_NoServiceCallsAndNoRefresh()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));

		presenter.OnAttackWithTarget(0);
		presenter.OnFleePressed();
		presenter.OnPotionPressed();

		Assert.Null(combat.LastMethod);
		Assert.Empty(refreshes);
	}

	[Fact]
	public void Guards_WhenCombatButNotAwaiting_NoExecuteAndNoRefresh()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService { AwaitingPlayerAction = false };
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));

		presenter.OnPotionPressed();

		Assert.Null(combat.LastMethod);
		Assert.Empty(refreshes);
	}

	[Fact]
	public void OnAttackWithTarget_WhenAwaiting_CallsCombatAndRefreshes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));

		presenter.OnAttackWithTarget(2, PlayerAttackChoice.Unarmed);

		Assert.Equal(nameof(ICombatService.ExecutePlayerAttack), combat.LastMethod);
		Assert.Equal(2, combat.LastAttackOrdinal);
		Assert.True(combat.LastAttackChoice.IsUnarmed);
		Assert.Single(refreshes);
		Assert.Equal(ExpectedCombatSuccessFlags, refreshes[0]);
	}

	[Fact]
	public void OnFleePressed_WhenAwaiting_CallsCombatAndRefreshes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));

		presenter.OnFleePressed();

		Assert.Equal(nameof(ICombatService.ExecutePlayerFlee), combat.LastMethod);
		Assert.Single(refreshes);
		Assert.Equal(ExpectedCombatSuccessFlags, refreshes[0]);
	}

	[Fact]
	public void OnPotionPressed_WhenAwaiting_CallsCombatAndRefreshes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));

		presenter.OnPotionPressed();

		Assert.Equal(nameof(ICombatService.ExecutePlayerUseHealthPotion), combat.LastMethod);
		Assert.Single(refreshes);
		Assert.Equal(ExpectedCombatSuccessFlags, refreshes[0]);
	}

	[Fact]
	public void OnTakeWithTarget_WhenAwaiting_CallsCombatAndRefreshes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));
		var payload = new TargetPayload { Kind = TargetPayloadKind.TakeTreasureItem };

		presenter.OnTakeWithTarget(payload);

		Assert.Equal(nameof(ICombatService.ExecutePlayerTakeTreasure), combat.LastMethod);
		Assert.Equal(TargetPayloadKind.TakeTreasureItem, combat.LastTakePayload!.Kind);
		Assert.Single(refreshes);
		Assert.Equal(ExpectedCombatSuccessFlags, refreshes[0]);
	}

	[Fact]
	public void OnDefendPressed_WhenAwaiting_CallsCombatAndRefreshes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));

		presenter.OnDefendPressed();

		Assert.Equal(nameof(ICombatService.ExecutePlayerDefend), combat.LastMethod);
		Assert.Single(refreshes);
		Assert.Equal(ExpectedCombatSuccessFlags, refreshes[0]);
	}

	[Fact]
	public void OnDisarmWithTarget_WhenAwaiting_CallsCombatAndRefreshes()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var combat = new RecordingCombatService();
		var refreshes = new List<UiRefreshFlags>();
		var presenter = CreatePresenter(session, combat, f => refreshes.Add(f));
		var payload = new TargetPayload { Kind = TargetPayloadKind.DisarmTrapInstance };

		presenter.OnDisarmWithTarget(payload);

		Assert.Equal(nameof(ICombatService.ExecutePlayerDisarmTrap), combat.LastMethod);
		Assert.Equal(TargetPayloadKind.DisarmTrapInstance, combat.LastDisarmPayload!.Kind);
		Assert.Single(refreshes);
		Assert.Equal(ExpectedCombatSuccessFlags, refreshes[0]);
	}
}
