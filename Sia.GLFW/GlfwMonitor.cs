namespace Sia.GLFW;

public readonly record struct GlfwMonitor(nint Handle)
{
    public bool IsNull => Handle == 0;
}
