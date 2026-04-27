using System.Collections.Generic;
using Xunit;

public sealed class PlayerDownedResolutionServiceTests
{
	private sealed class Handler : IPlayerDownedOutcomeHandler
	{
		private readonly bool _result;
		private readonly List<string> _calls;
		private readonly string _id;

		public Handler(string id, int priority, bool result, List<string> calls)
		{
			_id = id;
			Priority = priority;
			_result = result;
			_calls = calls;
		}

		public int Priority { get; }

		public bool TryResolve(GameSessionState session, PlayerDownedContext context)
		{
			_calls.Add(_id);
			return _result;
		}
	}

	private static PlayerDownedContext Context() =>
		new()
		{
			Vitals = new VitalsDamageResult(),
			DamageSource = new PlayerDamageSource { Type = DamageSourceType.Trap, DisplayName = "Trap" }
		};

	[Fact]
	public void Resolve_OrdersByPriority_AndStopsOnFirstTrue()
	{
		var calls = new List<string>();
		var svc = new PlayerDownedResolutionService(
		[
			new Handler("high", 100, false, calls),
			new Handler("low", 1, true, calls),
			new Handler("mid", 50, false, calls),
		]);

		svc.Resolve(new GameSessionState(), Context());

		Assert.Equal(["low"], calls);
	}

	[Fact]
	public void Resolve_WhenAllFalse_CallsAll()
	{
		var calls = new List<string>();
		var svc = new PlayerDownedResolutionService(
		[
			new Handler("a", 1, false, calls),
			new Handler("b", 2, false, calls),
		]);

		svc.Resolve(new GameSessionState(), Context());

		Assert.Equal(["a", "b"], calls);
	}
}
