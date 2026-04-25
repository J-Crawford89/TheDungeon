using Xunit;

public sealed class DamageTypeDefinitionTests
{
	[Fact]
	public void Record_preserves_id_name_family()
	{
		var d = new DamageTypeDefinition("slashing", "Slashing", DamageFamily.Physical);
		Assert.Equal("slashing", d.Id);
		Assert.Equal("Slashing", d.Name);
		Assert.Equal(DamageFamily.Physical, d.Family);
	}
}
