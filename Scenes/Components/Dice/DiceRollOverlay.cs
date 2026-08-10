using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public partial class DiceRollOverlay : Control
{
	[Export] public PackedScene RollingDieScene { get; set; } = null!;
	[Export] public NodePath DiceSpawnPath { get; set; }
	[Export] public NodePath CameraPath { get; set; }
	[Export] public DieVisualCatalogLibrary VisualCatalogLibrary { get; set; } = null!;
	[Export] public Vector3 SpawnBoundsHalfExtents { get; set; } = new(7f, 0f, 3.5f);
	[Export] public float SpawnHeight { get; set; } = 1.35f;
	[Export] public float SpawnEdgeMargin { get; set; } = 0.75f;
	[Export] public float MinimumSpawnSeparation { get; set; } = 1.25f;
	[Export] public int RandomSpawnAttempts { get; set; } = 16;
	[Export] public int MaxConcurrentDice { get; set; } = 12;
	[Export] public float DieLingerSeconds { get; set; } = 1.5f;

	private readonly object _spawnLock = new();
	private Node3D? _spawnRoot;
	private Camera3D? _camera;
	private int _activeCount;

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
		var reserved = SnapshotReservedSpawnPositions(dice.Count);
		for (var i = 0; i < dice.Count; i++)
			offsets[i] = AllocateSpawnOffset(reserved);

		var spawned = new List<RollingDie>(dice.Count);
		var faceValues = new List<int>(dice.Count);
		try
		{
			for (var i = 0; i < dice.Count; i++)
			{
				ct.ThrowIfCancellationRequested();
				var die = SpawnDie(dice[i], offsets[i]);
				if (die == null)
					continue;
				spawned.Add(die);
				faceValues.Add(dice[i].FaceValue);
			}

			await RollingDie.RollPredeterminedBatchAsync(spawned, faceValues, ct);
		}
		catch
		{
			foreach (var die in spawned)
				ReleaseDie(die);
			throw;
		}

		foreach (var die in spawned)
		{
			if (DieLingerSeconds > 0f && IsInstanceValid(die))
				_ = LingerAndReleaseAsync(die);
			else
				ReleaseDie(die);
		}
	}

	private RollingDie? SpawnDie(PhysicalDieRollSpec spec, Vector3 spawnOffset)
	{
		if (_activeCount >= MaxConcurrentDice)
		{
			GD.PushWarning($"{nameof(DiceRollOverlay)}: max concurrent dice ({MaxConcurrentDice}); skipping roll.");
			return null;
		}

		var die = RollingDieScene.Instantiate<RollingDie>();
		die.SpawnPosition = spawnOffset;
		die.SimulationBoundsHalfExtents = SpawnBoundsHalfExtents;
		var visual = VisualCatalogLibrary?.Resolve(spec.Kind, spec.DieType, spec.Role);
		if (visual == null)
			GD.PushWarning($"{nameof(DiceRollOverlay)}: no visual for {spec.Kind} {spec.DieType} {spec.Role}.");
		die.SetVisual(visual);
		_spawnRoot!.AddChild(die);
		_activeCount++;
		UpdateOverlayVisibility();
		return die;
	}
	private async Task LingerAndReleaseAsync(RollingDie die)
	{
		try
		{
			if (IsInstanceValid(die))
				await ToSignal(die.GetTree().CreateTimer(DieLingerSeconds), SceneTreeTimer.SignalName.Timeout);
		}
		finally
		{
			ReleaseDie(die);
		}
	}

	private void ReleaseDie(RollingDie? die)
	{
		if (_activeCount > 0)
			_activeCount--;
		if (IsInstanceValid(die))
			die!.QueueFree();
		UpdateOverlayVisibility();
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

	private Vector3 AllocateSpawnOffset(List<System.Numerics.Vector3> reserved)
	{
		lock (_spawnLock)
		{
			System.Numerics.Vector3 candidate = default;
			var attempts = Math.Max(1, RandomSpawnAttempts);
			for (var attempt = 0; attempt < attempts; attempt++)
			{
				candidate = DiceSpawnPositionPicker.Pick(
					GD.Randf(),
					GD.Randf(),
					new System.Numerics.Vector3(
						SpawnBoundsHalfExtents.X,
						SpawnBoundsHalfExtents.Y,
						SpawnBoundsHalfExtents.Z),
					SpawnHeight,
					SpawnEdgeMargin);
				if (DiceSpawnPositionPicker.HasMinimumSeparation(
					candidate,
					reserved,
					MinimumSpawnSeparation))
					break;
			}

			reserved.Add(candidate);
			return new Vector3(candidate.X, candidate.Y, candidate.Z);
		}
	}

	private List<System.Numerics.Vector3> SnapshotReservedSpawnPositions(int additionalCapacity)
	{
		lock (_spawnLock)
		{
			var reserved = new List<System.Numerics.Vector3>(_activeCount + additionalCapacity);
			if (_spawnRoot == null)
				return reserved;

			foreach (var child in _spawnRoot.GetChildren())
			{
				if (child is not RollingDie die || !IsInstanceValid(die))
					continue;
				var position = die.SpawnPosition;
				reserved.Add(new System.Numerics.Vector3(position.X, position.Y, position.Z));
			}
			return reserved;
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
