using Xunit;

namespace TheDungeon.Tests;

public sealed class DieRollSettleTrackerTests
{
	[Fact]
	public void Observe_RequiresContinuousTimeBelowBothThresholds()
	{
		var tracker = new DieRollSettleTracker(0.35f, 0.85f, 0.2f);

		Assert.False(tracker.Observe(0.2f, 0.5f, 0.1f));
		Assert.True(tracker.Observe(0.2f, 0.5f, 0.1f));
	}

	[Fact]
	public void Observe_MovementResetsConfirmationWindow()
	{
		var tracker = new DieRollSettleTracker(0.35f, 0.85f, 0.2f);

		Assert.False(tracker.Observe(0.2f, 0.5f, 0.1f));
		Assert.False(tracker.Observe(0.4f, 0.5f, 0.05f));
		Assert.False(tracker.Observe(0.2f, 0.5f, 0.1f));
		Assert.True(tracker.Observe(0.2f, 0.5f, 0.1f));
	}

	[Fact]
	public void Observe_RequiresSupportContactAndResetsWhenAirborne()
	{
		var tracker = new DieRollSettleTracker(0.35f, 0.85f, 0.2f);

		Assert.False(tracker.Observe(0.1f, 0.1f, 0.1f, hasSupportContact: true));
		Assert.False(tracker.Observe(0.1f, 0.1f, 0.1f, hasSupportContact: false));
		Assert.False(tracker.Observe(0.1f, 0.1f, 0.1f, hasSupportContact: true));
		Assert.True(tracker.Observe(0.1f, 0.1f, 0.1f, hasSupportContact: true));
	}
}
