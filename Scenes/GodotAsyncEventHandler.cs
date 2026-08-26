using System;
using System.Threading.Tasks;
using Godot;

/// <summary>Single safe boundary for Task-returning work launched by Godot void events.</summary>
public static class GodotAsyncEventHandler
{
	public static void Run(Func<Task> operation, string context) =>
		RunGuardedAsync(operation, context);

	private static async void RunGuardedAsync(Func<Task> operation, string context)
	{
		await AsyncOperationGuard.RunAsync(
			operation,
			exception => GD.PushError($"{context} failed: {exception}"));
	}
}
