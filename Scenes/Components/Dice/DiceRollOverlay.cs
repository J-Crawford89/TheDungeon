using Godot;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public partial class DiceRollOverlay : Control
{
	[Export] public PackedScene RollingDieScene { get; set; } = null!;
	[Export] public NodePath DiceSpawnPath { get; set; }
	[Export] public NodePath CameraPath { get; set; }
	[Export] public DieVisualCatalogLibrary VisualCatalogLibrary { get; set; } = null!;
	[Export] public Vector3 SpawnOffsetStep { get; set; } = new(1.4f, 0f, 0f);
	[Export] public Vector3 SpawnBoundsHalfExtents { get; set; } = new(7f, 0f, 3.5f);
	[Export] public float SpawnHeight { get; set; } = 2.5f;
	[Export] public int MaxSpawnColumns { get; set; } = 6;
	[Export] public int MaxConcurrentDice { get; set; } = 12;
	[Export] public float DieLingerSeconds { get; set; } = 1.5f;

	private readonly object _spawnLock = new();
	private Node3D? _spawnRoot;
	private Camera3D? _camera;
	private int _activeCount;
	private int _spawnSlot;

	public bool IsPresenting => _activeCount > 0;

	public override void _Ready()
	{
		if (!DiceSpawnPath.IsEmpty)
			_spawnRoot = GetNodeOrNull<Node3D>(DiceSpawnPath);
		if (!CameraPath.IsEmpty)
			_camera = GetNodeOrNull<Camera3D>(CameraPath);
		if (_spawnRoot == null)
			GD.PushError($"{nameof(DiceRollOverlay)}: assign {nameof(DiceSpawnPath)}.");
		if (_camera == null)
			GD.PushError($"{nameof(DiceRollOverlay)}: assign {nameof(CameraPath)}.");
		if (RollingDieScene == null)
			GD.PushError($"{nameof(DiceRollOverlay)}: assign {nameof(RollingDieScene)}.");

		if (!Engine.IsEditorHint())
		{
			ClearSpawnRootPlaceholders();
			Visible = false;
		}
	}

	public Task PresentDieAsync(PhysicalDieRollSpec spec, CancellationToken ct = default) =>
		PresentDiceBatchAsync([spec], ct);

	public async Task PresentDiceBatchAsync(IReadOnlyList<PhysicalDieRollSpec> dice, CancellationToken ct = default)
	{
		if (dice.Count == 0)
			return;
		if (_spawnRoot == null || RollingDieScene == null)
			return;

		var offsets = new Vector3[dice.Count];
		for (var i = 0; i < dice.Count; i++)
			offsets[i] = AllocateSpawnOffset();

		var tasks = new List<Task>(dice.Count);
		for (var i = 0; i < dice.Count; i++)
		{
			ct.ThrowIfCancellationRequested();
			tasks.Add(SpawnAndRollAsync(dice[i], offsets[i], ct));
		}

		await Task.WhenAll(tasks);
	}

	private async Task SpawnAndRollAsync(PhysicalDieRollSpec spec, Vector3 spawnOffset, CancellationToken ct)
	{
		if (_activeCount >= MaxConcurrentDice)
		{
			GD.PushWarning($"{nameof(DiceRollOverlay)}: max concurrent dice ({MaxConcurrentDice}); skipping roll.");
			return;
		}

		_activeCount++;
		UpdateOverlayVisibility();

		RollingDie? die = null;
		try
		{
			die = RollingDieScene.Instantiate<RollingDie>();
			die.SpawnPosition = spawnOffset;

			var visual = VisualCatalogLibrary?.Resolve(spec.Kind, spec.DieType, spec.Role);
			if (visual == null)
				GD.PushWarning($"{nameof(DiceRollOverlay)}: no visual for {spec.Kind} {spec.DieType} {spec.Role}.");
			die.SetVisual(visual);
			_spawnRoot!.AddChild(die);

			await die.RollAsync(spec.FaceValue, _camera, ct);

			if (DieLingerSeconds > 0f && IsInstanceValid(die))
			{
				await ToSignal(die.GetTree().CreateTimer(DieLingerSeconds), SceneTreeTimer.SignalName.Timeout);
				ct.ThrowIfCancellationRequested();
			}
		}
		finally
		{
			_activeCount--;
			die?.QueueFree();
			UpdateOverlayVisibility();
			if (_activeCount == 0)
				_spawnSlot = 0;
		}
	}

	private void UpdateOverlayVisibility()
	{
		if (Engine.IsEditorHint())
			return;
		Visible = _activeCount > 0 || HasDisplayedDice();
	}

	private bool HasDisplayedDice()
	{
		if (_spawnRoot == null)
			return false;
		foreach (var child in _spawnRoot.GetChildren())
		{
			if (child is RollingDie)
				return true;
		}
		return false;
	}

	private Vector3 AllocateSpawnOffset()
	{
		lock (_spawnLock)
		{
			var column = _spawnSlot % MaxSpawnColumns;
			var row = _spawnSlot / MaxSpawnColumns;
			_spawnSlot++;

			var centeredColumn = column - (MaxSpawnColumns - 1) * 0.5f;
			var x = centeredColumn * SpawnOffsetStep.X;
			var z = row * SpawnOffsetStep.Z;

			x = Mathf.Clamp(x, -SpawnBoundsHalfExtents.X, SpawnBoundsHalfExtents.X);
			z = Mathf.Clamp(z, -SpawnBoundsHalfExtents.Z, SpawnBoundsHalfExtents.Z);

			return new Vector3(x, SpawnHeight, z);
		}
	}

	private void ClearSpawnRootPlaceholders()
	{
		if (_spawnRoot == null)
			return;
		foreach (var child in _spawnRoot.GetChildren())
			child.QueueFree();
	}
}
