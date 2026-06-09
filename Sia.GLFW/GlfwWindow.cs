namespace Sia.GLFW;

public readonly record struct GlfwWindow(nint Handle, long Generation)
{
    public bool IsNull => Handle == 0;
}
