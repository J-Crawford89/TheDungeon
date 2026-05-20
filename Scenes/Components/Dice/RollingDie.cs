using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Thin wrapper; the visual scene root must be a <see cref="RigidBody3D"/> with mesh, collider, and calibration.</summary>
public partial class RollingDie : Node3D
{
	[ExportGroup("Spawn")]
	[Export] public Vector3 SpawnPosition { get; set; } = new(0, 2.5f, 0);

	[ExportGroup("Toss Impulse")]
	[Export] public float MinHorizontalImpulse { get; set; } = 4f;
	[Export] public float MaxHorizontalImpulse { get; set; } = 6.5f;
	[Export] public float MinUpwardImpulse { get; set; } = 1f;
	[Export] public float MaxUpwardImpulse { get; set; } = 2f;
	[Export] public Vector3 MinInitialRotationDegrees { get; set; } = new(0, 0, 0);
	[Export] public Vector3 MaxInitialRotationDegrees { get; set; } = new(360, 360, 360);

	[ExportGroup("Angular Impulse")]
	[Export] public Vector3 MinTorqueImpulse { get; set; } = new(-25f, -25f, -25f);
	[Export] public Vector3 MaxTorqueImpulse { get; set; } = new(25f, 25f, 25f);

	[ExportGroup("Timing")]
	[Export] public float MaxRollDurationSeconds { get; set; } = 1.8f;
	[Export] public float MinRollDurationSeconds { get; set; } = 0.9f;
	[Export] public float PostSnapDisplaySeconds { get; set; } = 0.5f;
	[Export] public float SettleLinearSpeedThreshold { get; set; } = 0.35f;
	[Export] public float SettleAngularSpeedThreshold { get; set; } = 0.85f;

	private RigidBody3D? _body;
	private DieFaceCalibration? _calibration;

	public RigidBody3D? Body => _body;
	public DieFaceCalibration? Calibration => _calibration;

	public void SetVisual(PackedScene? visualScene)
	{
		ClearVisual();
		if (visualScene == null)
			return;

		var visual = visualScene.Instantiate<Node>();
		if (visual is not RigidBody3D body)
		{
			GD.PushError($"{Name}: visual scene root must be RigidBody3D; got {visual.GetType().Name}.");
			visual.QueueFree();
			return;
		}

		_body = body;
		AddChild(_body);
		_body.Position = Vector3.Zero;
		_body.ContinuousCd = true;
		_calibration = _body.GetNodeOrNull<DieFaceCalibration>(".")
					 ?? _body.FindChild("*", owned: false) as DieFaceCalibration;
		if (_calibration == null)
			GD.PushWarning($"{Name}: visual has no {nameof(DieFaceCalibration)}; forced face snap may fail.");
	}

	public void PlaceAtSpawn()
	{
		Position = SpawnPosition;
		if (_body == null)
			return;
		_body.Freeze = true;
		_body.LinearVelocity = Vector3.Zero;
		_body.AngularVelocity = Vector3.Zero;
	}

	public Task RollAsync() => RollAsync(forcedFaceValue: -1, camera: null, ct: default);

	public async Task RollAsync(int forcedFaceValue, Camera3D? camera, CancellationToken ct = default)
	{
		if (_body == null)
		{
			GD.PushWarning($"{Name}: no visual body; cannot roll.");
			return;
		}

		Position = SpawnPosition;
		_body.Freeze = false;
		_body.Sleeping = false;
		_body.GlobalRotationDegrees = new Vector3(
			(float)GD.RandRange(MinInitialRotationDegrees.X, MaxInitialRotationDegrees.X),
			(float)GD.RandRange(MinInitialRotationDegrees.Y, MaxInitialRotationDegrees.Y),
			(float)GD.RandRange(MinInitialRotationDegrees.Z, MaxInitialRotationDegrees.Z));
		_body.LinearVelocity = Vector3.Zero;
		_body.AngularVelocity = Vector3.Zero;

		ApplyRandomTossImpulse();
		_body.ApplyTorqueImpulse(new Vector3(
			(float)GD.RandRange(MinTorqueImpulse.X, MaxTorqueImpulse.X),
			(float)GD.RandRange(MinTorqueImpulse.Y, MaxTorqueImpulse.Y),
			(float)GD.RandRange(MinTorqueImpulse.Z, MaxTorqueImpulse.Z)));

		var maxDuration = (float)GD.RandRange(MinRollDurationSeconds, MaxRollDurationSeconds);
		await WaitForPhysicsSettleAsync(maxDuration, ct);

		_body.Freeze = true;
		_body.LinearVelocity = Vector3.Zero;
		_body.AngularVelocity = Vector3.Zero;

		if (forcedFaceValue >= 0)
		{
			TrySnapToFace(forcedFaceValue, camera);
			if (PostSnapDisplaySeconds > 0f)
			{
				await ToSignal(GetTree().CreateTimer(PostSnapDisplaySeconds), SceneTreeTimer.SignalName.Timeout);
				ct.ThrowIfCancellationRequested();
			}
		}
	}

	public bool TrySnapToFace(int forcedFaceValue, Camera3D? camera)
	{
		if (_body == null || _calibration == null)
			return false;
		if (camera == null)
			return false;
		if (!_calibration.TryGetFaceOrientation(forcedFaceValue, out _))
			return false;

		_body.Freeze = true;
		_body.LinearVelocity = Vector3.Zero;
		_body.AngularVelocity = Vector3.Zero;

		var spin = (float)GD.RandRange(0d, 360d);
		var currentQuat = _body.GlobalTransform.Basis.GetRotationQuaternion();
		var solved = DieFaceRotationResolver.Resolve(
			_calibration,
			forcedFaceValue,
			currentQuat,
			camera.GlobalPosition,
			_body.GlobalPosition,
			spin);
		_body.GlobalTransform = new Transform3D(new Basis(solved), _body.GlobalPosition);
		return true;
	}

	private void ClearVisual()
	{
		_calibration = null;
		if (_body == null)
			return;
		_body.QueueFree();
		_body = null;
	}

	private void ApplyRandomTossImpulse()
	{
		if (_body == null)
			return;
		var angle = (float)GD.RandRange(0d, Math.Tau);
		var horiz = (float)GD.RandRange(MinHorizontalImpulse, MaxHorizontalImpulse);
		var up = (float)GD.RandRange(MinUpwardImpulse, MaxUpwardImpulse);
		var numeric = DieTossImpulseBuilder.Build(angle, horiz, up);
		_body.ApplyCentralImpulse(new Vector3(numeric.X, numeric.Y, numeric.Z));
	}

	private async Task WaitForPhysicsSettleAsync(float maxSeconds, CancellationToken ct)
	{
		if (_body == null)
			return;
		const float stepSeconds = 0.05f;
		var elapsed = 0f;
		while (elapsed < maxSeconds)
		{
			await ToSignal(GetTree().CreateTimer(stepSeconds), SceneTreeTimer.SignalName.Timeout);
			ct.ThrowIfCancellationRequested();
			elapsed += stepSeconds;
			if (elapsed < MinRollDurationSeconds)
				continue;
			if (_body.LinearVelocity.Length() <= SettleLinearSpeedThreshold
				&& _body.AngularVelocity.Length() <= SettleAngularSpeedThreshold)
				break;
		}
	}
}
