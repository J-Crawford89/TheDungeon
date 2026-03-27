using System;

public sealed class AbilityScores
{
    public int Might { get; set; }
    public int Constitution { get; set; }
    public int Dexterity { get; set; }
    public int Agility { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Gravitas { get; set; }
    public int Luck { get; set; }

    public int GetScore(AbilityScore ability) =>
        ability switch
        {
            AbilityScore.Might => Might,
            AbilityScore.Constitution => Constitution,
            AbilityScore.Dexterity => Dexterity,
            AbilityScore.Agility => Agility,
            AbilityScore.Intelligence => Intelligence,
            AbilityScore.Wisdom => Wisdom,
            AbilityScore.Gravitas => Gravitas,
            AbilityScore.Luck => Luck,
            _ => throw new ArgumentOutOfRangeException(nameof(ability)),
        };
}