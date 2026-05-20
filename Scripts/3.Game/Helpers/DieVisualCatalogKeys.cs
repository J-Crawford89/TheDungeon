using System.Collections.Generic;

/// <summary>Catalog lookup keys for physical dice; supports d100 entries stored under <see cref="DieType.d100"/>.</summary>
public static class DieVisualCatalogKeys
{
	public static IEnumerable<(DieType DieType, DieVisualRole Role)> GetResolveKeys(DieType dieType, DieVisualRole role)
	{
		yield return (dieType, role);

		if (dieType == DieType.d10 && role == DieVisualRole.PercentileTens)
			yield return (DieType.d100, DieVisualRole.PercentileTens);
	}
}
