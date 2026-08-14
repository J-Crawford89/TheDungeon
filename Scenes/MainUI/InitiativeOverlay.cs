#nullable enable
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class InitiativeOverlay : Control
{
	[Export] private Label _orderLabel = null!;
	[Export] private AnimationPlayer? _animationPlayer;
	[Export] private string _revealAnimation = "initiative_reveal";

	public override void _Ready()
	{
		if (_orderLabel == null)
			GD.PushError("InitiativeOverlay: assign the Order Label export to InitiativeOrderLabel.");
		Visible = false;
	}

	public async Task ShowOrderAsync(IReadOnlyList<string> names)
	{
		if (_orderLabel != null)
			_orderLabel.Text = names == null || names.Count == 0
				? string.Empty
				: string.Join(" → ", names);

		Visible = true;
		await PlayIfPresentAsync(_revealAnimation);
		Visible = false;
	}

	public void HideImmediate()
	{
		_animationPlayer?.Stop();
		Visible = false;
	}

	private async Task PlayIfPresentAsync(string animation)
	{
		if (_animationPlayer == null || string.IsNullOrWhiteSpace(animation))
			return;
		if (!_animationPlayer.HasAnimation(animation))
		{
			GD.PushError($"InitiativeOverlay: AnimationPlayer is missing '{animation}'.");
			return;
		}

		_animationPlayer.Play(animation);
		await ToSignal(_animationPlayer, AnimationMixer.SignalName.AnimationFinished);
	}
}
