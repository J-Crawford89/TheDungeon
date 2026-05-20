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
	[Export] public Vector3 SpawnOffsetStep { get; set; } = new(1.4f, 0f, 0f);
	[Export] public Vector3 SpawnBoundsHalfExtents { get; set; } = new(7f, 0f, 3.5f);
	[Export] public float SpawnHeight { get; set; } = 2.5f;
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
	private int _spawnSlot;

	private readonly List<TestDiePreset> _presets =
	[
		new("d3", DieType.d3, DieVisualRole.Standard, 1, 6),
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
		for (var i = 0; i < count; i++)
		{
			var die = CreateDie(_selectedPreset);
			if (die == null)
				continue;
			_lastSpawnedDie = die;
			if (rollFree)
				await RollWithOptionalClear(die, -1);
			else if (rollGameplay)
				await RollWithOptionalClear(die, forcedFace);
		}

		if (!rollFree && !rollGameplay)
			SetStatus($"Spawned {count}× {_selectedPreset.Label}.");
		else
			SetStatus($"Rolled {_selectedPreset.Label}.");
	}

	private async Task RollWithOptionalClear(RollingDie die, int forcedFace)
	{
		await die.RollAsync(forcedFace, _camera);
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

		await RollWithOptionalClear(tens, faces.PercentileTensFace);
		_lastSpawnedDie = ones;
		await RollWithOptionalClear(ones, faces.OnesDieFace);
		SetStatus($"d100 total {total} → tens {faces.PercentileTensFace}, ones {faces.OnesDieFace}");
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
		SetStatus(gameplayRoll ? $"Re-rolled gameplay face {face}." : "Re-rolled free.");
	}

	private void SnapLastInPlace()
	{
		if (_lastSpawnedDie == null || !GodotObject.IsInstanceValid(_lastSpawnedDie))
		{
			SetStatus("No last die to snap.");
			return;
		}

		var face = (int)_targetFaceSpin!.Value;
		var ok = _lastSpawnedDie.TrySnapToFace(face, _camera);
		SetStatus(ok ? $"Snapped in place to face {face}." : $"Snap failed for face {face}.");
	}

	private void VerifyLastCalibration()
	{
		if (_lastSpawnedDie == null || !GodotObject.IsInstanceValid(_lastSpawnedDie))
		{
			SetStatus("No last die to verify.");
			return;
		}

		var cal = _lastSpawnedDie.Calibration;
		var body = _lastSpawnedDie.Body;
		if (cal == null || body == null || _camera == null)
		{
			SetStatus("Last die has no calibration/body or camera missing.");
			return;
		}

		var targetFace = (int)_targetFaceSpin!.Value;
		var toCamera = (_camera.GlobalPosition - body.GlobalPosition).Normalized();
		var bodyQuat = body.GlobalTransform.Basis.GetRotationQuaternion();
		var numericQuat = new System.Numerics.Quaternion(bodyQuat.X, bodyQuat.Y, bodyQuat.Z, bodyQuat.W);
		var target = new System.Numerics.Vector3(toCamera.X, toCamera.Y, toCamera.Z);

		var normals = ToNumericNormals(cal.GetLocalFaceNormals());
		var ranked = DieFaceCalibrationVerifier.RankFaces(normals, numericQuat, target);
		var best = ranked[0];
		var requested = ranked.FirstOrDefault(r => r.Face == targetFace);
		var requestedDot = requested.Face == targetFace ? requested.DotWithCamera : float.NaN;

		SetStatus(
			$"Target face {targetFace} dot={requestedDot:F3} | " +
			$"Best: face {best.Face} dot={best.DotWithCamera:F3} | " +
			$"Top3: {string.Join(", ", ranked.Take(3).Select(r => $"{r.Face}({r.DotWithCamera:F2})"))}");
	}

	private static Dictionary<int, System.Numerics.Vector3> ToNumericNormals(IReadOnlyDictionary<int, Vector3> godotNormals)
	{
		var map = new Dictionary<int, System.Numerics.Vector3>(godotNormals.Count);
		foreach (var (face, local) in godotNormals)
			map[face] = new System.Numerics.Vector3(local.X, local.Y, local.Z);
		return map;
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
		var slot = _spawnSlot++;
		const int columns = 6;
		var column = slot % columns;
		var row = slot / columns;
		var centeredColumn = column - (columns - 1) * 0.5f;
		var x = Mathf.Clamp(centeredColumn * SpawnOffsetStep.X, -SpawnBoundsHalfExtents.X, SpawnBoundsHalfExtents.X);
		var z = Mathf.Clamp(row * SpawnOffsetStep.Z, -SpawnBoundsHalfExtents.Z, SpawnBoundsHalfExtents.Z);
		return new Vector3(x, SpawnHeight, z);
	}

	private void ClearAll()
	{
		if (_spawnRoot == null)
			return;
		foreach (var child in _spawnRoot.GetChildren())
			child.QueueFree();
		_lastSpawnedDie = null;
		_spawnSlot = 0;
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
