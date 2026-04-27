using System;
using Xunit;

public sealed class CombatAbilityEffectsRegistryTests
{
	private sealed class StubHandler : IPlayerCombatAbilityHandler
	{
		public required string AbilityId { get; init; }
		public bool CanExecuteResult { get; init; } = true;
		public int ExecuteCallCount { get; private set; }
		public Action? CapturedAdvance { get; private set; }

		public bool CanExecute(GameSessionState session) => CanExecuteResult;

		public void Execute(GameSessionState session, Action advanceTurnAndProcessMonsterPhases)
		{
			ExecuteCallCount++;
			CapturedAdvance = advanceTurnAndProcessMonsterPhases;
		}
	}

	[Fact]
	public void TryExecute_UnknownAbilityId_ReturnsFalse()
	{
		var reg = new CombatAbilityEffectsRegistry();
		var session = new GameSessionState();
		var called = false;
		void Advance() => called = true;

		var ok = reg.TryExecute("nope", session, Advance);

		Assert.False(ok);
		Assert.False(called);
	}

	[Fact]
	public void TryExecute_CanExecuteFalse_ReturnsFalseAndDoesNotRunExecute()
	{
		var reg = new CombatAbilityEffectsRegistry();
		var handler = new StubHandler
		{
			AbilityId = "test.ability",
			CanExecuteResult = false,
		};
		reg.Register(handler);
		var session = new GameSessionState();
		var called = false;
		void Advance() => called = true;

		var ok = reg.TryExecute("test.ability", session, Advance);

		Assert.False(ok);
		Assert.Equal(0, handler.ExecuteCallCount);
		Assert.False(called);
	}

	[Fact]
	public void TryExecute_CanExecuteTrue_InvokesExecuteWithAdvanceAndReturnsTrue()
	{
		var reg = new CombatAbilityEffectsRegistry();
		var handler = new StubHandler { AbilityId = "test.ability" };
		reg.Register(handler);
		var session = new GameSessionState();
		var advanceFromRegistry = false;
		void Advance() => advanceFromRegistry = true;

		var ok = reg.TryExecute("test.ability", session, Advance);

		Assert.True(ok);
		Assert.Equal(1, handler.ExecuteCallCount);
		Assert.NotNull(handler.CapturedAdvance);
		Assert.False(advanceFromRegistry);
		handler.CapturedAdvance!();
		Assert.True(advanceFromRegistry);
	}
}
