public sealed partial class NarrativeService
{
	public string ForExperienceChanged(int amount, int total)
	{
		if (amount > 0)
			return $"You gain {amount} experience. Total XP: {total}.";

		return $"You lose {System.Math.Abs(amount)} experience. Total XP: {total}.";
	}
}
