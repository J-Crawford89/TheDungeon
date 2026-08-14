using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

public sealed record PredeterminedDieThrowRequest(
	IReadOnlyList<Vector3> CollisionPoints,
	IReadOnlyDictionary<int, Vector3> FaceNormals,
	IReadOnlyDictionary<int, Quaternion> FaceUpOrientations,
	int DesiredFace,
	Vector3 StartPosition,
	Quaternion StartOrientation,
	Vector3 LinearVelocity,
	Vector3 AngularVelocity,
	float Mass,
	bool OppositeFacesShareValues = false);

public readonly record struct PredeterminedDieTrajectoryFrame(
	Vector3 Position,
	Quaternion Orientation);

public sealed record PredeterminedDieTrajectory(
	IReadOnlyList<PredeterminedDieTrajectoryFrame> Frames,
	int NaturalFace,
	int DisplayedFace,
	float DurationSeconds,
	float MaximumAngularSpeed,
	float AccumulatedRotationRadians,
	int UpwardFaceTransitions,
	float FinalUpwardFaceDot,
	bool SettledNaturally,
	float AverageContactSlipSpeed,
	float FinalContactSlipSpeed,
	float TimeToGripSeconds)
{
	public Quaternion DisplayOffset { get; init; } = Quaternion.Identity;
}

public sealed record PredeterminedDicePairContact(
	int FirstDieIndex,
	int SecondDieIndex,
	int SimulationStep,
	float TimeSeconds,
	int ContactCount,
	float MaximumDepth,
	Vector3 NormalTowardFirstDie,
	Vector3 FirstVelocityBefore,
	Vector3 SecondVelocityBefore,
	Vector3 FirstVelocityAfter,
	Vector3 SecondVelocityAfter)
{
	public float ApproachSpeedBefore => MathF.Max(
		0f,
		-Vector3.Dot(FirstVelocityBefore - SecondVelocityBefore, NormalTowardFirstDie));

	public float SeparationSpeedAfter => MathF.Max(
		0f,
		Vector3.Dot(FirstVelocityAfter - SecondVelocityAfter, NormalTowardFirstDie));
}

public sealed record PredeterminedDiceSimulationResult(
	IReadOnlyList<PredeterminedDieTrajectory> Trajectories,
	IReadOnlyList<PredeterminedDicePairContact> PairContacts,
	int ManifoldCallbackCount,
	int DynamicPairManifoldCount,
	int IdentifiedDiePairManifoldCount)
{
	public static PredeterminedDiceSimulationResult Empty { get; } = new(
		Array.Empty<PredeterminedDieTrajectory>(),
		Array.Empty<PredeterminedDicePairContact>(),
		0,
		0,
		0);
}

public sealed record PredeterminedDiceSimulationSettings
{
	public Vector3 BoundsHalfExtents { get; init; } = new(7f, 0f, 3.5f);
	public float FloorHeight { get; init; }
	public float WallHeight { get; init; } = 20f;
	public float WallThickness { get; init; } = 0.5f;
	public Vector3 Gravity { get; init; } = new(0f, -9.8f, 0f);
	public float LinearDamping { get; init; } = 0.18f;
	public float AngularDamping { get; init; } = 0.18f;
	public float Friction { get; init; } = 1.25f;
	public float WallFriction { get; init; } = 0.15f;
	public float ContactSpringFrequency { get; init; } = 30f;
	public float ContactDampingRatio { get; init; } = 0.35f;
	public float MaximumRecoveryVelocity { get; init; } = 5f;
	public float TimeStepSeconds { get; init; } = 1f / 60f;
	public float MinimumRollSeconds { get; init; } = 0.45f;
	public float MaximumRollSeconds { get; init; } = 8f;
	public float SettleLinearSpeed { get; init; } = 0.08f;
	public float SettleAngularSpeed { get; init; } = 0.15f;
	public float SettleConfirmationSeconds { get; init; } = 0.2f;
	public float MinimumLandingFaceDot { get; init; } = 0.95f;
}

/// <summary>
/// Runs dice in a manually stepped, invisible physics world, records their natural motion,
/// then applies a rotational symmetry before playback so the requested labeled face follows
/// the same trajectory as the naturally landed face. No corrective torque is used.
/// </summary>
public static class PredeterminedDiceTrajectorySimulator
{
	public static IReadOnlyList<PredeterminedDieTrajectory> Simulate(
		IReadOnlyList<PredeterminedDieThrowRequest> requests,
		PredeterminedDiceSimulationSettings? settings = null) =>
		SimulateWithDiagnostics(requests, settings).Trajectories;

	public static PredeterminedDiceSimulationResult SimulateWithDiagnostics(
		IReadOnlyList<PredeterminedDieThrowRequest> requests,
		PredeterminedDiceSimulationSettings? settings = null)
	{
		if (requests == null)
			throw new ArgumentNullException(nameof(requests));
		if (requests.Count == 0)
			return PredeterminedDiceSimulationResult.Empty;

		settings ??= new PredeterminedDiceSimulationSettings();
		ValidateSettings(settings);
		foreach (var request in requests)
			ValidateRequest(request);

		var pool = new BufferPool();
		var materials = new CollidableProperty<DiceContactMaterial>();
		var pairContactCollector = new DicePairContactCollector(requests.Count);
		var simulation = Simulation.Create(
			pool,
			new DiceNarrowPhaseCallbacks(
				materials,
				pairContactCollector,
				settings.MaximumRecoveryVelocity,
				new SpringSettings(
					settings.ContactSpringFrequency,
					settings.ContactDampingRatio)),
			new DicePoseIntegratorCallbacks(
				settings.Gravity,
				settings.LinearDamping,
				settings.AngularDamping),
			new SolveDescription(4, 4));
		simulation.Deterministic = true;

		try
		{
			AddTray(simulation, settings, materials);
			var bodies = new SimulatedBody[requests.Count];
			var recordedFrames = new List<PredeterminedDieTrajectoryFrame>[requests.Count];
			var maximumAngularSpeeds = new float[requests.Count];
			var surfaceGripTrackers = new SurfaceGripTracker[requests.Count];

			for (var i = 0; i < requests.Count; i++)
			{
				bodies[i] = AddDie(simulation, pool, requests[i], i, materials);
				recordedFrames[i] = new List<PredeterminedDieTrajectoryFrame>(256);
				RecordFrame(
					simulation,
					bodies[i],
					requests[i],
					settings,
					0f,
					recordedFrames[i],
					ref maximumAngularSpeeds[i],
					ref surfaceGripTrackers[i]);
			}

			var maximumSteps = (int)MathF.Ceiling(settings.MaximumRollSeconds / settings.TimeStepSeconds);
			var stableSeconds = 0f;
			var settledNaturally = false;
			for (var step = 1; step <= maximumSteps; step++)
			{
				pairContactCollector.BeginStep(step, simulation, bodies);
				simulation.Timestep(settings.TimeStepSeconds);
				var allSlow = true;
				pairContactCollector.CompleteStep(
					settings.TimeStepSeconds,
					simulation,
					bodies);
				for (var i = 0; i < bodies.Length; i++)
				{
					RecordFrame(
						simulation,
						bodies[i],
						requests[i],
						settings,
						step * settings.TimeStepSeconds,
						recordedFrames[i],
						ref maximumAngularSpeeds[i],
						ref surfaceGripTrackers[i]);
					var body = simulation.Bodies.GetBodyReference(bodies[i].Handle);
					if (body.Velocity.Linear.Length() > settings.SettleLinearSpeed ||
						body.Velocity.Angular.Length() > settings.SettleAngularSpeed ||
						FindNaturalLanding(
							requests[i],
							Quaternion.Normalize(body.Pose.Orientation)).Dot < settings.MinimumLandingFaceDot)
						allSlow = false;
				}

				var elapsed = step * settings.TimeStepSeconds;
				stableSeconds = elapsed >= settings.MinimumRollSeconds && allSlow
					? stableSeconds + settings.TimeStepSeconds
					: 0f;
				if (stableSeconds >= settings.SettleConfirmationSeconds)
				{
					settledNaturally = true;
					break;
				}
			}

			var results = new PredeterminedDieTrajectory[requests.Count];
			for (var i = 0; i < requests.Count; i++)
			{
				var rawFrames = recordedFrames[i];
				var naturalLanding = FindNaturalLanding(
					requests[i],
					rawFrames[^1].Orientation);
				var displayOffset = ComputeDisplayOffset(
					requests[i].FaceUpOrientations,
					requests[i].DesiredFace,
					naturalLanding.FaceUpOrientation);
				var displayFrames = new PredeterminedDieTrajectoryFrame[rawFrames.Count];
				for (var frameIndex = 0; frameIndex < rawFrames.Count; frameIndex++)
				{
					var frame = rawFrames[frameIndex];
					displayFrames[frameIndex] = frame with
					{
						Orientation = Quaternion.Normalize(frame.Orientation * displayOffset),
					};
				}

				results[i] = new PredeterminedDieTrajectory(
					displayFrames,
					naturalLanding.Face,
					requests[i].DesiredFace,
					MathF.Max(0f, displayFrames.Length - 1) * settings.TimeStepSeconds,
					maximumAngularSpeeds[i],
					MeasureAccumulatedRotation(rawFrames),
					CountUpwardFaceTransitions(requests[i], rawFrames),
					naturalLanding.Dot,
					settledNaturally,
					surfaceGripTrackers[i].AverageSlipSpeed,
					surfaceGripTrackers[i].FinalSlipSpeed,
					surfaceGripTrackers[i].GetTimeToGrip(
						MathF.Max(0f, displayFrames.Length - 1) * settings.TimeStepSeconds))
				{
					DisplayOffset = displayOffset,
				};
			}
			return new PredeterminedDiceSimulationResult(
				results,
				pairContactCollector.Contacts,
				pairContactCollector.ManifoldCallbackCount,
				pairContactCollector.DynamicPairManifoldCount,
				pairContactCollector.IdentifiedDiePairManifoldCount);
		}
		finally
		{
			materials.Dispose();
			simulation.Dispose();
			pool.Clear();
		}
	}

	private static float MeasureAccumulatedRotation(
		IReadOnlyList<PredeterminedDieTrajectoryFrame> frames)
	{
		var radians = 0f;
		for (var i = 1; i < frames.Count; i++)
		{
			var dot = Math.Clamp(
				MathF.Abs(Quaternion.Dot(frames[i - 1].Orientation, frames[i].Orientation)),
				0f,
				1f);
			radians += 2f * MathF.Acos(dot);
		}

		return radians;
	}

	private static int CountUpwardFaceTransitions(
		PredeterminedDieThrowRequest request,
		IReadOnlyList<PredeterminedDieTrajectoryFrame> frames)
	{
		var transitions = 0;
		var previousFace = int.MinValue;
		for (var frameIndex = 0; frameIndex < frames.Count; frameIndex++)
		{
			var bestFace = int.MinValue;
			var bestDot = float.NegativeInfinity;
			foreach (var (face, normal) in request.FaceNormals)
			{
				var dot = Vector3.Dot(
					Vector3.Transform(normal, frames[frameIndex].Orientation),
					Vector3.UnitY);
				if (dot > bestDot)
				{
					bestDot = dot;
					bestFace = face * 2;
				}
				if (request.OppositeFacesShareValues && -dot > bestDot)
				{
					bestDot = -dot;
					bestFace = face * 2 + 1;
				}
			}

			if (previousFace != int.MinValue && bestFace != previousFace)
				transitions++;
			previousFace = bestFace;
		}

		return transitions;
	}
	private static SimulatedBody AddDie(
		Simulation simulation,
		BufferPool pool,
		PredeterminedDieThrowRequest request,
		int dieIndex,
		CollidableProperty<DiceContactMaterial> materials)
	{
		var points = new QuickList<Vector3>(request.CollisionPoints.Count, pool);
		for (var i = 0; i < request.CollisionPoints.Count; i++)
			points.AllocateUnsafely() = request.CollisionPoints[i];
		ConvexHullHelper.CreateShape(
			points.Span.Slice(points.Count),
			pool,
			out var center,
			out var hull);
		points.Dispose(pool);

		var orientation = Quaternion.Normalize(request.StartOrientation);
		var centerPosition = request.StartPosition + Vector3.Transform(center, orientation);
		var shapeIndex = simulation.Shapes.Add(hull);
		var description = BodyDescription.CreateDynamic(
			new RigidPose(centerPosition, orientation),
			new BodyVelocity(request.LinearVelocity, request.AngularVelocity),
			hull.ComputeInertia(request.Mass),
			new CollidableDescription(shapeIndex, 0.1f),
			new BodyActivityDescription(-1f, byte.MaxValue));
		var handle = simulation.Bodies.Add(description);
		materials.Allocate(handle) = new DiceContactMaterial(1f, dieIndex);
		return new SimulatedBody(handle, center);
	}

	private static void AddTray(
		Simulation simulation,
		PredeterminedDiceSimulationSettings settings,
		CollidableProperty<DiceContactMaterial> materials)
	{
		var thickness = settings.WallThickness;
		var half = settings.BoundsHalfExtents;
		var floorShape = simulation.Shapes.Add(new Box(
			half.X * 2f + thickness * 2f,
			thickness,
			half.Z * 2f + thickness * 2f));
		var floorHandle = simulation.Statics.Add(new StaticDescription(
			new Vector3(0f, settings.FloorHeight - thickness * 0.5f, 0f),
			floorShape));
		materials.Allocate(floorHandle) = new DiceContactMaterial(settings.Friction);

		var wallCenterY = settings.FloorHeight + settings.WallHeight * 0.5f;
		var xWall = simulation.Shapes.Add(new Box(
			thickness,
			settings.WallHeight,
			half.Z * 2f + thickness * 2f));
		var zWall = simulation.Shapes.Add(new Box(
			half.X * 2f + thickness * 2f,
			settings.WallHeight,
			thickness));
		var leftWall = simulation.Statics.Add(new StaticDescription(
			new Vector3(-half.X - thickness * 0.5f, wallCenterY, 0f), xWall));
		var rightWall = simulation.Statics.Add(new StaticDescription(
			new Vector3(half.X + thickness * 0.5f, wallCenterY, 0f), xWall));
		var nearWall = simulation.Statics.Add(new StaticDescription(
			new Vector3(0f, wallCenterY, -half.Z - thickness * 0.5f), zWall));
		var farWall = simulation.Statics.Add(new StaticDescription(
			new Vector3(0f, wallCenterY, half.Z + thickness * 0.5f), zWall));
		var wallMaterial = new DiceContactMaterial(settings.WallFriction);
		materials.Allocate(leftWall) = wallMaterial;
		materials.Allocate(rightWall) = wallMaterial;
		materials.Allocate(nearWall) = wallMaterial;
		materials.Allocate(farWall) = wallMaterial;
	}

	private static void RecordFrame(
		Simulation simulation,
		SimulatedBody simulatedBody,
		PredeterminedDieThrowRequest request,
		PredeterminedDiceSimulationSettings settings,
		float elapsedSeconds,
		List<PredeterminedDieTrajectoryFrame> frames,
		ref float maximumAngularSpeed,
		ref SurfaceGripTracker surfaceGripTracker)
	{
		var body = simulation.Bodies.GetBodyReference(simulatedBody.Handle);
		var orientation = Quaternion.Normalize(body.Pose.Orientation);
		var originPosition = body.Pose.Position -
			Vector3.Transform(simulatedBody.HullCenter, orientation);
		frames.Add(new PredeterminedDieTrajectoryFrame(originPosition, orientation));
		maximumAngularSpeed = MathF.Max(maximumAngularSpeed, body.Velocity.Angular.Length());
		surfaceGripTracker.Observe(
			request.CollisionPoints,
			originPosition,
			body.Pose.Position,
			orientation,
			body.Velocity.Linear,
			body.Velocity.Angular,
			settings.FloorHeight,
			elapsedSeconds,
			settings.TimeStepSeconds);
	}

	private static NaturalLanding FindNaturalLanding(
		PredeterminedDieThrowRequest request,
		Quaternion orientation)
	{
		var bestFace = -1;
		var bestDot = float.NegativeInfinity;
		var bestFaceUp = Quaternion.Identity;
		foreach (var (face, normal) in request.FaceNormals)
		{
			var dot = Vector3.Dot(Vector3.Transform(normal, orientation), Vector3.UnitY);
			if (dot > bestDot)
			{
				bestDot = dot;
				bestFace = face;
				bestFaceUp = request.FaceUpOrientations[face];
			}

			if (request.OppositeFacesShareValues && -dot > bestDot)
			{
				bestDot = -dot;
				bestFace = face;
				bestFaceUp = CreateOppositeFaceUpOrientation(
					face,
					normal,
					request.FaceNormals,
					request.FaceUpOrientations[face]);
			}
		}
		return new NaturalLanding(bestFace, Quaternion.Normalize(bestFaceUp), bestDot);
	}

	private static Quaternion CreateOppositeFaceUpOrientation(
		int face,
		Vector3 normal,
		IReadOnlyDictionary<int, Vector3> faceNormals,
		Quaternion canonicalFaceUp)
	{
		var perpendicular = Vector3.Zero;
		var smallestAbsoluteDot = float.PositiveInfinity;
		foreach (var (otherFace, otherNormal) in faceNormals)
		{
			if (otherFace == face)
				continue;
			var absoluteDot = MathF.Abs(Vector3.Dot(normal, otherNormal));
			if (absoluteDot < smallestAbsoluteDot)
			{
				smallestAbsoluteDot = absoluteDot;
				perpendicular = otherNormal;
			}
		}

		perpendicular -= normal * Vector3.Dot(perpendicular, normal);
		if (perpendicular.LengthSquared() < 0.0001f)
		{
			var fallback = MathF.Abs(normal.X) < 0.9f ? Vector3.UnitX : Vector3.UnitY;
			perpendicular = Vector3.Normalize(Vector3.Cross(normal, fallback));
		}
		else
			perpendicular = Vector3.Normalize(perpendicular);
		var localHalfTurn = Quaternion.CreateFromAxisAngle(perpendicular, MathF.PI);
		return Quaternion.Normalize(canonicalFaceUp * localHalfTurn);
	}

	private static Quaternion ComputeDisplayOffset(
		IReadOnlyDictionary<int, Quaternion> faceUpOrientations,
		int desiredFace,
		Quaternion naturalFaceUp)
	{
		var desired = Quaternion.Normalize(faceUpOrientations[desiredFace]);
		var natural = Quaternion.Normalize(naturalFaceUp);
		return Quaternion.Normalize(Quaternion.Inverse(natural) * desired);
	}

	private static void ValidateRequest(PredeterminedDieThrowRequest request)
	{
		if (request.CollisionPoints == null || request.CollisionPoints.Count < 4)
			throw new ArgumentException("A die requires at least four convex-hull points.", nameof(request));
		if (request.FaceNormals == null || request.FaceNormals.Count == 0)
			throw new ArgumentException("A die requires calibrated face normals.", nameof(request));
		if (request.FaceUpOrientations == null ||
			!request.FaceUpOrientations.ContainsKey(request.DesiredFace))
			throw new ArgumentException($"No face-up orientation for desired face {request.DesiredFace}.", nameof(request));
		if (request.Mass <= 0f)
			throw new ArgumentOutOfRangeException(nameof(request), "Die mass must be positive.");
	}

	private static void ValidateSettings(PredeterminedDiceSimulationSettings settings)
	{
		if (settings.BoundsHalfExtents.X <= 0f || settings.BoundsHalfExtents.Z <= 0f)
			throw new ArgumentOutOfRangeException(nameof(settings), "Simulation bounds must be positive.");
		if (settings.WallHeight <= 0f || settings.WallThickness <= 0f)
			throw new ArgumentOutOfRangeException(nameof(settings), "Wall dimensions must be positive.");
		if (settings.TimeStepSeconds <= 0f || settings.MaximumRollSeconds <= 0f)
			throw new ArgumentOutOfRangeException(nameof(settings), "Simulation durations must be positive.");
		if (settings.Friction < 0f || settings.WallFriction < 0f)
			throw new ArgumentOutOfRangeException(nameof(settings), "Friction values cannot be negative.");
		if (settings.MinimumLandingFaceDot < -1f || settings.MinimumLandingFaceDot > 1f)
			throw new ArgumentOutOfRangeException(nameof(settings), "Landing face dot must be within [-1, 1].");
	}

	private readonly record struct SimulatedBody(BodyHandle Handle, Vector3 HullCenter);

	private sealed class DicePairContactCollector
	{
		private readonly Dictionary<long, ContactAccumulator> _stepContacts = new();
		private readonly List<PredeterminedDicePairContact> _contacts = new();
		private readonly Vector3[] _velocitiesBefore;
		private int _currentStep;

		public DicePairContactCollector(int dieCount)
		{
			_velocitiesBefore = new Vector3[dieCount];
		}

		public IReadOnlyList<PredeterminedDicePairContact> Contacts => _contacts;
		public int ManifoldCallbackCount { get; private set; }
		public int DynamicPairManifoldCount { get; private set; }
		public int IdentifiedDiePairManifoldCount { get; private set; }

		public void BeginStep(
			int simulationStep,
			Simulation simulation,
			IReadOnlyList<SimulatedBody> bodies)
		{
			_currentStep = simulationStep;
			_stepContacts.Clear();
			for (var i = 0; i < bodies.Count; i++)
				_velocitiesBefore[i] = simulation.Bodies
					.GetBodyReference(bodies[i].Handle)
					.Velocity.Linear;
		}

		public void Record<TManifold>(
			CollidablePair pair,
			ref TManifold manifold,
			int dieA,
			int dieB)
			where TManifold : unmanaged, IContactManifold<TManifold>
		{
			ManifoldCallbackCount++;
			if (pair.A.Mobility != CollidableMobility.Dynamic ||
				pair.B.Mobility != CollidableMobility.Dynamic)
				return;

			DynamicPairManifoldCount++;
			if (dieA < 0 || dieB < 0)
				return;

			IdentifiedDiePairManifoldCount++;
			if (manifold.Count == 0 || dieA == dieB)
				return;

			var firstDie = Math.Min(dieA, dieB);
			var secondDie = Math.Max(dieA, dieB);
			var key = ((long)firstDie << 32) | (uint)secondDie;
			if (!_stepContacts.TryGetValue(key, out var accumulator))
			{
				accumulator = new ContactAccumulator(firstDie, secondDie);
				_stepContacts.Add(key, accumulator);
			}

			for (var contactIndex = 0; contactIndex < manifold.Count; contactIndex++)
			{
				manifold.GetContact(
					contactIndex,
					out _,
					out var normal,
					out var depth,
					out _);
				if (dieA != firstDie)
					normal = -normal;
				accumulator.Add(normal, depth);
			}
		}

		public void CompleteStep(
			float timeStepSeconds,
			Simulation simulation,
			IReadOnlyList<SimulatedBody> bodies)
		{
			foreach (var accumulator in _stepContacts.Values)
			{
				var firstAfter = simulation.Bodies
					.GetBodyReference(bodies[accumulator.FirstDieIndex].Handle)
					.Velocity.Linear;
				var secondAfter = simulation.Bodies
					.GetBodyReference(bodies[accumulator.SecondDieIndex].Handle)
					.Velocity.Linear;
				_contacts.Add(new PredeterminedDicePairContact(
					accumulator.FirstDieIndex,
					accumulator.SecondDieIndex,
					_currentStep,
					_currentStep * timeStepSeconds,
					accumulator.ContactCount,
					accumulator.MaximumDepth,
					accumulator.Normal,
					_velocitiesBefore[accumulator.FirstDieIndex],
					_velocitiesBefore[accumulator.SecondDieIndex],
					firstAfter,
					secondAfter));
			}
		}

		private sealed class ContactAccumulator
		{
			private Vector3 _normalAtMaximumDepth;

			public ContactAccumulator(int firstDieIndex, int secondDieIndex)
			{
				FirstDieIndex = firstDieIndex;
				SecondDieIndex = secondDieIndex;
				MaximumDepth = float.NegativeInfinity;
			}

			public int FirstDieIndex { get; }
			public int SecondDieIndex { get; }
			public int ContactCount { get; private set; }
			public float MaximumDepth { get; private set; }
			public Vector3 Normal => Vector3.Normalize(_normalAtMaximumDepth);

			public void Add(Vector3 normal, float depth)
			{
				ContactCount++;
				if (depth <= MaximumDepth)
					return;
				MaximumDepth = depth;
				_normalAtMaximumDepth = normal;
			}
		}
	}

	private readonly record struct NaturalLanding(int Face, Quaternion FaceUpOrientation, float Dot);
	private readonly record struct DiceContactMaterial(float Friction, int DieIndex = -1);

	private struct SurfaceGripTracker
	{
		private const float FloorContactTolerance = 0.08f;
		private const float ContactPointHeightTolerance = 0.01f;
		private const float AbsoluteGripSlipSpeed = 0.4f;
		private const float RelativeGripSlip = 0.18f;
		private const float GripConfirmationSeconds = 0.05f;

		private float _accumulatedSlipSpeed;
		private int _contactSamples;
		private float _contactTimeSeconds;
		private float _gripConfirmationSeconds;
		private bool _gripped;
		private float _timeToGripSeconds;

		public readonly float AverageSlipSpeed =>
			_contactSamples == 0 ? 0f : _accumulatedSlipSpeed / _contactSamples;
		public float FinalSlipSpeed { get; private set; }

		public readonly float GetTimeToGrip(float durationSeconds) =>
			_gripped ? _timeToGripSeconds : durationSeconds;

		public void Observe(
			IReadOnlyList<Vector3> hullPoints,
			Vector3 originPosition,
			Vector3 centerPosition,
			Quaternion orientation,
			Vector3 linearVelocity,
			Vector3 angularVelocity,
			float floorHeight,
			float elapsedSeconds,
			float deltaSeconds)
		{
			var lowestY = float.PositiveInfinity;
			for (var i = 0; i < hullPoints.Count; i++)
			{
				var worldPoint = originPosition + Vector3.Transform(hullPoints[i], orientation);
				lowestY = MathF.Min(lowestY, worldPoint.Y);
			}

			if (lowestY > floorHeight + FloorContactTolerance)
			{
				_gripConfirmationSeconds = 0f;
				return;
			}

			var slipSpeed = float.PositiveInfinity;
			for (var i = 0; i < hullPoints.Count; i++)
			{
				var worldPoint = originPosition + Vector3.Transform(hullPoints[i], orientation);
				if (worldPoint.Y > lowestY + ContactPointHeightTolerance)
					continue;
				var pointVelocity = linearVelocity +
					Vector3.Cross(angularVelocity, worldPoint - centerPosition);
				var tangentSpeed = MathF.Sqrt(
					pointVelocity.X * pointVelocity.X +
					pointVelocity.Z * pointVelocity.Z);
				slipSpeed = MathF.Min(slipSpeed, tangentSpeed);
			}

			if (!float.IsFinite(slipSpeed))
				return;
			_contactTimeSeconds += deltaSeconds;
			_accumulatedSlipSpeed += slipSpeed;
			_contactSamples++;
			FinalSlipSpeed = slipSpeed;

			var horizontalSpeed = MathF.Sqrt(
				linearVelocity.X * linearVelocity.X +
				linearVelocity.Z * linearVelocity.Z);
			var gripThreshold = MathF.Max(
				AbsoluteGripSlipSpeed,
				horizontalSpeed * RelativeGripSlip);
			if (slipSpeed <= gripThreshold)
			{
				_gripConfirmationSeconds += deltaSeconds;
				if (!_gripped && _gripConfirmationSeconds >= GripConfirmationSeconds)
				{
					_gripped = true;
					_timeToGripSeconds = _contactTimeSeconds;
				}
			}
			else
				_gripConfirmationSeconds = 0f;
		}
	}

	private unsafe struct DiceNarrowPhaseCallbacks : INarrowPhaseCallbacks
	{
		private readonly CollidableProperty<DiceContactMaterial> _materials;
		private readonly DicePairContactCollector _pairContactCollector;
		private readonly float _maximumRecoveryVelocity;
		private readonly SpringSettings _springSettings;

		public DiceNarrowPhaseCallbacks(
			CollidableProperty<DiceContactMaterial> materials,
			DicePairContactCollector pairContactCollector,
			float maximumRecoveryVelocity,
			SpringSettings springSettings)
		{
			_materials = materials;
			_pairContactCollector = pairContactCollector;
			_maximumRecoveryVelocity = maximumRecoveryVelocity;
			_springSettings = springSettings;
		}

		public void Initialize(Simulation simulation) => _materials.Initialize(simulation);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool AllowContactGeneration(
			int workerIndex,
			CollidableReference a,
			CollidableReference b,
			ref float speculativeMargin) =>
			a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool AllowContactGeneration(
			int workerIndex,
			CollidablePair pair,
			int childIndexA,
			int childIndexB) => true;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ConfigureContactManifold<TManifold>(
			int workerIndex,
			CollidablePair pair,
			ref TManifold manifold,
			out PairMaterialProperties pairMaterial)
			where TManifold : unmanaged, IContactManifold<TManifold>
		{
			if (manifold.Convex)
			{
				_pairContactCollector.Record(
					pair,
					ref manifold,
					_materials[pair.A].DieIndex,
					_materials[pair.B].DieIndex);
			}
			pairMaterial.FrictionCoefficient =
				_materials[pair.A].Friction * _materials[pair.B].Friction;
			pairMaterial.MaximumRecoveryVelocity = _maximumRecoveryVelocity;
			pairMaterial.SpringSettings = _springSettings;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ConfigureContactManifold(
			int workerIndex,
			CollidablePair pair,
			int childIndexA,
			int childIndexB,
			ref ConvexContactManifold manifold)
		{
			_pairContactCollector.Record(
				pair,
				ref manifold,
				_materials[pair.A].DieIndex,
				_materials[pair.B].DieIndex);
			return true;
		}

		public void Dispose() { }
	}

	private struct DicePoseIntegratorCallbacks : IPoseIntegratorCallbacks
	{
		private readonly Vector3 _gravity;
		private readonly float _linearDamping;
		private readonly float _angularDamping;
		private Vector3Wide _gravityDt;
		private Vector<float> _linearDampingDt;
		private Vector<float> _angularDampingDt;

		public DicePoseIntegratorCallbacks(
			Vector3 gravity,
			float linearDamping,
			float angularDamping)
		{
			_gravity = gravity;
			_linearDamping = linearDamping;
			_angularDamping = angularDamping;
			_gravityDt = default;
			_linearDampingDt = default;
			_angularDampingDt = default;
		}

		public readonly AngularIntegrationMode AngularIntegrationMode =>
			AngularIntegrationMode.Nonconserving;
		public readonly bool AllowSubstepsForUnconstrainedBodies => false;
		public readonly bool IntegrateVelocityForKinematics => false;

		public void Initialize(Simulation simulation) { }

		public void PrepareForIntegration(float dt)
		{
			_gravityDt = Vector3Wide.Broadcast(_gravity * dt);
			_linearDampingDt = new Vector<float>(MathF.Pow(
				Math.Clamp(1f - _linearDamping, 0f, 1f), dt));
			_angularDampingDt = new Vector<float>(MathF.Pow(
				Math.Clamp(1f - _angularDamping, 0f, 1f), dt));
		}

		public void IntegrateVelocity(
			Vector<int> bodyIndices,
			Vector3Wide position,
			QuaternionWide orientation,
			BodyInertiaWide localInertia,
			Vector<int> integrationMask,
			int workerIndex,
			Vector<float> dt,
			ref BodyVelocityWide velocity)
		{
			velocity.Linear = (velocity.Linear + _gravityDt) * _linearDampingDt;
			velocity.Angular *= _angularDampingDt;
		}
	}
}
