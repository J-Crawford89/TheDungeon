#nullable enable
using Godot;

public partial class LogEntryControl : PanelContainer
{
	private const string FontColorKey = "font_color";

	[Export] private Label? _label;

	public override void _Ready()
	{
		if (_label == null)
			GD.PushWarning("LogEntryControl: assign the Label export.");
		else
			ConfigureLabel();
	}

	public void SetEntry(LogEntry entry)
	{
		if (_label == null)
		{
			GD.PushWarning("LogEntryControl: assign the Label export.");
			return;
		}

		ConfigureLabel();
		_label.Text = entry.Text;
		_label.TooltipText = entry.DetailText ?? string.Empty;
		_label.AddThemeColorOverride(FontColorKey, ColorForKind(entry.Kind));
	}

	private void ConfigureLabel()
	{
		if (_label == null)
			return;
		_label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
	}

	private static Color ColorForKind(LogEntryKind kind) =>
		kind switch
		{
			LogEntryKind.Debug => Color.FromHtml("#ff7f50"),
			LogEntryKind.Error => Color.FromHtml("#ff6b6b"),
			LogEntryKind.Important => Color.FromHtml("#ffd700"),
			LogEntryKind.Roll => Color.FromHtml("#88ccff"),
			_ => Color.FromHtml("#e8e8e8")
		};
}
