/// <summary>Executes a <see cref="DiceRollRequest"/> (used for test doubles and resolution flows).</summary>
public interface IDiceRollRequestExecutor
{
	DiceRollResult Roll(DiceRollRequest request);

	/// <summary>Rolls a single die (e.g. weapon damage dice, unarmed d6).</summary>
	DieRollResult RollDie(DieType dieType);
}
