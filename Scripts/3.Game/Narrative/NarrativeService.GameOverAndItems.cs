public sealed partial class NarrativeService
{
	public string ForGameOverTitle() => "Game Over";

	public string ForGameOverBody(PlayerDamageSource source)
	{
		return source.Type switch
		{
			DamageSourceType.Monster => $"You were slain by {source.DisplayName}. Over the coming days, it continues to feast on your corpse.",
			DamageSourceType.Trap => $"You succumb to {source.DisplayName}. Your belongings lie scattered in the dark.",
			DamageSourceType.Environmental => $"You are undone by {source.DisplayName}. Nothing remains but silence.",
			_ => "Your adventure ends here.",
		};
	}

	public string ForTakeNothingHere() => "There is nothing here to take.";

	public string ForNothingYouCanAttack() => "There is nothing you can attack.";

	public string ForStrikeUnarmedInstead() =>
		"You have nothing to attack with in that hand; you strike unarmed instead.";

	public string ForWeaponHasNoAttacksConfigured() =>
		"That weapon has no attacks configured; you cannot strike with it.";

	public string ForNothingHereToOpen() => "There is nothing here to open.";

	public string ForNothingHereToDisarm() => "There is nothing here to disarm.";

	public string ForTookCurrency(CoinPurse grant, string treasureName, CoinPurse purseAfter) =>
		$"You take {treasureName} ({CurrencyFormatter.DescribeSentenceGrant(grant)}). You now carry {CurrencyFormatter.DescribeSentenceTotal(purseAfter)}.";

	public string ForTookItem(string itemName) => $"You take {itemName}.";

	public string ForUsedHealthPotion(int healed, int hpAfter) =>
		$"You drink a health potion and recover {healed} HP. You now have {hpAfter} HP.";

	public string ForHealthPotionAtFullHealth() =>
		"You are already at full health; you save the potion for later.";

	public string ForHealthPotionNoneLeft() => "You have no health potions to use.";

	public string ForDefendStance() => "You take a defensive stance, ready to block the next solid blow.";

	public string ForDefendAbsorbedHit() => "Your guard absorbs the hit — you take no damage from that strike.";

	public string ForDefendAlreadyDefending() => "You are already defending; you hold your position.";

	public string ForDefendOnCooldown() => "You are still recovering your footing and cannot defend yet.";

	public string ForDefendCannotUse() => "You cannot use Defend right now.";
}
