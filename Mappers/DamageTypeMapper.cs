#nullable enable

public static class DamageTypeMapper
{
	public static DamageTypeDefinition ToDomain(DamageTypeResource resource)
	{
		var id = resource.Id?.Trim() ?? "";
		var name = resource.Name?.Trim() ?? "";
		return new DamageTypeDefinition(id, name, resource.Family);
	}
}
