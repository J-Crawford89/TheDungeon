using Xunit;

public sealed class CharacterNameValidatorTests
{
	[Fact]
	public void Sanitize_KeepsAllowedCharacters_AndStripsDisallowed()
	{
		var value = CharacterNameValidator.Sanitize("Ari-Jean O'Malley_123!");

		Assert.Equal("Ari-Jean O'Malley", value);
	}

	[Fact]
	public void Sanitize_EnforcesMaxLength()
	{
		var longName = new string('A', CharacterNameValidator.MaxLength + 8);

		var value = CharacterNameValidator.Sanitize(longName);

		Assert.Equal(CharacterNameValidator.MaxLength, value.Length);
	}

	[Fact]
	public void IsNonEmptyValid_RejectsWhitespaceAndDisallowedOnly()
	{
		Assert.False(CharacterNameValidator.IsNonEmptyValid("   "));
		Assert.False(CharacterNameValidator.IsNonEmptyValid("1234!!"));
		Assert.True(CharacterNameValidator.IsNonEmptyValid("Ari"));
	}
}
