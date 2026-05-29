namespace Sia.GLFW;

/// <summary>A copyable GLFW window capability suitable for an ECS component.</summary>
/// <remarks>
/// Copying this value does not create ownership. Windows created through
/// <see cref="GlfwWorldExtensions.CreateGlfwWindow"/> are owned by the world's
/// <see cref="GlfwModule"/>. Standalone callers must use
/// <see cref="Glfw.DestroyWindow(ref GlfwWindow)"/>.
/// </remarks>
public readonly record struct GlfwWindow(nint Handle)
{
    public bool IsNull => Handle == 0;
}
