using System;
using System.Threading;
using System.Threading.Tasks;
using Bunit.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bunit.Extensions.WaitForHelpers
{
	/// <summary>
	/// Represents a helper class that can wait for a render notifications from a <see cref="IRenderedFragmentBase"/> type,
	/// until a specific timeout is reached.
	/// </summary>
	public abstract class WaitForHelper : IDisposable
	{
		private readonly object lockObject = new();
		private readonly Timer timer;
		private readonly TaskCompletionSource<object?> checkPassedCompletionSource;
		private readonly Func<bool> completeChecker;
		private readonly IRenderedFragmentBase renderedFragment;
		private readonly ILogger logger;
		private bool isDisposed;
		private Exception? capturedException;

		/// <summary>
		/// Gets the error message passed to the user when the wait for helper times out.
		/// </summary>
		protected virtual string? TimeoutErrorMessage { get; }

		/// <summary>
		/// Gets the error message passed to the user when the wait for checker throws an exception.
		/// Only used if <see cref="StopWaitingOnCheckException"/> is true.
		/// </summary>
		protected virtual string? CheckThrowErrorMessage { get; }

		/// <summary>
		/// Gets a value indicating whether to continue waiting if the wait condition checker throws.
		/// </summary>
		protected abstract bool StopWaitingOnCheckException { get; }

		/// <summary>
		/// Gets the task that will complete successfully if the check passed before the timeout was reached.
		/// The task will complete with an <see cref="WaitForFailedException"/> exception if the timeout was reached without the check passing.
		/// </summary>
		public Task WaitTask { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="WaitForHelper"/> class.
		/// </summary>
		protected WaitForHelper(IRenderedFragmentBase renderedFragment, Func<bool> completeChecker, TimeSpan? timeout = null)
		{
			this.renderedFragment = renderedFragment ?? throw new ArgumentNullException(nameof(renderedFragment));
			this.completeChecker = completeChecker ?? throw new ArgumentNullException(nameof(completeChecker));
			logger = renderedFragment.Services.CreateLogger<WaitForHelper>();

			var renderer = renderedFragment.Services.GetRequiredService<ITestRenderer>();
			var renderException = renderer
				.UnhandledException
				.ContinueWith(x => Task.FromException(x.Result), CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current)
				.Unwrap();

			checkPassedCompletionSource = new TaskCompletionSource<object?>();
			WaitTask = Task.WhenAny(checkPassedCompletionSource.Task, renderException).Unwrap();

			timer = new Timer(OnTimeout, this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

			if (!WaitTask.IsCompleted)
			{
				OnAfterRender(this, EventArgs.Empty);
				this.renderedFragment.OnAfterRender += OnAfterRender;
				OnAfterRender(this, EventArgs.Empty);
				StartTimer(timeout);
			}
		}

		private void StartTimer(TimeSpan? timeout)
		{
			if (isDisposed)
				return;

			lock (lockObject)
			{
				if (isDisposed)
					return;

				timer.Change(GetRuntimeTimeout(timeout), Timeout.InfiniteTimeSpan);
			}
		}

		private void OnAfterRender(object? sender, EventArgs args)
		{
			if (isDisposed)
				return;

			lock (lockObject)
			{
				if (isDisposed)
					return;

				try
				{
					logger.LogDebug(new EventId(1, nameof(OnAfterRender)), $"Checking the wait condition for component {renderedFragment.ComponentId}");
					if (completeChecker())
					{
						checkPassedCompletionSource.TrySetResult(null);
						logger.LogDebug(new EventId(2, nameof(OnAfterRender)), $"The check completed successfully for component {renderedFragment.ComponentId}");
						Dispose();
					}
					else
					{
						logger.LogDebug(new EventId(3, nameof(OnAfterRender)), $"The check failed for component {renderedFragment.ComponentId}");
					}
				}
				catch (Exception ex)
				{
					capturedException = ex;
					logger.LogDebug(new EventId(4, nameof(OnAfterRender)), $"The checker of component {renderedFragment.ComponentId} throw an exception with message '{ex.Message}'");

					if (StopWaitingOnCheckException)
					{
						checkPassedCompletionSource.TrySetException(new WaitForFailedException(CheckThrowErrorMessage, capturedException));
						Dispose();
					}
				}
			}
		}

		private void OnTimeout(object? state)
		{
			if (isDisposed)
				return;

			lock (lockObject)
			{
				if (isDisposed)
					return;

				logger.LogDebug(new EventId(5, nameof(OnTimeout)), $"The wait for helper for component {renderedFragment.ComponentId} timed out");

				checkPassedCompletionSource.TrySetException(new WaitForFailedException(TimeoutErrorMessage, capturedException));

				Dispose();
			}
		}

		/// <summary>
		/// Disposes the wait helper and cancels the any ongoing waiting, if it is not
		/// already in one of the other completed states.
		/// </summary>
		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes of the wait task and related logic.
		/// </summary>
		/// <remarks>
		/// The disposing parameter should be false when called from a finalizer, and true when called from the
		/// <see cref="Dispose()"/> method. In other words, it is true when deterministically called and false when non-deterministically called.
		/// </remarks>
		/// <param name="disposing">Set to true if called from <see cref="Dispose()"/>, false if called from a finalizer.f.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (isDisposed || !disposing)
				return;

			lock (lockObject)
			{
				if (isDisposed)
					return;

				isDisposed = true;
				renderedFragment.OnAfterRender -= OnAfterRender;
				timer.Dispose();
				checkPassedCompletionSource.TrySetCanceled();
				logger.LogDebug(new EventId(6, nameof(Dispose)), $"The state wait helper for component {renderedFragment.ComponentId} disposed");
			}
		}

		private static TimeSpan GetRuntimeTimeout(TimeSpan? timeout)
		{
			return timeout ?? TimeSpan.FromSeconds(1);
		}
	}
}
