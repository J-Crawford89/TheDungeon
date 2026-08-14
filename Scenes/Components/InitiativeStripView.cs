#nullable enable
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class InitiativeStripView : MarginContainer
{
	[Export] private HBoxContainer _order = null!;
	[Export] private Control _inactiveTemplate = null!;
	[Export] private Control _activeTemplate = null!;
	[Export] private AnimationPlayer? _animationPlayer;

	//ANIMATIONS
	[Export] private string _fadeInAnimation = "initiative_strip_fade_in";
	[Export] private string _fadeOutAnimation = "initiative_strip_fade_out";

	public override void _Ready()
	{
		if (_order == null)
			GD.PushError("InitiativeStripView: assign the Order export to InitiativeOrder.");
		if (_inactiveTemplate == null)
			GD.PushError("InitiativeStripView: assign the Inactive Template export to Combatant1.");
		if (_activeTemplate == null)
			GD.PushError("InitiativeStripView: assign the Active Template export to Combatant2.");

		if (_inactiveTemplate != null)
			_inactiveTemplate.Visible = false;
		if (_activeTemplate != null)
			_activeTemplate.Visible = false;
		Visible = false;
	}

	public async Task ShowOrderAsync(IReadOnlyList<string> names, int currentIndex)
	{
		Rebuild(names, currentIndex);
		Visible = true;
		await PlayIfPresentAsync(_fadeInAnimation);
	}

	public void SetCurrentIndex(IReadOnlyList<string> names, int currentIndex) =>
		Rebuild(names, currentIndex);

	public async Task FadeOutAsync()
	{
		if (!Visible)
			return;
		await PlayIfPresentAsync(_fadeOutAnimation);
		Visible = false;
	}

	public void HideImmediate()
	{
		_animationPlayer?.Stop();
		Visible = false;
	}

	private void Rebuild(IReadOnlyList<string> names, int currentIndex)
	{
		if (_order == null || _inactiveTemplate == null || _activeTemplate == null)
			return;

		ClearLiveSlots();
		if (names == null || names.Count == 0)
			return;

		for (var i = 0; i < names.Count; i++)
		{
			var template = i == currentIndex ? _activeTemplate : _inactiveTemplate;
			if (template.Duplicate() is not Control slot)
				continue;
			slot.Visible = true;
			SetNameLabel(slot, names[i]);
			_order.AddChild(slot);
		}
	}

	private void ClearLiveSlots()
	{
		foreach (var child in _order.GetChildren())
		{
			if (child == _inactiveTemplate || child == _activeTemplate)
				continue;
			_order.RemoveChild(child);
			child.QueueFree();
		}
	}

	private static void SetNameLabel(Node slot, string name)
	{
		if (slot.FindChild("CombatantNameLabel", recursive: true, owned: false) is Label label)
			label.Text = name;
	}

	private async Task PlayIfPresentAsync(string animation)
	{
		if (_animationPlayer == null || string.IsNullOrWhiteSpace(animation))
			return;
		if (!_animationPlayer.HasAnimation(animation))
		{
			GD.PushError($"InitiativeStripView: AnimationPlayer is missing '{animation}'.");
			return;
		}

		_animationPlayer.Play(animation);
		await ToSignal(_animationPlayer, AnimationMixer.SignalName.AnimationFinished);
	}
}
