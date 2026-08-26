using System;
using Xunit;

public sealed class CombatAbilityEffectsRegistryTests
{
	private sealed class StubHandler : IPlayerCombatAbilityHandler
	{
		public required string AbilityId { get; init; }
		public bool IsVisibleResult { get; init; } = true;
		public bool CanExecuteResult { get; init; } = true;
		public int ExecuteCallCount { get; private set; }
		public int ReportCannotExecuteCallCount { get; private set; }
		public Action? CapturedAdvance { get; private set; }

		public bool IsVisible(GameSessionState session) => IsVisibleResult;

		public bool CanExecute(GameSessionState session) => CanExecuteResult;

		public void Execute(GameSessionState session, Action advanceTurn)
		{
			ExecuteCallCount++;
			CapturedAdvance = advanceTurn;
		}

		public void ReportCannotExecute(GameSessionState session) => ReportCannotExecuteCallCount++;
	}

	[Fact]
	public void IsVisible_UnknownAbilityId_ReturnsFalse()
	{
		var reg = new CombatAbilityEffectsRegistry();
		Assert.False(reg.IsVisible("nope", new GameSessionState()));
	}

	[Fact]
	public void IsVisible_DelegatesToRegisteredHandler()
	{
		var reg = new CombatAbilityEffectsRegistry();
		reg.Register(new StubHandler { AbilityId = "test.ability", IsVisibleResult = true });

		Assert.True(reg.IsVisible("test.ability", new GameSessionState()));
	}

	[Fact]
	public void CanExecute_HiddenAbility_ReturnsFalseWithoutConsultingExecutionResult()
	{
		var reg = new CombatAbilityEffectsRegistry();
		reg.Register(new StubHandler
		{
			AbilityId = "test.ability",
			IsVisibleResult = false,
			CanExecuteResult = true,
		});

		Assert.False(reg.CanExecute("test.ability", new GameSessionState()));
	}

	[Fact]
	public void CanExecute_UnknownAbilityId_ReturnsFalse()
	{
		var reg = new CombatAbilityEffectsRegistry();
		Assert.False(reg.CanExecute("nope", new GameSessionState()));
	}

	[Fact]
	public void CanExecute_DelegatesToHandler()
	{
		var reg = new CombatAbilityEffectsRegistry();
		reg.Register(new StubHandler { AbilityId = "test.ability", CanExecuteResult = true });
		Assert.True(reg.CanExecute("test.ability", new GameSessionState()));
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
		Assert.Equal(1, handler.ReportCannotExecuteCallCount);
		Assert.False(called);
	}

	[Fact]
	public void TryExecute_NotVisible_ReturnsFalseWithoutReportingUnavailable()
	{
		var reg = new CombatAbilityEffectsRegistry();
		var handler = new StubHandler
		{
			AbilityId = "test.ability",
			IsVisibleResult = false,
		};
		reg.Register(handler);

		var ok = reg.TryExecute("test.ability", new GameSessionState(), () => { });

		Assert.False(ok);
		Assert.Equal(0, handler.ExecuteCallCount);
		Assert.Equal(0, handler.ReportCannotExecuteCallCount);
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

	[Fact]
	public void Register_SecondHandlerForSameId_ReplacesAllPolicyAndExecutionRouting()
	{
		var reg = new CombatAbilityEffectsRegistry();
		var first = new StubHandler { AbilityId = "test.ability", IsVisibleResult = false };
		var replacement = new StubHandler { AbilityId = "test.ability" };
		reg.Register(first);
		reg.Register(replacement);

		Assert.True(reg.IsVisible("test.ability", new GameSessionState()));
		Assert.True(reg.TryExecute("test.ability", new GameSessionState(), () => { }));
		Assert.Equal(0, first.ExecuteCallCount);
		Assert.Equal(1, replacement.ExecuteCallCount);
	}
}
