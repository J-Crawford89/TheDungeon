using System.Collections.Generic;

public sealed class DiceExpressionResult
{
    public DiceExpression Expression { get; set; }
    public IReadOnlyList<DieRollResult> Rolls { get; set; }
}