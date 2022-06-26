using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;

namespace Bunit.RenderingV2;

public partial class TestRendererV2 : Renderer
{
	private readonly ILogger logger;
	private readonly Dictionary<int, IRenderedComponent> rootComponents = new();
	private readonly HtmlParser htmlParser;
	private TaskCompletionSource<Exception> unhandledExceptionTsc = new();
	private Exception? capturedUnhandledException;

	internal HtmlParser HtmlParser => htmlParser;

	public int RenderCount { get; private set; }

	public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

	public Task<Exception> UnhandledException => unhandledExceptionTsc.Task;

	public TestRendererV2(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
		: base(serviceProvider, loggerFactory)
	{
		logger = loggerFactory.CreateLogger<TestRendererV2>();
		htmlParser = new HtmlParser(new HtmlParserOptions
		{
			IsAcceptingCustomElementsEverywhere = false,
			IsKeepingSourceReferences = false,
			IsNotConsumingCharacterReferences = false,
			IsNotSupportingFrames = false,
			IsPreservingAttributeNames = false,
			IsScripting = false,
			IsSupportingProcessingInstructions = false,
			IsEmbedded = true,
			IsStrictMode = false,
		});
	}

	/// <summary>
	/// Renders the <paramref name="renderFragment"/> inside a <see cref="RootComponent"/>.
	/// This returns as soon all components in the <paramref name="renderFragment"/>
	/// have finished their first render cycle. It does not await any async life cycle methods
	/// in the components. To await life cycle methods, use <see cref="RenderAsync(RenderFragment)"/>.
	/// </summary>
	public IRenderedComponent<RootComponent> Render(RenderFragment renderFragment)
	{
		try
		{
			var renderTask = Dispatcher.InvokeAsync(() =>
			{
				var rc = InitializeRenderedRootComponent(renderFragment);
				_ = RenderRootComponentAsync(rc.ComponentId);
				return rc;
			});
			Debug.Assert(renderTask.IsCompletedSuccessfully, "There should not be any asynchronous code in the inside the lambda passed to Dispatcher.InvokeAsync.");
			return renderTask.Result;
		}
		catch (Exception ex)
		{
			HandleException(ex);
			throw;
		}
	}

	/// <summary>
	/// Renders the <paramref name="renderFragment"/> inside a <see cref="RootComponent"/>.
	/// The returned task completes when all components in the <paramref name="renderFragment"/>
	/// have finished rendering completely. That includes waiting for any async operations
	/// in life-cycle methods of the components in the render tree.
	/// </summary>
	public async Task<IRenderedComponent<RootComponent>> RenderAsync(RenderFragment renderFragment)
	{
		try
		{
			return await Dispatcher.InvokeAsync(async () =>
			{
				var rc = InitializeRenderedRootComponent(renderFragment);
				await RenderRootComponentAsync(rc.ComponentId).ConfigureAwait(false);
				return rc;
			}).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			HandleException(ex);
			throw;
		}
	}

	private IRenderedComponent<RootComponent> InitializeRenderedRootComponent(RenderFragment renderFragment)
	{
		var component = new RootComponent(renderFragment);
		var componentId = AssignRootComponentId(component);
		return InitializeRenderedComponent<RootComponent>(componentId, component, default(IElement));
	}

	internal RenderedComponentV2<TComponent> InitializeRenderedComponent<TComponent>(int componentId, TComponent instance, IElement? parentElement)
		where TComponent : IComponent
	{
		var rc = parentElement is null
			? new RenderedComponentV2<TComponent>(componentId, instance, this)
			: new RenderedComponentV2<TComponent>(componentId, instance, this, parentElement);

		rootComponents[componentId] = rc;

		return rc;
	}

	internal IRenderedComponent<IComponent> InitializeRenderedComponent(int componentId, IComponent instance, IElement? parentElement)
	{
		var rcGenType = typeof(RenderedComponentV2<>);
		var componentType = instance.GetType();
		var rcType = rcGenType.MakeGenericType(componentType);
		var rc = (IRenderedComponent<IComponent>)Activator.CreateInstance(rcType, new object?[] { componentId, instance, this, parentElement })!;

		rootComponents[componentId] = rc;

		return rc;
	}

	protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
	{
		RenderCount++;
		var numUpdatedComponents = renderBatch.UpdatedComponents.Count;
		for (var componentIndex = 0; componentIndex < numUpdatedComponents; componentIndex++)
		{
			var updatedComponent = renderBatch.UpdatedComponents.Array[componentIndex];

			if (updatedComponent.Edits.Count > 0)
			{
				var rc = rootComponents[updatedComponent.ComponentId];
				rc.ApplyEdits(updatedComponent, renderBatch, RenderCount);
			}
		}

		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	protected override void HandleException(Exception exception)
	{
		if (exception is null)
			return;

		LogUnhandledException(logger, exception);

		capturedUnhandledException = exception;

		if (!unhandledExceptionTsc.TrySetResult(capturedUnhandledException))
		{
			unhandledExceptionTsc = new TaskCompletionSource<Exception>();
			unhandledExceptionTsc.SetResult(capturedUnhandledException);
		}
	}
}
