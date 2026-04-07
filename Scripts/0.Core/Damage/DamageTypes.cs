public static class DamageTypes
{
    public static readonly DamageTypeDefinition Bludgeoning = new("bludgeoning", "Bludgeoning", DamageFamily.Physical);
    public static readonly DamageTypeDefinition Piercing = new("piercing", "Piercing", DamageFamily.Physical);
    public static readonly DamageTypeDefinition Slashing = new("slashing", "Slashing", DamageFamily.Physical);

    public static readonly DamageTypeDefinition Acid = new("acid", "Acid", DamageFamily.Elemental);
    public static readonly DamageTypeDefinition Air = new("air", "Air", DamageFamily.Elemental);
    public static readonly DamageTypeDefinition Electric = new("electric", "Electric", DamageFamily.Elemental);
    public static readonly DamageTypeDefinition Fire = new("fire", "Fire", DamageFamily.Elemental);
    public static readonly DamageTypeDefinition Frost = new("frost", "Frost", DamageFamily.Elemental);
    public static readonly DamageTypeDefinition Poison = new("poison", "Poison", DamageFamily.Elemental);

    public static readonly DamageTypeDefinition Force = new("force", "Force", DamageFamily.Ethereal);
    public static readonly DamageTypeDefinition Holy = new("holy", "Holy", DamageFamily.Ethereal);
    public static readonly DamageTypeDefinition Necrotic = new("necrotic", "Necrotic", DamageFamily.Ethereal);
    public static readonly DamageTypeDefinition Psychic = new("psychic", "Psychic", DamageFamily.Ethereal);
    public static readonly DamageTypeDefinition Sonic = new("sonic", "Sonic", DamageFamily.Ethereal);
    public static readonly DamageTypeDefinition Unholy = new("unholy", "Unholy", DamageFamily.Ethereal);

    public static IReadOnlyList<DamageTypeDefinition> All => new[]
    {
        Bludgeoning,
        Piercing,
        Slashing,
        Acid,
        Air,
        Electric,
        Fire,
        Frost,
        Poison,
        Force,
        Holy,
        Necrotic,
        Psychic,
        Sonic,
        Unholy
    };
}