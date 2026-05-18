using Godot;
using System;
using System.Threading.Tasks;

public partial class RollingDie : RigidBody3D
{
	[ExportGroup("Spawn")]
	[Export] public Vector3 SpawnPosition { get; set; } = new(0, 2.5f, 0);
	[Export] public Vector3 MinInitialRotationDegrees { get; set; } = new(0, 0, 0);
	[Export] public Vector3 MaxInitialRotationDegrees { get; set; } = new(360, 360, 360);

	[ExportGroup("Linear Impulse")]
	[Export] public Vector3 MinImpulse { get; set; } = new(4.0f, 1.0f, -0.8f);
	[Export] public Vector3 MaxImpulse { get; set; } = new(6.5f, 2.0f, 0.8f);

	[ExportGroup("Angular Impulse")]
	[Export] public Vector3 MinTorqueImpulse { get; set; } = new(-25.0f, -25.0f, -25.0f);
	[Export] public Vector3 MaxTorqueImpulse { get; set; } = new(25.0f, 25.0f, 25.0f);

	[ExportGroup("Timing")]
	[Export] public float MaxRollDurationSeconds { get; set; } = 1.8f;
	[Export] public float MinRollDurationSeconds { get; set; } = 0.9f;

	[Export] public int FaceValue { get; set; } = 20;

	public async Task RollAsync()
	{
		Freeze = false;
		Sleeping = false;

		GlobalPosition = SpawnPosition;

		GlobalRotationDegrees = new Vector3(
			(float)GD.RandRange(MinInitialRotationDegrees.X, MaxInitialRotationDegrees.X),
			(float)GD.RandRange(MinInitialRotationDegrees.Y, MaxInitialRotationDegrees.Y),
			(float)GD.RandRange(MinInitialRotationDegrees.Z, MaxInitialRotationDegrees.Z)
			);

		LinearVelocity = Vector3.Zero;
		AngularVelocity = Vector3.Zero;

		ApplyCentralImpulse(new Vector3(
			(float)GD.RandRange(MinImpulse.X, MaxImpulse.X),
			(float)GD.RandRange(MinImpulse.Y, MaxImpulse.Y),
			(float)GD.RandRange(MinImpulse.Z, MaxImpulse.Z)
			));

		ApplyTorqueImpulse(new Vector3(
			(float)GD.RandRange(MinTorqueImpulse.X, MaxTorqueImpulse.X),
			(float)GD.RandRange(MinTorqueImpulse.Y, MaxTorqueImpulse.Y),
			(float)GD.RandRange(MinTorqueImpulse.Z, MaxTorqueImpulse.Z)
			));

		await ToSignal(GetTree().CreateTimer(GD.RandRange(MinRollDurationSeconds, MaxRollDurationSeconds)), SceneTreeTimer.SignalName.Timeout);

		Freeze = true;
		LinearVelocity = Vector3.Zero;
		AngularVelocity = Vector3.Zero;
	}
}
