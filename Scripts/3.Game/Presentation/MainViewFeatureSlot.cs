#nullable enable

/// <summary>One leaf entity in the main view feature row (icon + stable highlight key).</summary>
public sealed class MainViewFeatureSlot
{
	public required string HighlightKey { get; init; }

	/// <summary>Presentation key resolved to a texture in the Godot layer; see <see cref="MainViewPresentationIconKeys"/>.</summary>
	public required string PresentationIconKey { get; init; }
	/// <summary>When in target-selection mode, label under the icon (matches command button text).</summary>
	public string? TargetingLabel { get; init; }
}
