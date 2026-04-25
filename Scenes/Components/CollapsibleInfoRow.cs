using Godot;
using System;

public partial class CollapsibleInfoRow : VBoxContainer
{
	[Export] private Button _headerButton;
	[Export] private MarginContainer _bodyMargin;
	[Export] private RichTextLabel _bodyLabel;

	private bool _isExpanded;
	private string _title = string.Empty;

	public override void _Ready()
	{
		_headerButton.Pressed += Toggle;
		SetExpanded(false);
	}

	public override void _ExitTree()
	{
		_headerButton.Pressed -= Toggle;
	}

	public void SetContent(string title, string description)
	{
		_title = title ?? string.Empty;
		_bodyLabel.Text = description ?? string.Empty;
		RefreshHeaderText();
	}

	private void Toggle()
	{
		SetExpanded(!_isExpanded);
	}

	private void SetExpanded(bool expanded)
	{
		_isExpanded = expanded;
		_bodyMargin.Visible = expanded;
		RefreshHeaderText();
	}

	private void RefreshHeaderText()
	{
		var symbol = _isExpanded ? "▼" : "▶";
		_headerButton.Text = $"  {symbol}   {_title}";
	}
}
