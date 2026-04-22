using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public sealed class DiceRollService : IDiceRollRequestExecutor
{
    private readonly Random _random;

    public DiceRollService(Random random)
    {
        _random = random;
    }

    public DiceRollResult Roll(DiceRollRequest request)
    {
        List<DiceExpressionResult> expressionResults = [];

        foreach (var expression in request.DiceExpressions)
        {
            List<DieRollResult> dieRollResults = [];
            for (var i = 0; i < expression.NumberOfDice; i++)
            {
                dieRollResults.Add(Roll(expression.DieType));
            }
            expressionResults.Add(new() { Expression = expression, Rolls = dieRollResults });
        }

        var (rollTotal, resolvedD20) = ComputeRollTotalAndResolvedD20(request.CheckStyle, expressionResults);
        int modifierTotal = request.ModifiersWithSources.Sum(m => m.Modifier);
        int total = rollTotal + modifierTotal;

        return new DiceRollResult()
        {
            SummaryText = $"{request.DiceRollLabel}: {total}",
            DetailText = BuildDetailText(expressionResults, request.ModifiersWithSources, total),
            RollTotal = rollTotal,
            ModifierTotal = modifierTotal,
            Total = total,
            ExpressionResults = expressionResults,
            ModifiersWithSources = request.ModifiersWithSources,
            ResolvedD20CheckValue = resolvedD20
        };
    }

    public DieRollResult Roll(DieType dieType)
    {
        var sides = (int)dieType;
        return new DieRollResult()
        {
            DieType = dieType,
            RolledValue = _random.Next(1, sides + 1)
        };
    }

    public DiceRollResult RollD20Plus(string label, int modifier, string modSource)
    {
        var req = new DiceRollRequest
        {
            DiceRollLabel = label,
            TargetNumber = 0,
            CheckStyle = D20CheckStyle.None,
            DiceExpressions = new List<DiceExpression>
            {
                new() { NumberOfDice = 1, DieType = DieType.d20, InD20CheckPool = false }
            },
            ModifiersWithSources = new List<ModifierWithSource>
            {
                new() { Modifier = modifier, Source = modSource }
            }
        };
        return Roll(req);
    }

    private string BuildDetailText(List<DiceExpressionResult> results, List<ModifierWithSource> modifiersWithSources, int total)
    {
        var sb = new StringBuilder();
        var firstPart = true;

        foreach (var result in results)
        {
            if (!firstPart)
                sb.Append(" + ");

            sb.Append($"{result.Expression.NumberOfDice}{result.Expression.DieType.ToString()} ({string.Join(", ", result.Rolls.Select(r => r.RolledValue))})");

            firstPart = false;
        }

        foreach (var modifier in modifiersWithSources)
        {
            if (!firstPart)
                sb.Append(modifier.Modifier >= 0 ? " + " : " - ");

            sb.Append($"{modifier.Source} ({modifier.Modifier.ToString()})");

            firstPart = false;
        }

        sb.Append($" = {total}");

        return sb.ToString();
    }

    private (int rollTotal, int? resolvedD20CheckValue) ComputeRollTotalAndResolvedD20(
    D20CheckStyle style,
    List<DiceExpressionResult> expressionResults)
    {
        if (style == D20CheckStyle.None)
        {
            var sumAll = expressionResults.Sum(r => r.Rolls.Sum(d => d.RolledValue));
            return (sumAll, null);
        }

        var pool = new List<int>();
        var otherSum = 0;
        foreach (var er in expressionResults)
        {
            var inPool = er.Expression.InD20CheckPool;
            foreach (var d in er.Rolls)
            {
                if (inPool && d.DieType == DieType.d20)
                    pool.Add(d.RolledValue);
                else
                    otherSum += d.RolledValue;
            }
        }

        int poolPart;
        int? resolved = null;
        switch (style)
        {
            case D20CheckStyle.Standard:
                if (pool.Count == 1)
                {
                    resolved = pool[0];
                    poolPart = pool[0];
                }
                else
                    poolPart = Sum(pool);
                break;
            case D20CheckStyle.Advantage:
                if (pool.Count >= 2)
                {
                    poolPart = pool.Max();
                    resolved = poolPart;
                }
                else
                    poolPart = Sum(pool);
                break;
            case D20CheckStyle.Disadvantage:
                if (pool.Count >= 2)
                {
                    poolPart = pool.Min();
                    resolved = poolPart;
                }
                else
                    poolPart = Sum(pool);
                break;
            default:
                poolPart = Sum(pool);
                break;
        }

        return (poolPart + otherSum, resolved);
    }

    private int Sum(List<int> values)
    {
        var s = 0;
        foreach (var v in values)
            s += v;
        return s;
    }
}