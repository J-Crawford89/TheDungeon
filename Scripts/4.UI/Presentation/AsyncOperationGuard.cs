using System;
using System.Threading.Tasks;

/// <summary>
/// Converts an awaited operation into a safe event-boundary task by reporting faults and
/// treating cancellation as an expected termination path.
/// </summary>
public static class AsyncOperationGuard
{
	public static async Task RunAsync(Func<Task> operation, Action<Exception> reportException)
	{
		ArgumentNullException.ThrowIfNull(operation);
		ArgumentNullException.ThrowIfNull(reportException);

		try
		{
			await operation();
		}
		catch (OperationCanceledException)
		{
			// Cancellation is an expected way for scene-bound work to stop.
		}
		catch (Exception exception)
		{
			reportException(exception);
		}
	}
}
