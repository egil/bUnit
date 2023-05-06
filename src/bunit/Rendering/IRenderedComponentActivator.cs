namespace Bunit.Rendering;

/// <summary>
/// Represents an activator for <see cref="IRenderedFragmentBase"/> and <see cref="IRenderedComponent{TComponent}"/> types.
/// </summary>
public interface IRenderedComponentActivator
{
	/// <summary>
	/// Creates an <see cref="IRenderedFragmentBase"/> with the specified <paramref name="componentId"/>.
	/// </summary>
	IRenderedFragmentBase CreateRenderedFragment(int componentId);

	/// <summary>
	/// Creates an <see cref="IRenderedComponent{TComponent}"/> with the specified <paramref name="componentId"/>.
	/// </summary>
	IRenderedComponent<TComponent> CreateRenderedComponent<TComponent>(int componentId)
		where TComponent : IComponent;

	/// <summary>
	/// Creates an <see cref="IRenderedComponent{TComponent}"/> with the specified <paramref name="componentId"/>,
	/// <paramref name="component"/>, and <paramref name="componentFrames"/>.
	/// </summary>
	IRenderedComponent<TComponent> CreateRenderedComponent<TComponent>(int componentId, TComponent component, RenderTreeFrameDictionary componentFrames)
		where TComponent : IComponent;
}
