#nullable enable
using System.Collections.Generic;

public sealed class PlayerAttackDamageRollResult
{
	public int Total { get; init; }
	public string Detail { get; init; } = string.Empty;
	public IReadOnlyList<PhysicalDieRollSpec> VisualDice { get; init; } = [];
}
