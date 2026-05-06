using System.Linq;
using Xunit;

public sealed class PlayerCharacterSnapshotTests
{
	[Fact]
	public void From_CopiesCoreFieldsAndGrantedAbilities()
	{
		var player = new PlayerState
		{
			Name = "Ari",
			Sex = CharacterSex.Female,
			MaxHp = 22,
			Level = 3,
			Purse = new CoinPurse { Copper = 40 },
			Facing = HorizontalDirection.East,
		};
		player.AbilityScores.Might = 14;
		player.GrantedAbilities.Add(new GrantedAbility { AbilityId = AbilityIds.Defend });
		player.GrantedAbilities.Add(new GrantedAbility { AbilityId = "custom.test" });

		var snap = PlayerCharacterSnapshot.From(player);

		Assert.Equal("Ari", snap.Name);
		Assert.Equal(CharacterSex.Female, snap.Sex);
		Assert.Equal(22, snap.MaxHp);
		Assert.Equal(3, snap.Level);
		Assert.Equal(40, snap.Purse.Copper);
		Assert.Equal(HorizontalDirection.East, snap.Facing);
		Assert.Equal(14, snap.AbilityScores.Might);
		Assert.Equal(
			new[] { AbilityIds.Defend, "custom.test" }.OrderBy(x => x).ToArray(),
			snap.GrantedAbilityIds.OrderBy(x => x).ToArray());
	}
}
