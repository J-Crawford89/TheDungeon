using System;

public static class PlayerDamageReductionHelper
{
	public static int TotalDamageReductionFor(PlayerState player, DamageTypeDefinition? incomingDamageType)
	{
		ArgumentNullException.ThrowIfNull(player);

		var total = player.DamageReductionAllDamage;
		if (incomingDamageType == null)
			return total;

		var id = incomingDamageType.Id?.Trim() ?? "";
		if (id.Length > 0 && player.DamageReductionByDamageTypeId.TryGetValue(id, out var byType))
			total += byType;

		if (player.DamageReductionByDamageFamily.TryGetValue(incomingDamageType.Family, out var byFamily))
			total += byFamily;

		return total;
	}
}
