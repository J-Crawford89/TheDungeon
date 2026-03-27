public static class InitiativeHelper
{
	public static int Compare(InitiativeEntry a, InitiativeEntry b)
	{
		var c = b.Total.CompareTo(a.Total);
		if (c != 0)
			return c;
		c = b.Agility.CompareTo(a.Agility);
		if (c != 0)
			return c;
		if (a.IsPlayer != b.IsPlayer)
			return a.IsPlayer ? -1 : 1;
		return a.MonsterIndex.CompareTo(b.MonsterIndex);
	}
}
