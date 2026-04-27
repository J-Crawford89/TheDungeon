using System;
using Xunit;

public sealed class AbilityScoresTests
{
	[Fact]
	public void GetScore_ReturnsMappedAbilityValues()
	{
		var scores = new AbilityScores
		{
			Might = 1,
			Constitution = 2,
			Dexterity = 3,
			Agility = 4,
			Intelligence = 5,
			Wisdom = 6,
			Gravitas = 7,
			Luck = 8,
		};

		Assert.Equal(1, scores.GetScore(AbilityScore.Might));
		Assert.Equal(2, scores.GetScore(AbilityScore.Constitution));
		Assert.Equal(3, scores.GetScore(AbilityScore.Dexterity));
		Assert.Equal(4, scores.GetScore(AbilityScore.Agility));
		Assert.Equal(5, scores.GetScore(AbilityScore.Intelligence));
		Assert.Equal(6, scores.GetScore(AbilityScore.Wisdom));
		Assert.Equal(7, scores.GetScore(AbilityScore.Gravitas));
		Assert.Equal(8, scores.GetScore(AbilityScore.Luck));
	}

	[Fact]
	public void GetScore_InvalidEnum_Throws()
	{
		var scores = new AbilityScores();
		Assert.Throws<ArgumentOutOfRangeException>(() => scores.GetScore((AbilityScore)999));
	}
}
