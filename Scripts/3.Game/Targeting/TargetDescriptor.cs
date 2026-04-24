#nullable enable
using System.Collections.Generic;

public sealed class TargetDescriptor
{
	public required string Label { get; init; }
	public required TargetPayload Payload { get; init; }

	/// <summary>Single-slot hover key; null for <see cref="TargetPayloadKind.TakeAllEligibleTreasure"/>.</summary>
	public string? HighlightKey { get; init; }

	/// <summary>For Take All: every treasure slot key collected by the aggregate action (main-view multi-highlight).</summary>
	public IReadOnlyList<string> TakeAllHighlightKeys { get; init; } = System.Array.Empty<string>();
}
