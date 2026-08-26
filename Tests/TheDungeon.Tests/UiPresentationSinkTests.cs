using System.Threading;
using System.Threading.Tasks;
using Xunit;

public sealed class UiPresentationSinkTests
{
	[Fact]
	public async Task ResolvedRollReaction_RefreshesOnce()
	{
		var refreshCount = 0;
		var sink = new UiResolvedRollReactionSink(() => refreshCount++);

		await sink.NotifyAsync();

		Assert.Equal(1, refreshCount);
	}

	[Fact]
	public async Task ResolvedRollReaction_WhenCanceled_DoesNotRefresh()
	{
		var refreshCount = 0;
		var sink = new UiResolvedRollReactionSink(() => refreshCount++);
		var token = new CancellationToken(canceled: true);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sink.NotifyAsync(token));

		Assert.Equal(0, refreshCount);
	}

	[Fact]
	public async Task CombatTurnPresentation_ForwardsKindAndAwaitsCoordinator()
	{
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		CombatTurnPresentationKind? forwardedKind = null;
		var sink = new UiCombatTurnPresentationSink((kind, _) =>
		{
			forwardedKind = kind;
			return completion.Task;
		});

		var presentation = sink.PresentAsync(CombatTurnPresentationKind.ActiveTurnChanged);

		Assert.Equal(CombatTurnPresentationKind.ActiveTurnChanged, forwardedKind);
		Assert.False(presentation.IsCompleted);

		completion.SetResult();
		await presentation;
	}

	[Fact]
	public async Task CombatTurnPresentation_WhenCanceled_DoesNotForward()
	{
		var forwardCount = 0;
		var sink = new UiCombatTurnPresentationSink((_, _) =>
		{
			forwardCount++;
			return Task.CompletedTask;
		});
		var token = new CancellationToken(canceled: true);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			sink.PresentAsync(CombatTurnPresentationKind.OrderRevealed, token));

		Assert.Equal(0, forwardCount);
	}
}
