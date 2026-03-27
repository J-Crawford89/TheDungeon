using System.IO;
using System.Runtime.CompilerServices;

public static class DebugMessage
{
	public static string Format(
		string attemptedAction,
		string failureDescription,
		string? inputSummary = null,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "")
	{
		var fileName = Path.GetFileName(sourceFilePath);
		var message = $"{fileName}.{memberName} :: Attempted: {attemptedAction}. Failed: {failureDescription}.";
		if (!string.IsNullOrEmpty(inputSummary))
			message += $" Inputs: {inputSummary}";
		return message;
	}
}
