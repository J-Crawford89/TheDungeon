/// <summary>
/// How d20 dice marked <see cref="DiceExpression.InD20CheckPool"/> combine for the check total and for natural 1 / 20.
/// Use <see cref="None"/> for rolls where d20s should not drive crits (e.g. damage) even if a d20 is present.
/// </summary>
public enum D20CheckStyle
{
	/// <summary>Sum every die; pool flags ignored; no natural 1/20 crit semantics.</summary>
	None,

	/// <summary>Pool must contain exactly one d20; that value is used for crits.</summary>
	Standard,

	/// <summary>Pool must contain at least two d20s; highest is used for the check slice and for crits.</summary>
	Advantage,

	/// <summary>Pool must contain at least two d20s; lowest is used for the check slice and for crits.</summary>
	Disadvantage
}
