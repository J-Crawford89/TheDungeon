public sealed class DiceExpression
{
	public int NumberOfDice { get; set; }
	public DieType DieType { get; set; }

	/// <summary>
	/// When <see cref="DiceRollRequest.CheckStyle"/> is not <see cref="D20CheckStyle.None"/>,
	/// each d20 from this expression joins the check pool for aggregation and crit resolution.
	/// Non-d20 dice in this expression still roll and sum into the total normally.
	/// </summary>
	public bool InD20CheckPool { get; set; }
}