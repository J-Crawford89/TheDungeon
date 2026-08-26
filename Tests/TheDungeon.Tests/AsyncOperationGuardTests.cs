using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public sealed class AsyncOperationGuardTests
{
	[Fact]
	public async Task RunAsync_WhenOperationCompletes_DoesNotReportException()
	{
		var reports = new List<Exception>();
		var operationRan = false;

		await AsyncOperationGuard.RunAsync(
			() =>
			{
				operationRan = true;
				return Task.CompletedTask;
			},
			reports.Add);

		Assert.True(operationRan);
		Assert.Empty(reports);
	}

	[Fact]
	public async Task RunAsync_WhenOperationThrowsSynchronously_ReportsException()
	{
		var reports = new List<Exception>();
		var expected = new InvalidOperationException("synchronous fault");

		await AsyncOperationGuard.RunAsync(
			() => throw expected,
			reports.Add);

		Assert.Same(expected, Assert.Single(reports));
	}

	[Fact]
	public async Task RunAsync_WhenOperationFaultsAsynchronously_ReportsException()
	{
		var reports = new List<Exception>();
		var expected = new InvalidOperationException("asynchronous fault");

		await AsyncOperationGuard.RunAsync(
			async () =>
			{
				await Task.Yield();
				throw expected;
			},
			reports.Add);

		Assert.Same(expected, Assert.Single(reports));
	}

	[Fact]
	public async Task RunAsync_WhenOperationIsCanceled_DoesNotReportException()
	{
		var reports = new List<Exception>();
		var canceled = Task.FromCanceled(new CancellationToken(canceled: true));

		await AsyncOperationGuard.RunAsync(() => canceled, reports.Add);

		Assert.Empty(reports);
	}
}
