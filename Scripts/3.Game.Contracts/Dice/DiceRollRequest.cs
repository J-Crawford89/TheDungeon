using System.Collections.Generic;

public sealed class DiceRollRequest
{
	public string DiceRollLabel { get; set; }
	public int TargetNumber { get; set; }
	public List<DiceExpression> DiceExpressions { get; set; } = new();
	public List<ModifierWithSource> ModifiersWithSources { get; set; } = new();

	/// <summary>
	/// How pooled d20s (see <see cref="DiceExpression.InD20CheckPool"/>) affect total and crits.
	/// Default <see cref="D20CheckStyle.None"/> keeps previous behavior: sum all dice, no auto crit.
	/// </summary>
	public D20CheckStyle CheckStyle { get; set; } = D20CheckStyle.None;
}