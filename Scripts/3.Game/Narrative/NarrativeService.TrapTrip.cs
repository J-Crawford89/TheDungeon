public sealed partial class NarrativeService
{
	public string ForTrapTripLeadIn(TrapTripCause cause, string trapName, bool wasHiddenBeforeTrip)
	{
		switch (cause)
		{
			case TrapTripCause.DisarmFailed:
				return $"You set off the {trapName} while failing to disarm it.";
			case TrapTripCause.LeftRoom when wasHiddenBeforeTrip:
				return $"Leaving the room, you stumble into the {trapName}—hidden until now.";
			case TrapTripCause.LeftRoom:
				return $"As you leave, you trigger the {trapName}.";
			default:
				return $"The {trapName} goes off.";
		}
	}

	public string ForTrapTripOutcomeDamage(string trapName, int damage, int hpAfter) =>
		$"You take {damage} damage from the {trapName} (HP {hpAfter}).";

	public string ForTrapTripOutcomeNoDamage(string trapName) =>
		$"You weather the {trapName} without injury.";

	public string ForTrapTripEffectLine(string trapName, string effectText) =>
		$"{trapName}: {effectText}";
}
