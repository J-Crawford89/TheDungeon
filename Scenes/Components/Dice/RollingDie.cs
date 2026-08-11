using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Thin wrapper around a typed die visual. Free rolls use live Godot physics. Gameplay rolls
/// pre-simulate a natural trajectory, remap it by a valid die symmetry, and replay it without
/// corrective torque or a final snap.
/// </summary>
public partial class RollingDie : Node3D
{
	[ExportGroup("Spawn")]
	[Export] public Vector3 SpawnPosition { get; set; } = new(0, 1.35f, 0);

	[ExportGroup("Natural Toss")]
	[Export(PropertyHint.Range, "0,30,0.1,or_greater")] public float MinThrowSpeed { get; set; } = 6f;
	[Export(PropertyHint.Range, "0,30,0.1,or_greater")] public float MaxThrowSpeed { get; set; } = 9f;
	[Export(PropertyHint.Range, "0,1.25,0.01")] public float MinRollCoupling { get; set; } = 0.98f;
	[Export(PropertyHint.Range, "0,1.25,0.01")] public float MaxRollCoupling { get; set; } = 1f;
	[Export(PropertyHint.Range, "0,5,0.05")] public float MaxTumbleJitter { get; set; } = 1.25f;

	[ExportGroup("Predetermined Surface")]
	[Export(PropertyHint.Range, "0,2,0.05")] public float SurfaceGrip { get; set; } = 1.25f;

	[ExportGroup("Presentation")]
	[Export] public float PostRollDisplaySeconds { get; set; } = 0.5f;

	public Vector3 SimulationBoundsHalfExtents { get; set; } = new(7f, 0f, 3.5f);

	private const float SimulationFloorHeight = 0f;
	private const float ReleaseFloorClearance = 0.05f;
	private const float SimulationWallHeight = 20f;
	private const float SimulationLinearDamping = 0.18f;
	private const float SimulationAngularDamping = 0.18f;
	private const float SimulationMaximumRollSeconds = 8f;
	private const float SafetyRollTimeoutSeconds = 8f;
	private const float SettleLinearSpeedThreshold = 0.1f;
	private const float SettleAngularSpeedThreshold = 0.2f;
	private const float SettleConfirmationSeconds = 0.25f;
	private const int MinimumPredeterminedUpwardFaceTransitions = 1;
	private const float MinimumPredeterminedLandingFaceDot = 0.95f;
	private const int MaximumTrajectoryAttempts = 4;

	private DieVisualBody? _body;
	private DieFaceCalibration? _calibration;
	private uint _liveCollisionLayer = 1;
	private uint _liveCollisionMask = 1;
	private IReadOnlyDictionary<int, System.Numerics.Vector3>? _effectiveFaceNormals;

	public RigidBody3D? Body => _body;
	public DieFaceCalibration? Calibration => _calibration;
	public bool LastRollUsedPredeterminedPlayback { get; private set; }
	public bool LastTrajectorySettledNaturally { get; private set; }
	public int LastSimulatedNaturalFace { get; private set; } = -1;
	public int LastDisplayedFace { get; private set; } = -1;
	public float LastLaunchSpeed { get; private set; }
	public float LastInitialAngularSpeed { get; private set; }
	public float LastEffectiveRollingRadius { get; private set; }
	public float LastRollCoupling { get; private set; }
	public float LastInitialSurfaceSlipSpeed { get; private set; }
	public float LastMaximumAngularSpeed { get; private set; }
	public float LastAccumulatedRotationRadians { get; private set; }
	public int LastUpwardFaceTransitions { get; private set; }
	public float LastCalibrationHullAlignmentDot { get; private set; }
	public float LastAverageContactSlipSpeed { get; private set; }
	public float LastFinalContactSlipSpeed { get; private set; }
	public float LastTimeToGripSeconds { get; private set; }
	public float LastRollDurationSeconds { get; private set; }
	public float LastDisplayedFaceDot { get; private set; }

	public void SetVisual(PackedScene? visualScene)
	{
		ClearVisual();
		if (visualScene == null)
			return;

		var visual = visualScene.Instantiate<Node>();
		if (visual is not DieVisualBody body)
		{
			GD.PushError($"{Name}: visual scene root must use {nameof(DieVisualBody)}; got {visual.GetType().Name}.");
			visual.QueueFree();
			return;
		}

		_body = body;
		AddChild(_body);
		_body.Position = Vector3.Zero;
		_body.ContinuousCd = true;
		_body.ContactMonitor = true;
		_body.MaxContactsReported = 8;
		_liveCollisionLayer = _body.CollisionLayer;
		_liveCollisionMask = _body.CollisionMask;
		_calibration = _body.Calibration;
		if (_calibration == null)
			GD.PushWarning($"{Name}: assign the visual root's {nameof(DieVisualBody.Calibration)} reference in the Godot editor.");
	}

	public void PlaceAtSpawn()
	{
		Position = SpawnPosition;
		if (_body == null)
			return;
		EnableLiveCollision();
		_body.Freeze = true;
		_body.LinearVelocity = Vector3.Zero;
		_body.AngularVelocity = Vector3.Zero;
		_body.Transform = Transform3D.Identity;
	}

	public Task RollAsync() => RollAsync(forcedFaceValue: -1, ct: default);

	public async Task RollAsync(int forcedFaceValue, CancellationToken ct = default)
	{
		if (_body == null)
		{
			GD.PushWarning($"{Name}: no visual body; cannot roll.");
			return;
		}

		if (forcedFaceValue >= 0 &&
			TryBuildPredeterminedTrajectory(forcedFaceValue, out var trajectory))
		{
			await PlayPredeterminedTrajectoryAsync(trajectory, ct);
			return;
		}

		if (forcedFaceValue >= 0)
			GD.PushWarning($"{Name}: cannot pre-simulate face {forcedFaceValue}; allowing a natural free roll.");
		await RollNaturallyAsync(ct);
	}

	public static async Task RollPredeterminedBatchAsync(
		IReadOnlyList<RollingDie> dice,
		IReadOnlyList<int> faceValues,
		CancellationToken ct = default)
	{
		if (dice == null)
			throw new ArgumentNullException(nameof(dice));
		if (faceValues == null)
			throw new ArgumentNullException(nameof(faceValues));
		if (dice.Count != faceValues.Count)
			throw new ArgumentException("Dice and face-value counts must match.");
		if (dice.Count == 0)
			return;

		IReadOnlyList<PredeterminedDieTrajectory>? trajectories = null;
		for (var attempt = 1; attempt <= MaximumTrajectoryAttempts; attempt++)
		{
			var requests = new PredeterminedDieThrowRequest[dice.Count];
			for (var i = 0; i < dice.Count; i++)
			{
				ct.ThrowIfCancellationRequested();
				if (!dice[i].TryPreparePredeterminedRoll(faceValues[i], out requests[i]))
					throw new InvalidOperationException(
						$"{dice[i].Name}: cannot prepare predetermined face {faceValues[i]}.");
			}

			trajectories = PredeterminedDiceTrajectorySimulator.Simulate(
				requests,
				dice[0].BuildSimulationSettings());
			if (trajectories.All(MeetsPredeterminedMotionQuality))
				break;
			if (attempt == MaximumTrajectoryAttempts)
				GD.PushWarning(
					$"Dice batch did not produce an upward-face transition after {attempt} natural attempts; " +
					"using the final physically valid trajectory.");
		}

		var playbackTasks = new Task[dice.Count];
		for (var i = 0; i < dice.Count; i++)
			playbackTasks[i] = dice[i].PlayPredeterminedTrajectoryAsync(trajectories![i], ct);
		await Task.WhenAll(playbackTasks);
	}

	public bool TryPreparePredeterminedRoll(
		int forcedFaceValue,
		out PredeterminedDieThrowRequest request)
	{
		request = null!;
		if (_body == null || _calibration == null)
			return false;
		if (!_calibration.TryGetFaceOrientation(forcedFaceValue, out _))
			return false;

		ResetRollDiagnostics();
		var startOrientation = CreateRandomInitialOrientation();
		if (!TryGetHullPoints(out var hullPoints))
			return false;
		if (!TryBuildHullAlignedCalibration(hullPoints, out var effectiveCalibration))
			return false;
		_effectiveFaceNormals = effectiveCalibration.FaceNormals;
		LastCalibrationHullAlignmentDot = effectiveCalibration.MinimumSourceAlignmentDot;
		var toss = CreateRandomTossPlan(hullPoints, startOrientation);
		CaptureTossDiagnostics(toss);
		var startPosition = ToNumeric(SpawnPosition);
		startPosition.Y = DieTossKinematicsBuilder.ComputeOriginHeightForFloorClearance(
			hullPoints,
			ToNumeric(startOrientation),
			SimulationFloorHeight,
			ReleaseFloorClearance);
		var faceNormals = effectiveCalibration.FaceNormals;
		var faceUpOrientations = effectiveCalibration.FaceUpOrientations;
		request = new PredeterminedDieThrowRequest(
			hullPoints,
			faceNormals,
			faceUpOrientations,
			forcedFaceValue,
			startPosition,
			ToNumeric(startOrientation),
			ToNumeric(toss.LinearVelocity),
			ToNumeric(toss.AngularVelocity),
			_body.Mass,
			faceNormals.Count == 3 && hullPoints.Count == 8);
		return true;
	}

	public async Task PlayPredeterminedTrajectoryAsync(
		PredeterminedDieTrajectory trajectory,
		CancellationToken ct = default)
	{
		if (_body == null || trajectory.Frames.Count == 0)
			return;

		LastRollUsedPredeterminedPlayback = true;
		LastTrajectorySettledNaturally = trajectory.SettledNaturally;
		LastSimulatedNaturalFace = trajectory.NaturalFace;
		LastDisplayedFace = trajectory.DisplayedFace;
		LastMaximumAngularSpeed = trajectory.MaximumAngularSpeed;
		LastAccumulatedRotationRadians = trajectory.AccumulatedRotationRadians;
		LastUpwardFaceTransitions = trajectory.UpwardFaceTransitions;
		LastAverageContactSlipSpeed = trajectory.AverageContactSlipSpeed;
		LastFinalContactSlipSpeed = trajectory.FinalContactSlipSpeed;
		LastTimeToGripSeconds = trajectory.TimeToGripSeconds;
		LastRollDurationSeconds = trajectory.DurationSeconds;
		Position = SpawnPosition;
		_body.Freeze = true;
		_body.Sleeping = false;
		_body.LinearVelocity = Vector3.Zero;
		_body.AngularVelocity = Vector3.Zero;
		DisablePlaybackCollision();

		ApplyTrajectoryFrame(trajectory.Frames[0]);
		for (var i = 1; i < trajectory.Frames.Count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			ct.ThrowIfCancellationRequested();
			ApplyTrajectoryFrame(trajectory.Frames[i]);
		}

		if (_effectiveFaceNormals != null &&
			_effectiveFaceNormals.TryGetValue(trajectory.DisplayedFace, out var localNormal))
		{
			var bodyOrientation = ToNumeric(
				_body.GlobalTransform.Basis.GetRotationQuaternion());
			LastDisplayedFaceDot = System.Numerics.Vector3.Dot(
				System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Transform(
					localNormal,
					bodyOrientation)),
				System.Numerics.Vector3.UnitY);
		}
		if (!trajectory.SettledNaturally)
			GD.PushWarning($"{Name}: offscreen trajectory reached {trajectory.DurationSeconds:F1}s before settling; no snap was applied.");

		if (PostRollDisplaySeconds > 0f)
		{
			await ToSignal(GetTree().CreateTimer(PostRollDisplaySeconds), SceneTreeTimer.SignalName.Timeout);
			ct.ThrowIfCancellationRequested();
		}
	}

	public bool TrySnapToFace(int forcedFaceValue)
	{
		if (_body == null || _calibration == null)
			return false;
		if (!TryGetEffectiveFaceCalibration(out var effectiveCalibration) ||
			!effectiveCalibration.FaceUpOrientations.TryGetValue(forcedFaceValue, out var faceOrientation))
			return false;

		FreezeBody();
		var currentQuat = _body.GlobalTransform.Basis.GetRotationQuaternion();
		var solved = DieFaceOrientationSolver.SolveNearestFromFaceOrientation(
			faceOrientation,
			ToNumeric(currentQuat),
			System.Numerics.Vector3.UnitY);
		_body.GlobalTransform = new Transform3D(new Basis(ToGodot(solved)), _body.GlobalPosition);
		return true;
	}

	private async Task RollNaturallyAsync(CancellationToken ct)
	{
		if (_body == null)
			return;

		ResetRollDiagnostics();
		var startOrientation = CreateRandomInitialOrientation();
		if (!TryGetHullPoints(out var hullPoints))
			return;
		var toss = CreateRandomTossPlan(hullPoints, startOrientation);
		CaptureTossDiagnostics(toss);
		var releaseHeight = DieTossKinematicsBuilder.ComputeOriginHeightForFloorClearance(
			hullPoints,
			ToNumeric(startOrientation),
			SimulationFloorHeight,
			ReleaseFloorClearance);
		Position = SpawnPosition;
		EnableLiveCollision();
		_body.Freeze = true;
		_body.LockRotation = false;
		_body.Transform = new Transform3D(
			new Basis(startOrientation),
			new Vector3(0f, releaseHeight - SpawnPosition.Y, 0f));
		_body.Sleeping = false;
		_body.LinearVelocity = Vector3.Zero;
		_body.AngularVelocity = Vector3.Zero;
		_body.Freeze = false;
		_body.ApplyCentralImpulse(toss.LinearVelocity * _body.Mass);
		_body.AngularVelocity = toss.AngularVelocity;

		LastRollDurationSeconds = await WaitForNaturalSettleAsync(ct);
		FreezeBody();
	}

	private TossPlan CreateRandomTossPlan(
		IReadOnlyList<System.Numerics.Vector3> hullPoints,
		Quaternion startOrientation)
	{
		var angle = (float)GD.RandRange(0d, Math.Tau);
		var minSpeed = MathF.Max(0f, MathF.Min(MinThrowSpeed, MaxThrowSpeed));
		var maxSpeed = MathF.Max(minSpeed, MathF.Max(MinThrowSpeed, MaxThrowSpeed));
		var throwSpeed = (float)GD.RandRange(minSpeed, maxSpeed);
		var travelDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
		var linearVelocity = travelDirection * throwSpeed;

		var minCoupling = MathF.Max(0f, MathF.Min(MinRollCoupling, MaxRollCoupling));
		var maxCoupling = MathF.Max(minCoupling, MathF.Max(MinRollCoupling, MaxRollCoupling));
		var rollCoupling = (float)GD.RandRange(minCoupling, maxCoupling);
		var tumbleAngle = (float)GD.RandRange(0d, Math.Tau);
		var tumbleAxis = (
			travelDirection * Mathf.Cos(tumbleAngle) +
			Vector3.Up * Mathf.Sin(tumbleAngle)).Normalized();
		var tumbleSpeed = (float)GD.RandRange(0d, MathF.Max(0f, MaxTumbleJitter));
		var kinematics = DieTossKinematicsBuilder.Build(
			hullPoints,
			ToNumeric(startOrientation),
			ToNumeric(linearVelocity),
			rollCoupling,
			ToNumeric(tumbleAxis),
			tumbleSpeed);

		return new TossPlan(
			linearVelocity,
			ToGodot(kinematics.AngularVelocity),
			throwSpeed,
			kinematics.EffectiveRollingRadius,
			rollCoupling,
			kinematics.InitialSurfaceSlipSpeed);
	}

	private static Quaternion CreateRandomInitialOrientation()
	{
		var u1 = GD.Randf();
		var u2 = GD.Randf();
		var u3 = GD.Randf();
		var firstRadius = MathF.Sqrt(1f - u1);
		var secondRadius = MathF.Sqrt(u1);
		var firstAngle = (float)Math.Tau * u2;
		var secondAngle = (float)Math.Tau * u3;
		return new Quaternion(
			firstRadius * MathF.Sin(firstAngle),
			firstRadius * MathF.Cos(firstAngle),
			secondRadius * MathF.Sin(secondAngle),
			secondRadius * MathF.Cos(secondAngle)).Normalized();
	}

	private bool TryGetHullPoints(out IReadOnlyList<System.Numerics.Vector3> hullPoints)
	{
		hullPoints = Array.Empty<System.Numerics.Vector3>();
		if (_body == null)
			return false;
		try
		{
			hullPoints = _body.GetConvexHullPoints();
			return true;
		}
		catch (InvalidOperationException ex)
		{
			GD.PushError(ex.Message);
			return false;
		}
	}

	private void CaptureTossDiagnostics(TossPlan toss)
	{
		LastLaunchSpeed = toss.LaunchSpeed;
		LastInitialAngularSpeed = toss.AngularVelocity.Length();
		LastEffectiveRollingRadius = toss.EffectiveRollingRadius;
		LastRollCoupling = toss.RollCoupling;
		LastInitialSurfaceSlipSpeed = toss.InitialSurfaceSlipSpeed;
	}

	private async Task<float> WaitForNaturalSettleAsync(CancellationToken ct)
	{
		if (_body == null)
			return 0f;

		var tracker = new DieRollSettleTracker(
			SettleLinearSpeedThreshold,
			SettleAngularSpeedThreshold,
			SettleConfirmationSeconds);
		var elapsed = 0f;
		while (elapsed < SafetyRollTimeoutSeconds)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			ct.ThrowIfCancellationRequested();
			var delta = (float)GetPhysicsProcessDeltaTime();
			elapsed += delta;
			LastMaximumAngularSpeed = MathF.Max(
				LastMaximumAngularSpeed,
				_body.AngularVelocity.Length());
			if (tracker.Observe(
				_body.LinearVelocity.Length(),
				_body.AngularVelocity.Length(),
				delta,
				_body.GetContactCount() > 0))
				return elapsed;
		}

		GD.PushWarning($"{Name}: natural free roll did not settle within {SafetyRollTimeoutSeconds:F1}s.");
		return elapsed;
	}

	private PredeterminedDiceSimulationSettings BuildSimulationSettings() => new()
	{
		BoundsHalfExtents = ToNumeric(SimulationBoundsHalfExtents),
		FloorHeight = SimulationFloorHeight,
		WallHeight = SimulationWallHeight,
		Friction = SurfaceGrip,
		LinearDamping = SimulationLinearDamping,
		AngularDamping = SimulationAngularDamping,
		MaximumRollSeconds = SimulationMaximumRollSeconds,
		SettleLinearSpeed = SettleLinearSpeedThreshold,
		SettleAngularSpeed = SettleAngularSpeedThreshold,
		SettleConfirmationSeconds = SettleConfirmationSeconds,
	};

	private void ApplyTrajectoryFrame(PredeterminedDieTrajectoryFrame frame)
	{
		if (_body == null)
			return;
		var localPosition = ToGodot(frame.Position) - SpawnPosition;
		_body.Transform = new Transform3D(
			new Basis(ToGodot(frame.Orientation)),
			localPosition);
	}

	private void ResetRollDiagnostics()
	{
		LastRollUsedPredeterminedPlayback = false;
		LastTrajectorySettledNaturally = false;
		LastSimulatedNaturalFace = -1;
		LastDisplayedFace = -1;
		LastLaunchSpeed = 0f;
		LastInitialAngularSpeed = 0f;
		LastEffectiveRollingRadius = 0f;
		LastRollCoupling = 0f;
		LastInitialSurfaceSlipSpeed = 0f;
		LastMaximumAngularSpeed = 0f;
		LastAccumulatedRotationRadians = 0f;
		LastUpwardFaceTransitions = 0;
		LastCalibrationHullAlignmentDot = 0f;
		LastAverageContactSlipSpeed = 0f;
		LastFinalContactSlipSpeed = 0f;
		LastTimeToGripSeconds = 0f;
		LastRollDurationSeconds = 0f;
		LastDisplayedFaceDot = 0f;
		_effectiveFaceNormals = null;
	}

	public bool TryGetEffectiveFaceCalibration(out DieFaceHullAlignmentResult calibration)
	{
		calibration = null!;
		return TryGetHullPoints(out var hullPoints) &&
			TryBuildHullAlignedCalibration(hullPoints, out calibration);
	}

	private bool TryBuildHullAlignedCalibration(
		IReadOnlyList<System.Numerics.Vector3> hullPoints,
		out DieFaceHullAlignmentResult calibration)
	{
		calibration = null!;
		if (_calibration == null)
			return false;
		try
		{
			calibration = DieFaceHullAlignment.Align(
				hullPoints,
				ToNumericOrientations(_calibration.GetFaceUpOrientations()));
			return true;
		}
		catch (ArgumentException ex)
		{
			GD.PushError($"{Name}: cannot align face calibration to convex hull: {ex.Message}");
			return false;
		}
	}

	private bool TryBuildPredeterminedTrajectory(
		int forcedFaceValue,
		out PredeterminedDieTrajectory trajectory)
	{
		trajectory = null!;
		for (var attempt = 1; attempt <= MaximumTrajectoryAttempts; attempt++)
		{
			if (!TryPreparePredeterminedRoll(forcedFaceValue, out var request))
				return false;
			trajectory = PredeterminedDiceTrajectorySimulator.Simulate(
				[request],
				BuildSimulationSettings())[0];
			if (MeetsPredeterminedMotionQuality(trajectory))
				return true;
		}

		GD.PushWarning(
			$"{Name}: roll did not change its upward face after {MaximumTrajectoryAttempts} natural attempts; " +
			"using the final physically valid trajectory.");
		return true;
	}

	private static bool MeetsPredeterminedMotionQuality(PredeterminedDieTrajectory trajectory) =>
		trajectory.SettledNaturally &&
		trajectory.FinalUpwardFaceDot >= MinimumPredeterminedLandingFaceDot &&
		trajectory.UpwardFaceTransitions >= MinimumPredeterminedUpwardFaceTransitions;

	private void EnableLiveCollision()
	{
		if (_body == null)
			return;
		_body.CollisionLayer = _liveCollisionLayer;
		_body.CollisionMask = _liveCollisionMask;
	}

	private void DisablePlaybackCollision()
	{
		if (_body == null)
			return;
		_body.CollisionLayer = 0;
		_body.CollisionMask = 0;
	}

	private void FreezeBody()
	{
		if (_body == null)
			return;
		_body.Freeze = true;
		_body.LinearVelocity = Vector3.Zero;
		_body.AngularVelocity = Vector3.Zero;
	}

	private void ClearVisual()
	{
		_calibration = null;
		if (_body == null)
			return;
		_body.QueueFree();
		_body = null;
	}

	private static Dictionary<int, System.Numerics.Quaternion> ToNumericOrientations(
		IReadOnlyDictionary<int, Quaternion> godotOrientations)
	{
		var result = new Dictionary<int, System.Numerics.Quaternion>(godotOrientations.Count);
		foreach (var (face, orientation) in godotOrientations)
			result[face] = ToNumeric(orientation);
		return result;
	}

	private static System.Numerics.Vector3 ToNumeric(Vector3 value) =>
		new(value.X, value.Y, value.Z);

	private static System.Numerics.Quaternion ToNumeric(Quaternion value) =>
		new(value.X, value.Y, value.Z, value.W);

	private static Vector3 ToGodot(System.Numerics.Vector3 value) =>
		new(value.X, value.Y, value.Z);

	private static Quaternion ToGodot(System.Numerics.Quaternion value) =>
		new(value.X, value.Y, value.Z, value.W);

	private readonly record struct TossPlan(
		Vector3 LinearVelocity,
		Vector3 AngularVelocity,
		float LaunchSpeed,
		float EffectiveRollingRadius,
		float RollCoupling,
		float InitialSurfaceSlipSpeed);
}