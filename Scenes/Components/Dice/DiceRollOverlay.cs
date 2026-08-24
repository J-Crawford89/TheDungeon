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

	[ExportGroup("Dice Audio")]
	[Export] public NodePath ThrowPlayerPath { get; set; } = new("DiceThrowPlayer");
	[Export] public NodePath ImpactPlayerPath { get; set; } = new("DiceImpactPlayer");
	[Export] public NodePath SettlePlayerPath { get; set; } = new("DiceSettlePlayer");
	[Export] public Godot.Collections.Array<AudioStream> ThrowStreams { get; set; } = [];
	[Export] public Godot.Collections.Array<AudioStream> ImpactStreams { get; set; } = [];
	[Export] public Godot.Collections.Array<AudioStream> SettleStreams { get; set; } = [];
	[Export(PropertyHint.Range, "-40,6,0.5")] public float ThrowVolumeDb { get; set; } = -7f;
	[Export(PropertyHint.Range, "-40,6,0.5")] public float ImpactVolumeDb { get; set; } = -9f;
	[Export(PropertyHint.Range, "-40,6,0.5")] public float SettleVolumeDb { get; set; } = -10f;
	[Export(PropertyHint.Range, "0,0.25,0.01")] public float PitchVariation { get; set; } = 0.08f;
	[Export(PropertyHint.Range, "0.03,0.3,0.005")] public float MinimumImpactIntervalSeconds { get; set; } = 0.065f;
	[Export(PropertyHint.Range, "1,16,1")] public int MaximumImpactSoundsPerRoll { get; set; } = 8;

	private readonly object _spawnLock = new();
	private readonly SemaphoreSlim _presentationGate = new(1, 1);
	private Node3D? _spawnRoot;
	private Camera3D? _camera;
	private AudioStreamPlayer? _throwPlayer;
	private AudioStreamPlayer? _impactPlayer;
	private AudioStreamPlayer? _settlePlayer;
	private readonly RandomNumberGenerator _audioRandom = new();
	private readonly List<ImpactCue> _impactSchedule = new();
	private int _nextImpactCue;
	private int _lastThrowIndex = -1;
	private int _lastImpactIndex = -1;
	private int _lastSettleIndex = -1;
	private int _activeCount;

	public bool IsPresenting => _activeCount > 0;

	public override void _Ready()
	{
		if (!DiceSpawnPath.IsEmpty)
			_spawnRoot = GetNodeOrNull<Node3D>(DiceSpawnPath);
		if (!CameraPath.IsEmpty)
			_camera = GetNodeOrNull<Camera3D>(CameraPath);
		if (!ThrowPlayerPath.IsEmpty)
			_throwPlayer = GetNodeOrNull<AudioStreamPlayer>(ThrowPlayerPath);
		if (!ImpactPlayerPath.IsEmpty)
			_impactPlayer = GetNodeOrNull<AudioStreamPlayer>(ImpactPlayerPath);
		if (!SettlePlayerPath.IsEmpty)
			_settlePlayer = GetNodeOrNull<AudioStreamPlayer>(SettlePlayerPath);
		if (_spawnRoot == null)
			GD.PushError($"{nameof(DiceRollOverlay)}: assign {nameof(DiceSpawnPath)}.");
		if (_camera == null)
			GD.PushError($"{nameof(DiceRollOverlay)}: assign {nameof(CameraPath)}.");
		if (RollingDieScene == null)
			GD.PushError($"{nameof(DiceRollOverlay)}: assign {nameof(RollingDieScene)}.");
		if (_throwPlayer == null || _impactPlayer == null || _settlePlayer == null)
			GD.PushWarning($"{nameof(DiceRollOverlay)}: assign all three dice audio players.");

		_audioRandom.Randomize();
		if (ThrowStreams.Count == 0 || ImpactStreams.Count == 0 || SettleStreams.Count == 0)
			GD.PushWarning($"{nameof(DiceRollOverlay)}: assign all three exported dice audio palettes.");

		if (!Engine.IsEditorHint())
		{
			ClearSpawnRootPlaceholders();
			Visible = false;
		}
	}

	public Task PresentDieAsync(
		PhysicalDieRollSpec spec,
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default) =>
		PresentDiceBatchAsync([spec], profile, ct);

	public async Task PresentDiceBatchAsync(
		IReadOnlyList<PhysicalDieRollSpec> dice,
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default)
	{
		if (dice.Count == 0)
			return;

		await _presentationGate.WaitAsync(ct);
		try
		{
			if (_spawnRoot == null || RollingDieScene == null)
				return;

			var offsets = new Vector3[dice.Count];
			var reserved = SnapshotReservedSpawnPositions(dice.Count);
			for (var i = 0; i < dice.Count; i++)
				offsets[i] = AllocateSpawnOffset(reserved);

			var timing = DicePresentationTiming.For(profile);
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
					die.MaximumPredeterminedPlaybackSeconds *= timing.PlaybackDurationMultiplier;
					die.PostRollDisplaySeconds *= timing.ResultPauseMultiplier;
					spawned.Add(die);
					faceValues.Add(dice[i].FaceValue);
				}

				await RollingDie.RollPredeterminedBatchWithDiagnosticsAsync(
					spawned,
					faceValues,
					ct,
					BeginAudioPlayback,
					UpdateAudioPlayback,
					CompleteAudioPlayback);
			}
			finally
			{
				foreach (var die in spawned)
					ReleaseDie(die);
			}
		}
		finally
		{
			_presentationGate.Release();
		}
	}

	private void BeginAudioPlayback(PredeterminedDicePlaybackTiming timing)
	{
		PlayVariant(_throwPlayer, ThrowStreams, ref _lastThrowIndex, ThrowVolumeDb);
		BuildImpactSchedule(timing);
	}

	private void UpdateAudioPlayback(float progress)
	{
		while (_nextImpactCue < _impactSchedule.Count &&
			_impactSchedule[_nextImpactCue].Progress <= progress)
		{
			var cue = _impactSchedule[_nextImpactCue++];
			PlayVariant(
				_impactPlayer,
				ImpactStreams,
				ref _lastImpactIndex,
				ImpactVolumeDb + cue.VolumeOffsetDb);
		}
	}

	private void CompleteAudioPlayback()
	{
		PlayVariant(_settlePlayer, SettleStreams, ref _lastSettleIndex, SettleVolumeDb);
		_impactSchedule.Clear();
		_nextImpactCue = 0;
	}

	private void BuildImpactSchedule(PredeterminedDicePlaybackTiming timing)
	{
		_impactSchedule.Clear();
		_nextImpactCue = 0;
		if (timing.VisibleDurationSeconds <= 0f)
			return;

		var candidates = new List<ImpactCue>();
		foreach (var trajectory in timing.Trajectories)
		{
			var frames = trajectory.Frames;
			if (frames.Count < 3 || trajectory.DurationSeconds <= 0f)
				continue;
			var stepSeconds = trajectory.DurationSeconds / (frames.Count - 1);
			for (var i = 2; i < frames.Count; i++)
			{
				var previousVelocity = (frames[i - 1].Position - frames[i - 2].Position) / stepSeconds;
				var currentVelocity = (frames[i].Position - frames[i - 1].Position) / stepSeconds;
				var velocityChange = (currentVelocity - previousVelocity).Length();
				if (velocityChange < 0.65f)
					continue;
				var progress = (float)i / (frames.Count - 1);
				candidates.Add(new ImpactCue(progress, velocityChange));
			}
		}

		foreach (var contact in timing.PairContacts)
		{
			if (timing.RecordedDurationSeconds <= 0f || contact.ApproachSpeedBefore < 0.2f)
				continue;
			candidates.Add(new ImpactCue(
				Math.Clamp(contact.TimeSeconds / timing.RecordedDurationSeconds, 0f, 1f),
				contact.ApproachSpeedBefore * 1.5f));
		}

		candidates.Sort((left, right) => left.Progress.CompareTo(right.Progress));
		var minimumProgressGap = MinimumImpactIntervalSeconds / timing.VisibleDurationSeconds;
		foreach (var candidate in candidates)
		{
			if (candidate.Progress < 0.035f || candidate.Progress > 0.94f)
				continue;
			if (_impactSchedule.Count == 0 ||
				candidate.Progress - _impactSchedule[^1].Progress >= minimumProgressGap)
			{
				_impactSchedule.Add(candidate);
			}
			else if (candidate.Strength > _impactSchedule[^1].Strength)
			{
				_impactSchedule[^1] = candidate;
			}
		}

		if (_impactSchedule.Count > MaximumImpactSoundsPerRoll)
			_impactSchedule.RemoveRange(
				MaximumImpactSoundsPerRoll,
				_impactSchedule.Count - MaximumImpactSoundsPerRoll);
		for (var i = 0; i < _impactSchedule.Count; i++)
		{
			var cue = _impactSchedule[i];
			_impactSchedule[i] = cue with
			{
				VolumeOffsetDb = Mathf.Lerp(-5f, 1f, Math.Clamp(cue.Strength / 5f, 0f, 1f)),
			};
		}
	}

	private void PlayVariant(
		AudioStreamPlayer? player,
		IReadOnlyList<AudioStream> streams,
		ref int lastIndex,
		float volumeDb)
	{
		if (player == null || streams.Count == 0)
			return;
		var index = streams.Count == 1 ? 0 : _audioRandom.RandiRange(0, streams.Count - 1);
		if (index == lastIndex && streams.Count > 1)
			index = (index + 1 + _audioRandom.RandiRange(0, streams.Count - 2)) % streams.Count;
		lastIndex = index;
		player.Stream = streams[index];
		player.VolumeDb = volumeDb;
		player.PitchScale = _audioRandom.RandfRange(1f - PitchVariation, 1f + PitchVariation);
		player.Play();
	}

	private readonly record struct ImpactCue(
		float Progress,
		float Strength,
		float VolumeOffsetDb = 0f);

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

	private void ReleaseDie(RollingDie? die)
	{
		if (_activeCount > 0)
			_activeCount--;
		if (IsInstanceValid(die))
		{
			var parent = die!.GetParent();
			parent?.RemoveChild(die);
			die.QueueFree();
		}
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
