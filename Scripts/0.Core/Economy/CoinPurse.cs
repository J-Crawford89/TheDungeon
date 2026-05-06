#nullable enable

/// <summary>Physical coin stacks on the player or in a reward grant; denominations do not auto-consolidate.</summary>
public sealed class CoinPurse
{
	public int Copper { get; set; }
	public int Silver { get; set; }
	public int Gold { get; set; }
	public int Platinum { get; set; }

	public CoinPurse Clone() =>
		new()
		{
			Copper = Copper,
			Silver = Silver,
			Gold = Gold,
			Platinum = Platinum,
		};
}
