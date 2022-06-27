using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Dom.Events;
using AngleSharp.Html.Parser;
using Bunit.RenderingV2.AngleSharp;

namespace Bunit.RenderingV2.ComponentTree;

internal class ComponentAdapter
{
	private readonly List<ComponentAdapter> children;
	private readonly IDocument dom;
	private readonly HtmlParser htmlParser;
	private readonly TestRendererV2 renderer;
	private int latestUpdateNumber;

	public int ComponentId { get; }

	public IComponent Component { get; }

	public IElement ParentElement { get; }

	public NodeSpan NodeSpan { get; private set; }

	public IReadOnlyList<ComponentAdapter> Children => children;

	public ComponentAdapter(
		int componentId,
		IComponent component,
		IElement parentElement,
		NodeSpan nodeSpan,
		HtmlParser htmlParser,
		TestRendererV2 renderer)
	{
		ComponentId = componentId;
		Component = component;
		ParentElement = parentElement;
		NodeSpan = nodeSpan;
		this.htmlParser = htmlParser;
		this.renderer = renderer;
		this.dom = parentElement.Owner!;
		children = new List<ComponentAdapter>();
	}

	public void AddChild(ComponentAdapter child)
		=> children.Add(child);

	public void ApplyEdits(in RenderTreeDiff updatedComponent, in RenderBatch renderBatch, int updateNumber)
	{
		if (latestUpdateNumber >= updateNumber)
		{
			return;
		}

		latestUpdateNumber = updateNumber;

		ApplyEdits(updatedComponent, renderBatch, this, this.ParentElement);
	}

	private static void ApplyEdits(in RenderTreeDiff updatedComponent, in RenderBatch renderBatch, in ComponentAdapter owner, IElement containingElement)
	{
		foreach (var edit in updatedComponent.Edits)
		{
			switch (edit.Type)
			{
				case RenderTreeEditType.PrependFrame:
				{
					ApplyPrependFrame(edit.ReferenceFrameIndex, renderBatch, owner, containingElement);
					break;
				}
				case RenderTreeEditType.SetAttribute:
				{
					ref var frame = ref renderBatch.ReferenceFrames.Array[edit.ReferenceFrameIndex];
					var node = (IElement)containingElement.ChildNodes[edit.SiblingIndex];
					ApplySetAttribute(ref frame, owner, node);
					break;
				}
				case RenderTreeEditType.UpdateText:
				{
					ref var frame = ref renderBatch.ReferenceFrames.Array[edit.ReferenceFrameIndex];
					var node = containingElement.ChildNodes[edit.SiblingIndex];
					node.TextContent = frame.TextContent;
					break;
				}
				// StepIn seems to be about going from the current containing element into a child
				// element basedon the sibling index
				case RenderTreeEditType.StepIn:
				{
					containingElement = (IElement)owner.ParentElement.ChildNodes[edit.SiblingIndex];
					break;
				}
				case RenderTreeEditType.StepOut:
				{
					containingElement = containingElement.ParentElement!;
					break;
				}
				default:
					throw new NotImplementedException($"Edit type not supported: {edit.Type}");
			}
		}
	}

#pragma warning disable MA0051 // Method is too long
	private static void ApplyPrependFrame(int referenceFrameIndex, in RenderBatch renderBatch, in ComponentAdapter owner, IElement containingElement)
#pragma warning restore MA0051 // Method is too long
	{
		ref var frame = ref renderBatch.ReferenceFrames.Array[referenceFrameIndex];
		switch (frame.FrameType)
		{
			case RenderTreeFrameType.Component:
			{
				var child = owner.renderer.CreateComponentAdapter(frame.ComponentId, frame.Component, owner.ParentElement, new NodeSpan(containingElement));
				owner.AddChild(child);
				for (var i = referenceFrameIndex + 1; i < renderBatch.UpdatedComponents.Count; i++)
				{
					var componentEdits = renderBatch.UpdatedComponents.Array[i];
					if (componentEdits.ComponentId == frame.ComponentId && componentEdits.Edits.Count > 0)
					{
						child.ApplyEdits(componentEdits, renderBatch, owner.latestUpdateNumber);
					}
				}
				break;
			}
			case RenderTreeFrameType.Element:
			{
				var newElement = owner.dom.CreateElement(frame.ElementName);

				//if (ReferenceEquals(owner.NodeSpan.Source, containingElement) && owner.NodeSpan.Last is not null)
				//{
				//	newElement.InsertAfter(owner.NodeSpan.Last);
				//	owner.NodeSpan = owner.NodeSpan with
				//	{
				//		Last = newElement
				//	};
				//}
				//else
				//{
				containingElement.AppendChild(newElement);
				//}

				var endIndexExcl = referenceFrameIndex + frame.ElementSubtreeLength;
				for (var descendantIndex = referenceFrameIndex + 1; descendantIndex < endIndexExcl; descendantIndex++)
				{
					ref var candidateFrame = ref renderBatch.ReferenceFrames.Array[descendantIndex];
					if (candidateFrame.FrameType == RenderTreeFrameType.Attribute)
					{
						ApplySetAttribute(ref candidateFrame, owner, newElement);
					}
					else
					{
						// As soon as we see a non-attribute child, all the subsequent child frames are
						// not attributes, so bail out and insert the remnants recursively
						InsertFrameRange(descendantIndex, endIndexExcl, renderBatch, owner, newElement);
						break;
					}
				}
				break;
			}
			case RenderTreeFrameType.Markup:
			{
				// TODO: should we detect whitespace/newlines/text only and skip using ParseFragment for speed?
				var markupNodes = owner.htmlParser.ParseFragment(frame.MarkupContent, containingElement);
				if (markupNodes.Length > 0)
				{
					//if (ReferenceEquals(owner.NodeSpan.Source, containingElement) && owner.NodeSpan.Last is not null)
					//{
					//	markupNodes.InsertAfter(owner.NodeSpan.Last);
					//	owner.UpdateLastNode(markupNodes[^1]);
					//}
					//else
					//{
					containingElement.AppendNodes(markupNodes);
					//}
				}
				break;
			}
			case RenderTreeFrameType.Text:
			{
				var text = owner.dom.CreateTextNode(frame.TextContent);
				containingElement.AppendChild(text);
				break;
			}
			default:
				throw new NotImplementedException($"Frame type not supported: {frame.FrameType}");
		}
	}

	private static void InsertFrameRange(int startIndex, int endIndexExcl, in RenderBatch batch, in ComponentAdapter owner, IElement containingElement)
	{
		for (var frameIndex = startIndex; frameIndex < endIndexExcl; frameIndex++)
		{
			ApplyPrependFrame(frameIndex, batch, owner, containingElement);

			// Skip over any descendants, since they are already dealt with recursively
			ref var frame = ref batch.ReferenceFrames.Array[frameIndex];
			frameIndex += CountDescendantFrames(frame);
		}
	}

	private static int CountDescendantFrames(RenderTreeFrame frame) => frame.FrameType switch
	{
		// The following frame types have a subtree length. Other frames may use that memory slot
		// to mean something else, so we must not read it. We should consider having nominal subtypes
		// of RenderTreeFramePointer that prevent access to non-applicable fields.
		RenderTreeFrameType.Component => frame.ComponentSubtreeLength - 1,
		RenderTreeFrameType.Element => frame.ElementSubtreeLength - 1,
		RenderTreeFrameType.Region => frame.RegionSubtreeLength - 1,
		_ => 0,
	};

#pragma warning disable MA0051 // Method is too long
	private static void ApplySetAttribute(ref RenderTreeFrame attributeFrame, ComponentAdapter owner, IElement element)
#pragma warning restore MA0051 // Method is too long
	{
		if (attributeFrame.AttributeValue is Delegate)
		{
			var eventHandlerId = attributeFrame.AttributeEventHandlerId;

			// TODO: Should we pass/create an EventFieldInfo in the event handler?
			// TODO: Can we handle async event handlers via the AngleSharp event dispatch system?
			element.AddEventListener(
				attributeFrame.AttributeName,
				(sender, ev) =>
				{
					EventArgs blazorEvent;
					if (ev is BunitEvent be)
					{
						blazorEvent = be.BlazorEventArgs;
						var dispatchResult = owner.renderer.DispatchEventAsync(
							eventHandlerId,
							default(EventFieldInfo),
							be.BlazorEventArgs);

						be.AddEventHandlerTask(dispatchResult);
					}
					else
					{
						blazorEvent = Map(ev);
						owner.renderer.DispatchEventAsync(
							eventHandlerId,
							default(EventFieldInfo),
							blazorEvent);
					}
					ApplySideEffect(sender, blazorEvent);
				});
		}
		else
		{
			element.SetAttribute(
				attributeFrame.AttributeName,
				attributeFrame.AttributeValue?.ToString() ?? string.Empty);
		}

		static void ApplySideEffect(object node, EventArgs e)
		{
			// This applies side effects to DOM elements.
			// Is it a good idea? There is a related issue on this.
			// Could this cause problems when the DOM tree is updated?
			// Perhaps not, since a ApplySetAttribute would simply override the value.

			// TODO: Is there a way to get AngleSharp to do this?
			// TODO: Get all side effects implemented.
			switch (node)
			{
				case IHtmlInputElement input when e is KeyboardEventArgs kb:
				{
					input.SetAttribute("value", input.GetAttribute("value") + kb.Key);
					break;
				}
				default:
					break;
			}
		}

		static EventArgs Map(Event e)
		{
			return e switch
			{
				Event _ and { Type: "onclick" } or
				MouseEvent _ and { Type: "onclick" } => new MouseEventArgs()
				{
					Type = ToBlazorEventType(e.Type),
					Detail = 1
				},
				KeyboardEvent ke => new KeyboardEventArgs
				{
					Type = ToBlazorEventType(ke.Type),
					Key = ke.Key!,
				},
				Event _ => EventArgs.Empty,
				_ => throw new NotImplementedException($"Mapping for {e.Type} not implemented.")
			};

			// Strip out "on" from start of event type since that is what
			// Blazor expects. AngleSharp/HTML5 requires the "on" prefix.
			static string ToBlazorEventType(string type) => type[2..];
		}
	}
}

