using Xunit;

public sealed class TargetHighlightMappingTests
{
	private static TargetPayload DummyPayload(TargetPayloadKind kind = TargetPayloadKind.AttackLivingMonsterOrdinal) =>
		new() { Kind = kind };

	[Fact]
	public void TryGetDescriptorIndex_ReturnsIndex_WhenHighlightKeyMatches()
	{
		TargetDescriptor[] list =
		[
			new TargetDescriptor { Label = "A", HighlightKey = "monster:0", Payload = DummyPayload() },
			new TargetDescriptor { Label = "B", HighlightKey = "monster:1", Payload = DummyPayload() },
		];

		Assert.Equal(1, TargetHighlightMapping.TryGetDescriptorIndex(list, "monster:1"));
	}

	[Fact]
	public void TryGetDescriptorIndex_ReturnsAggregateIndex_WhenKeyInTakeAllHighlightKeys()
	{
		TargetDescriptor[] list =
		[
			new TargetDescriptor
			{
				Label = "Take all",
				HighlightKey = null,
				TakeAllHighlightKeys = ["treasure:0:0", "treasure:1:0"],
				Payload = new TargetPayload { Kind = TargetPayloadKind.TakeAllEligibleTreasure },
			},
			new TargetDescriptor { Label = "Other", HighlightKey = "trap:0:0", Payload = DummyPayload(TargetPayloadKind.DisarmTrapInstance) },
		];

		Assert.Equal(0, TargetHighlightMapping.TryGetDescriptorIndex(list, "treasure:1:0"));
	}

	[Fact]
	public void TryGetDescriptorIndex_ReturnsNull_WhenNoMatch()
	{
		TargetDescriptor[] list =
		[
			new TargetDescriptor { Label = "A", HighlightKey = "monster:0", Payload = DummyPayload() },
		];

		Assert.Null(TargetHighlightMapping.TryGetDescriptorIndex(list, "monster:9"));
		Assert.Null(TargetHighlightMapping.TryGetDescriptorIndex(list, ""));
	}

	[Fact]
	public void TryGetDescriptorIndex_FirstDescriptorWins_WhenSameHighlightKeyListedTwice()
	{
		TargetDescriptor[] list =
		[
			new TargetDescriptor { Label = "First", HighlightKey = "monster:0", Payload = DummyPayload() },
			new TargetDescriptor { Label = "Second", HighlightKey = "monster:0", Payload = DummyPayload() },
		];

		Assert.Equal(0, TargetHighlightMapping.TryGetDescriptorIndex(list, "monster:0"));
	}
}
