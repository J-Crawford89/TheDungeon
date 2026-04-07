public sealed record DamageComponent(
    DiceExpression DamageDice,
    int FlatAmount,
    DamageTypeDefinition DamageType
);