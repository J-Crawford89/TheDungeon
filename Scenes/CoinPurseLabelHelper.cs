#nullable enable
using Godot;

/// <summary>Shows one line per denomination when count ≥ 1; hides otherwise.</summary>
public static class CoinPurseLabelHelper
{
	public static void ApplyDenominationLabels(
		Label? copper,
		Label? silver,
		Label? gold,
		Label? platinum,
		CoinPurse purse)
	{
		ApplyOne(copper, purse.Copper, "Copper");
		ApplyOne(silver, purse.Silver, "Silver");
		ApplyOne(gold, purse.Gold, "Gold");
		ApplyOne(platinum, purse.Platinum, "Platinum");
	}

	private static void ApplyOne(Label? label, int count, string name)
	{
		if (label == null)
			return;
		if (count < 1)
		{
			label.Visible = false;
			label.Text = "";
			return;
		}

		label.Visible = true;
		label.Text = $"{name}: {count}";
	}
}
