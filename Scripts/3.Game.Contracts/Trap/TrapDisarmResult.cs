#nullable enable
using System;
using System.Collections.Generic;

/// <summary>Outcome from attempting to disarm a trap in the current room.</summary>
public sealed class TrapDisarmResult
{
	public TrapDisarmResultCode ResultCode { get; init; }

	public ResolutionResult? ResolvedCheck { get; init; }

	public bool TrapFeatureRemoved { get; init; }

	public int DamageDealtToPlayer { get; init; }

	/// <summary>Item definition ids granted on success (e.g. rope from snare).</summary>
	public IReadOnlyList<string> GrantedInventoryItemDefinitionIds { get; init; } =
		Array.Empty<string>();

	/// <summary>In combat, advance the turn after a rolled check even on failure.</summary>
	public bool ShouldAdvanceCombatTurn =>
		ResultCode == TrapDisarmResultCode.DisarmCheckResolved;
}
