#nullable enable

/// <summary>Opens the modal container loot picker (Godot overlay implements this).</summary>
public interface IContainerLootOverlayOpener
{
	void OpenLootPanel(int containerOrdinal);
}
