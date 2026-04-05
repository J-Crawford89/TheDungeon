using System;

public sealed class PlayerVitalsService
{
	public VitalsDamageResult ApplyDamage(PlayerState player, int damage)
	{
		var hpBefore = player.CurrentHp;
		var hypothetical = hpBefore - damage;
		var clamped = Math.Max(0, hypothetical);
		player.CurrentHp = clamped;
		var overkillMagnitude = hypothetical < 0 ? -hypothetical : 0;
		return new VitalsDamageResult
		{
			HpBefore = hpBefore,
			DamageRequested = damage,
			HpAfterClamped = clamped,
			HypotheticalHpAfter = hypothetical,
			OverkillMagnitude = overkillMagnitude,
		};
	}
}
