namespace Sia.GLFW;

/// <summary>A borrowed GLFW monitor capability.</summary>
public readonly record struct GlfwMonitor(nint Handle)
{
    public bool IsNull => Handle == 0;
}
