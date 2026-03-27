using System.Collections.Generic;

public sealed class DiceRollResult
{
	public string SummaryText { get; init; }
	public string DetailText { get; set; }
	public int RollTotal { get; set; }
	public int ModifierTotal { get; set; }
	public int Total { get; set; }
	public IReadOnlyList<DiceExpressionResult> ExpressionResults { get; set; }
	public IReadOnlyList<ModifierWithSource> ModifiersWithSources { get; set; }

	/// <summary>
	/// After advantage/disadvantage or a single check die, the d20 face used for natural 1 / 20.
	/// Null when <see cref="DiceRollRequest.CheckStyle"/> is <see cref="D20CheckStyle.None"/> or the pool is invalid for the style.
	/// </summary>
	public int? ResolvedD20CheckValue { get; set; }
}