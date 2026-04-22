public sealed partial class NarrativeService
{
	public string ForTrapDisarmRoll(string trapName, int totalRoll, int disarmDc, string detailText) =>
		$"Disarm {trapName}: total {totalRoll} vs DC {disarmDc}. {detailText}";

	public string ForTrapDisarmSuccess(string trapName) =>
		$"You successfully disarm the {trapName}.";

	public string ForTrapDisarmFailSafe(string trapName) =>
		$"You fail to disarm the {trapName}. It remains.";

	public string ForTrapDisarmFailDamage(string trapName, int damage, int hpAfter) =>
		$"You fail to disarm the {trapName}. You take {damage} damage (HP {hpAfter}).";

	public string ForTrapDisarmGrantedItem(string itemName) =>
		$"You salvage: {itemName}.";

	public string ForTrapDisarmGrantItemMissing(string itemId) =>
		$"[Loot] Item definition '{itemId}' not found.";
}
