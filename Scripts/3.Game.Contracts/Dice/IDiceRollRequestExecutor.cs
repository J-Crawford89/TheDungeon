/// <summary>Executes a <see cref="DiceRollRequest"/> (used for test doubles and resolution flows).</summary>
public interface IDiceRollRequestExecutor
{
	DiceRollResult Roll(DiceRollRequest request);
}
