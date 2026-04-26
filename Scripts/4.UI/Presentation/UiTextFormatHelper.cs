/// <summary>UI-friendly entry point; delegates to <see cref="DisplayIdHelper"/> for shared rules.</summary>
public static class UiTextFormatHelper
{
	public static string FromIdToDisplayName(string? id) =>
		DisplayIdHelper.FormatSnakeOrRawId(id);
}
