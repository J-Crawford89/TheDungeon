using System.Collections.Generic;
using Xunit;

public sealed class UnarmedProficiencyResolverTests
{
	[Fact]
	public void Resolve_WhenAllOnly_ReturnsAllWithLabel()
	{
		var d = new Dictionary<ProficiencyKey, ProficiencyRank>
		{
			[new ProficiencyKey(ProficiencyTargetType.UnarmedStrikes, UnarmedProficiencyIds.All)] = ProficiencyRank.Trained,
		};

		var r = UnarmedProficiencyResolver.Resolve(d, null);

		Assert.Equal(ProficiencyRank.Trained, r.Rank);
		Assert.Equal("Unarmed Strikes Proficiency", r.ModifierSourceLabel);
	}

	[Fact]
	public void Resolve_WhenSpecificOnly_UsesSpecificLabel()
	{
		var d = new Dictionary<ProficiencyKey, ProficiencyRank>
		{
			[new ProficiencyKey(ProficiencyTargetType.UnarmedStrikes, "sting")] = ProficiencyRank.Expert,
		};

		var r = UnarmedProficiencyResolver.Resolve(d, "sting");

		Assert.Equal(ProficiencyRank.Expert, r.Rank);
		Assert.Contains("sting", r.ModifierSourceLabel, System.StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void Resolve_WhenBoth_PrefersHigherRank()
	{
		var d = new Dictionary<ProficiencyKey, ProficiencyRank>
		{
			[new ProficiencyKey(ProficiencyTargetType.UnarmedStrikes, "a")] = ProficiencyRank.Trained,
			[new ProficiencyKey(ProficiencyTargetType.UnarmedStrikes, UnarmedProficiencyIds.All)] = ProficiencyRank.Expert,
		};

		var r = UnarmedProficiencyResolver.Resolve(d, "a");

		Assert.Equal(ProficiencyRank.Expert, r.Rank);
	}

	[Fact]
	public void Resolve_OnTie_PrefersSpecificOverAll()
	{
		var d = new Dictionary<ProficiencyKey, ProficiencyRank>
		{
			[new ProficiencyKey(ProficiencyTargetType.UnarmedStrikes, "bite")] = ProficiencyRank.Trained,
			[new ProficiencyKey(ProficiencyTargetType.UnarmedStrikes, UnarmedProficiencyIds.All)] = ProficiencyRank.Trained,
		};

		var r = UnarmedProficiencyResolver.Resolve(d, "bite");

		Assert.Equal(ProficiencyRank.Trained, r.Rank);
		Assert.Contains("bite", r.ModifierSourceLabel, System.StringComparison.OrdinalIgnoreCase);
		Assert.NotEqual("Unarmed Strikes Proficiency", r.ModifierSourceLabel);
	}

	[Fact]
	public void Resolve_WhenNeither_Untrained()
	{
		var r = UnarmedProficiencyResolver.Resolve(new Dictionary<ProficiencyKey, ProficiencyRank>(), "x");
		Assert.Equal(ProficiencyRank.Untrained, r.Rank);
		Assert.Equal("", r.ModifierSourceLabel);
	}
}
