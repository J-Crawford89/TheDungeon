using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class DiceTestScene : Node3D
{
	[Export] public PackedScene RollingDieScene { get; set; } = null!;
	[Export] public DieVisualCatalogLibrary VisualCatalogLibrary { get; set; } = null!;
	[Export] public NodePath SpawnRootPath { get; set; }
	[Export] public NodePath CameraPath { get; set; } = new("Camera3D");
	[Export] public Vector3 SpawnBoundsHalfExtents { get; set; } = new(7f, 0f, 3.5f);
	[Export] public float SpawnHeight { get; set; } = 1.35f;
	[Export] public float SpawnEdgeMargin { get; set; } = 0.75f;
	[Export] public float MinimumSpawnSeparation { get; set; } = 1.25f;
	[Export] public int RandomSpawnAttempts { get; set; } = 16;
	[Export] public bool PersistDiceUntilClear { get; set; } = true;
	[Export] public float AutoClearLingerSeconds { get; set; } = 1.5f;

	private Node3D? _spawnRoot;
	private Camera3D? _camera;
	private Label? _statusLabel;
	private SpinBox? _spawnCountSpin;
	private SpinBox? _targetFaceSpin;
	private SpinBox? _d100TotalSpin;
	private TestDiePreset? _selectedPreset;
	private RollingDie? _lastSpawnedDie;

	private readonly List<TestDiePreset> _presets =
	[
		new("d3", DieType.d3, DieVisualRole.Standard, 1, 3),
		new("d4", DieType.d4, DieVisualRole.Standard, 1, 4),
		new("d6", DieType.d6, DieVisualRole.Standard, 1, 6),
		new("d8", DieType.d8, DieVisualRole.Standard, 1, 8),
		new("d10", DieType.d10, DieVisualRole.Standard, 1, 10),
		new("d12", DieType.d12, DieVisualRole.Standard, 1, 12),
		new("d20", DieType.d20, DieVisualRole.Standard, 1, 20),
		new("Pct tens", DieType.d10, DieVisualRole.PercentileTens, 0, 90),
		new("d10 ones", DieType.d10, DieVisualRole.Standard, 1, 10),
	];

	public override void _Ready()
	{
		_spawnRoot = GetNodeOrNull<Node3D>(SpawnRootPath);
		_camera = GetNodeOrNull<Camera3D>(CameraPath);
		if (_spawnRoot == null)
			GD.PushError($"{nameof(DiceTestScene)}: assign {nameof(SpawnRootPath)}.");
		if (_camera == null)
			GD.PushError($"{nameof(DiceTestScene)}: assign {nameof(CameraPath)}.");
		if (RollingDieScene == null)
			GD.PushError($"{nameof(DiceTestScene)}: assign {nameof(RollingDieScene)}.");

		_selectedPreset = _presets.FirstOrDefault(p => p.DieType == DieType.d6);
		BuildUi();
		SetStatus("Ready. Persist=" + PersistDiceUntilClear);
	}

	public override async void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		if (key.Keycode == Key.Space && key.ShiftPressed)
		{
			await SpawnAndRollAsync(gameplayRoll: true);
			GetViewport().SetInputAsHandled();
		}
		else if (key.Keycode == Key.Space)
		{
			await SpawnAndRollAsync(gameplayRoll: false);
			GetViewport().SetInputAsHandled();
		}
	}

	private void BuildUi()
	{
		var layer = new CanvasLayer();
		AddChild(layer);

		var panel = new PanelContainer
		{
			Position = new Vector2(8, 8),
			CustomMinimumSize = new Vector2(320, 0),
		};
		layer.AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 8);
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_right", 8);
		margin.AddThemeConstantOverride("margin_bottom", 8);
		panel.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 6);
		margin.AddChild(vbox);

		vbox.AddChild(new Label { Text = "Dice test harness" });

		var dieRow = new HBoxContainer();
		dieRow.AddThemeConstantOverride("separation", 4);
		vbox.AddChild(dieRow);
		foreach (var preset in _presets)
		{
			var btn = new Button { Text = preset.Label, ToggleMode = true };
			btn.ButtonPressed = preset == _selectedPreset;
			btn.Toggled += pressed =>
			{
				if (!pressed)
					return;
				_selectedPreset = preset;
				UpdateFaceSpinRange();
				SetStatus($"Selected {preset.Label} (faces {preset.MinFace}–{preset.MaxFace})");
			};
			dieRow.AddChild(btn);
		}

		var countRow = new HBoxContainer();
		vbox.AddChild(countRow);
		countRow.AddChild(new Label { Text = "Spawn count" });
		_spawnCountSpin = new SpinBox { MinValue = 1, MaxValue = 12, Value = 1 };
		countRow.AddChild(_spawnCountSpin);

		var faceRow = new HBoxContainer();
		vbox.AddChild(faceRow);
		faceRow.AddChild(new Label { Text = "Target face" });
		_targetFaceSpin = new SpinBox { MinValue = 1, MaxValue = 6, Value = 1 };
		faceRow.AddChild(_targetFaceSpin);

		var d100Row = new HBoxContainer();
		vbox.AddChild(d100Row);
		d100Row.AddChild(new Label { Text = "d100 total" });
		_d100TotalSpin = new SpinBox { MinValue = 1, MaxValue = 100, Value = 26 };
		d100Row.AddChild(_d100TotalSpin);
		var d100Btn = new Button { Text = "Spawn+gameplay d100 pair" };
		d100Btn.Pressed += () => _ = SpawnD100GameplayAsync((int)_d100TotalSpin!.Value);
		d100Row.AddChild(d100Btn);

		AddButton(vbox, "Spawn only", () => _ = SpawnDiceAsync(rollFree: false, rollGameplay: false, forcedFace: -1));
		AddButton(vbox, "Spawn + free roll", () => _ = SpawnAndRollAsync(gameplayRoll: false));
		AddButton(vbox, "Spawn + gameplay roll", () => _ = SpawnAndRollAsync(gameplayRoll: true));
		AddButton(vbox, "Re-roll last (free)", () => _ = RerollLastAsync(gameplayRoll: false));
		AddButton(vbox, "Re-roll last (gameplay)", () => _ = RerollLastAsync(gameplayRoll: true));
		AddButton(vbox, "Snap in place (gameplay face)", SnapLastInPlace);
		AddButton(vbox, "Verify calibration (last die)", VerifyLastCalibration);
		AddButton(vbox, "Forced collision: head-on d6 pair", () => _ = SpawnForcedCollisionAsync(glancing: false));
		AddButton(vbox, "Forced collision: glancing d6 pair", () => _ = SpawnForcedCollisionAsync(glancing: true));
		AddButton(vbox, "Clear all", ClearAll);

		_statusLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(300, 48) };
		vbox.AddChild(_statusLabel);

		UpdateFaceSpinRange();
	}

	private static void AddButton(Container parent, string text, Action onPress)
	{
		var btn = new Button { Text = text };
		btn.Pressed += onPress;
		parent.AddChild(btn);
	}

	private void UpdateFaceSpinRange()
	{
		if (_targetFaceSpin == null || _selectedPreset == null)
			return;
		_targetFaceSpin.MinValue = _selectedPreset.MinFace;
		_targetFaceSpin.MaxValue = _selectedPreset.MaxFace;
		_targetFaceSpin.Value = Math.Clamp(_targetFaceSpin.Value, _selectedPreset.MinFace, _selectedPreset.MaxFace);
	}

	private async Task SpawnAndRollAsync(bool gameplayRoll)
	{
		var face = gameplayRoll ? (int)_targetFaceSpin!.Value : -1;
		await SpawnDiceAsync(rollFree: !gameplayRoll, rollGameplay: gameplayRoll, forcedFace: face);
	}

	private async Task SpawnDiceAsync(bool rollFree, bool rollGameplay, int forcedFace)
	{
		if (_selectedPreset == null || _spawnRoot == null || RollingDieScene == null)
			return;

		var count = (int)_spawnCountSpin!.Value;
		var spawned = new List<RollingDie>(count);
		var batchSimulation = PredeterminedDiceSimulationResult.Empty;
		for (var i = 0; i < count; i++)
		{
			var die = CreateDie(_selectedPreset);
			if (die == null)
				continue;
			_lastSpawnedDie = die;
			spawned.Add(die);
		}

		if (rollFree)
			await Task.WhenAll(spawned.Select(die => RollWithOptionalClear(die, -1)));
		else if (rollGameplay)
		{
			batchSimulation = await RollingDie.RollPredeterminedBatchWithDiagnosticsAsync(
				spawned,
				Enumerable.Repeat(forcedFace, spawned.Count).ToArray());
			await Task.WhenAll(spawned.Select(ClearAfterRollIfNeededAsync));
		}

		if (!rollFree && !rollGameplay)
			SetStatus($"Spawned {count}× {_selectedPreset.Label}.");
		else
		{
			var detail = _lastSpawnedDie == null ? "" : $" {DescribeMotion(_lastSpawnedDie)}";
			var contactDetail = rollGameplay ? $" {DescribeBatchContacts(batchSimulation)}" : "";
			SetStatus($"Rolled {_selectedPreset.Label}.{detail}{contactDetail}");
		}
	}

	private async Task RollWithOptionalClear(RollingDie die, int forcedFace)
	{
		await die.RollAsync(forcedFace);
		await ClearAfterRollIfNeededAsync(die);
	}

	private async Task ClearAfterRollIfNeededAsync(RollingDie die)
	{
		if (!PersistDiceUntilClear && AutoClearLingerSeconds > 0f)
		{
			await ToSignal(GetTree().CreateTimer(AutoClearLingerSeconds), SceneTreeTimer.SignalName.Timeout);
			if (IsInstanceValid(die))
				die.QueueFree();
		}
	}

	private async Task SpawnD100GameplayAsync(int total)
	{
		if (_spawnRoot == null || RollingDieScene == null || VisualCatalogLibrary == null)
			return;

		var faces = PercentileDiceFaceMapper.Map(total);
		var tensPreset = _presets.First(p => p.Role == DieVisualRole.PercentileTens);
		var onesPreset = _presets.First(p => p is { DieType: DieType.d10, Role: DieVisualRole.Standard });

		var tens = CreateDie(tensPreset);
		var ones = CreateDie(onesPreset);
		if (tens == null || ones == null)
		{
			SetStatus("Failed to spawn d100 pair.");
			return;
		}

		var simulation = await RollingDie.RollPredeterminedBatchWithDiagnosticsAsync(
			[tens, ones],
			[faces.PercentileTensFace, faces.OnesDieFace]);
		await Task.WhenAll(
			ClearAfterRollIfNeededAsync(tens),
			ClearAfterRollIfNeededAsync(ones));
		_lastSpawnedDie = ones;
		SetStatus($"d100 total {total} → tens {faces.PercentileTensFace}, ones {faces.OnesDieFace}. " +
			DescribeBatchContacts(simulation));
	}

	private async Task SpawnForcedCollisionAsync(bool glancing)
	{
		if (_spawnRoot == null || RollingDieScene == null || VisualCatalogLibrary == null)
			return;

		ClearAll();
		var preset = _presets.First(p => p is { DieType: DieType.d6, Role: DieVisualRole.Standard });
		var firstDie = CreateDie(preset);
		var secondDie = CreateDie(preset);
		if (firstDie == null || secondDie == null ||
			!firstDie.TryPreparePredeterminedRoll(1, out var firstTemplate) ||
			!secondDie.TryPreparePredeterminedRoll(6, out var secondTemplate))
		{
			SetStatus("Failed to prepare the forced d6 collision pair.");
			return;
		}

		var orientation = System.Numerics.Quaternion.CreateFromYawPitchRoll(0.63f, 1.17f, 0.41f);
		var firstVelocity = new System.Numerics.Vector3(7.5f, 0f, 0f);
		var secondVelocity = new System.Numerics.Vector3(-7.5f, 0f, 0f);
		var halfOffsetZ = glancing ? 0.18f : 0f;
		var firstRequest = ConfigureForcedCollisionRequest(
			firstTemplate,
			new System.Numerics.Vector3(-0.75f, 0f, -halfOffsetZ),
			orientation,
			firstVelocity);
		var secondRequest = ConfigureForcedCollisionRequest(
			secondTemplate,
			new System.Numerics.Vector3(0.75f, 0f, halfOffsetZ),
			orientation,
			secondVelocity);
		firstDie.SpawnPosition = new Vector3(
			firstRequest.StartPosition.X,
			firstRequest.StartPosition.Y,
			firstRequest.StartPosition.Z);
		secondDie.SpawnPosition = new Vector3(
			secondRequest.StartPosition.X,
			secondRequest.StartPosition.Y,
			secondRequest.StartPosition.Z);
		firstDie.PlaceAtSpawn();
		secondDie.PlaceAtSpawn();

		var simulation = await RollingDie.RollPreparedPredeterminedBatchAsync(
			[firstDie, secondDie],
			[firstRequest, secondRequest]);
		await Task.WhenAll(
			ClearAfterRollIfNeededAsync(firstDie),
			ClearAfterRollIfNeededAsync(secondDie));
		_lastSpawnedDie = secondDie;
		var collisionKind = glancing ? "glancing" : "head-on";
		SetStatus($"Forced {collisionKind} d6 pair. {DescribeBatchContacts(simulation)}");
	}

	private static PredeterminedDieThrowRequest ConfigureForcedCollisionRequest(
		PredeterminedDieThrowRequest template,
		System.Numerics.Vector3 position,
		System.Numerics.Quaternion orientation,
		System.Numerics.Vector3 velocity)
	{
		position.Y = DieTossKinematicsBuilder.ComputeOriginHeightForFloorClearance(
			template.CollisionPoints,
			orientation,
			floorHeight: 0f,
			clearance: 0.05f);
		var kinematics = DieTossKinematicsBuilder.Build(
			template.CollisionPoints,
			orientation,
			velocity,
			rollCoupling: 0.99f,
			System.Numerics.Vector3.UnitY,
			tumbleSpeed: 0.6f);
		return template with
		{
			StartPosition = position,
			StartOrientation = orientation,
			LinearVelocity = velocity,
			AngularVelocity = kinematics.AngularVelocity,
		};
	}

	private async Task RerollLastAsync(bool gameplayRoll)
	{
		if (_lastSpawnedDie == null || !GodotObject.IsInstanceValid(_lastSpawnedDie))
		{
			SetStatus("No last die to re-roll.");
			return;
		}

		var face = gameplayRoll ? (int)_targetFaceSpin!.Value : -1;
		await RollWithOptionalClear(_lastSpawnedDie, face);
		var prefix = gameplayRoll ? $"Re-rolled gameplay face {face}." : "Re-rolled free.";
		SetStatus($"{prefix} {DescribeMotion(_lastSpawnedDie)}");
	}

	private void SnapLastInPlace()
	{
		if (_lastSpawnedDie == null || !GodotObject.IsInstanceValid(_lastSpawnedDie))
		{
			SetStatus("No last die to snap.");
			return;
		}

		var face = (int)_targetFaceSpin!.Value;
		var ok = _lastSpawnedDie.TrySnapToFace(face);
		SetStatus(ok ? $"Snapped in place to face {face}." : $"Snap failed for face {face}.");
	}

	private void VerifyLastCalibration()
	{
		if (_lastSpawnedDie == null || !GodotObject.IsInstanceValid(_lastSpawnedDie))
		{
			SetStatus("No last die to verify.");
			return;
		}

		var body = _lastSpawnedDie.Body;
		if (body == null ||
			!_lastSpawnedDie.TryGetEffectiveFaceCalibration(out var effectiveCalibration))
		{
			SetStatus("Last die has no usable calibration/body/hull alignment.");
			return;
		}

		var targetFace = (int)_targetFaceSpin!.Value;
		var bodyQuat = body.GlobalTransform.Basis.GetRotationQuaternion();
		var numericQuat = new System.Numerics.Quaternion(bodyQuat.X, bodyQuat.Y, bodyQuat.Z, bodyQuat.W);
		var target = System.Numerics.Vector3.UnitY;

		var normals = effectiveCalibration.FaceNormals;
		var ranked = DieFaceCalibrationVerifier.RankFaces(normals, numericQuat, target);
		var best = ranked[0];
		var requested = ranked.FirstOrDefault(r => r.Face == targetFace);
		var requestedDot = requested.Face == targetFace ? requested.DotWithTarget : float.NaN;

		SetStatus(
			$"Target face {targetFace} dot(up)={requestedDot:F3} | " +
			$"Best: face {best.Face} dot(up)={best.DotWithTarget:F3} | " +
			$"Top3: {string.Join(", ", ranked.Take(3).Select(r => $"{r.Face}({r.DotWithTarget:F2})"))}");
	}

	private static string DescribeBatchContacts(PredeterminedDiceSimulationResult simulation)
	{
		if (simulation.Trajectories.Count <= 1)
			return "Single-die playback; pair contacts are not applicable.";

		var contacts = simulation.PairContacts;
		if (contacts.Count == 0)
			return "Bepu die contacts: 0; playback=shared-uniform; avoidance=none.";

		var pairs = string.Join(
			", ",
			contacts
				.Select(contact => $"{contact.FirstDieIndex}-{contact.SecondDieIndex}")
				.Distinct());
		var maximumResponse = contacts.Max(contact => System.Numerics.Vector3.Distance(
			contact.FirstVelocityBefore - contact.SecondVelocityBefore,
			contact.FirstVelocityAfter - contact.SecondVelocityAfter));
		return $"Bepu die contacts: {contacts.Sum(contact => contact.ContactCount)} points over " +
			$"{contacts.Count} steps (pairs {pairs}); max approach/response " +
			$"{contacts.Max(contact => contact.ApproachSpeedBefore):F2}/{maximumResponse:F2}; " +
			"playback=shared-uniform; avoidance=none.";
	}

	private static string DescribeMotion(RollingDie die)
	{
		var landing = die.LastRollUsedPredeterminedPlayback
			? $"pre-sim natural {die.LastSimulatedNaturalFace} -> displayed {die.LastDisplayedFace}; " +
			  $"physical {die.LastRollDurationSeconds:F2}s -> visible " +
			  $"{die.LastVisiblePlaybackSeconds:F2}s " +
			  $"({die.LastPlaybackCompressionRatio:F1}x, " +
			  $"{die.LastVisiblePlaybackFrameCount} frames), rotation " +
			  $"{die.LastAccumulatedRotationRadians / MathF.Tau:F1} turns, " +
			  $"face changes {die.LastUpwardFaceTransitions}, " +
			  $"max spin {die.LastMaximumAngularSpeed:F1}, " +
			  $"grip {die.LastTimeToGripSeconds:F2}s, avg/final slip " +
			  $"{die.LastAverageContactSlipSpeed:F2}/{die.LastFinalContactSlipSpeed:F2}, " +
			  $"dot {die.LastDisplayedFaceDot:F3}, settled={die.LastTrajectorySettledNaturally}"
			: $"natural settle in {die.LastRollDurationSeconds:F1}s, max spin {die.LastMaximumAngularSpeed:F1}";
		return $"Launch {die.LastLaunchSpeed:F1}, radius {die.LastEffectiveRollingRadius:F2}, " +
			$"coupling {die.LastRollCoupling:F2}, initial spin {die.LastInitialAngularSpeed:F1}, " +
			$"release slip {die.LastInitialSurfaceSlipSpeed:F2}, calibration/hull " +
			$"{die.LastCalibrationHullAlignmentDot:F3}; {landing}.";
	}
	private RollingDie? CreateDie(TestDiePreset preset)
	{
		if (_spawnRoot == null || RollingDieScene == null || VisualCatalogLibrary == null)
			return null;

		var visual = VisualCatalogLibrary.Resolve(DieRollVisualKind.Player, preset.DieType, preset.Role);
		if (visual == null)
		{
			GD.PushWarning($"No visual for {preset.Label}");
			SetStatus($"No catalog visual for {preset.Label}.");
			return null;
		}

		var die = RollingDieScene.Instantiate<RollingDie>();
		die.SpawnPosition = AllocateSpawnOffset();
		die.SetVisual(visual);
		_spawnRoot.AddChild(die);
		die.PlaceAtSpawn();
		return die;
	}

	private Vector3 AllocateSpawnOffset()
	{
		var existing = new List<System.Numerics.Vector3>();
		if (_spawnRoot != null)
		{
			foreach (var child in _spawnRoot.GetChildren())
			{
				if (child is not RollingDie die || !IsInstanceValid(die))
					continue;
				var position = die.SpawnPosition;
				existing.Add(new System.Numerics.Vector3(position.X, position.Y, position.Z));
			}
		}

		System.Numerics.Vector3 point = default;
		var attempts = Math.Max(1, RandomSpawnAttempts);
		for (var attempt = 0; attempt < attempts; attempt++)
		{
			point = DiceSpawnPositionPicker.Pick(
				GD.Randf(),
				GD.Randf(),
				new System.Numerics.Vector3(
					SpawnBoundsHalfExtents.X,
					SpawnBoundsHalfExtents.Y,
					SpawnBoundsHalfExtents.Z),
				SpawnHeight,
				SpawnEdgeMargin);
			if (DiceSpawnPositionPicker.HasMinimumSeparation(
				point,
				existing,
				MinimumSpawnSeparation))
				break;
		}

		return new Vector3(point.X, point.Y, point.Z);
	}

	private void ClearAll()
	{
		if (_spawnRoot == null)
			return;
		foreach (var child in _spawnRoot.GetChildren())
			child.QueueFree();
		_lastSpawnedDie = null;
		SetStatus("Cleared all dice.");
	}

	private void SetStatus(string message) => _statusLabel?.SetDeferred(Label.PropertyName.Text, message);

	private sealed record TestDiePreset(
		string Label,
		DieType DieType,
		DieVisualRole Role,
		int MinFace,
		int MaxFace);
}
