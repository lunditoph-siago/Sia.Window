namespace Sia.GLFW;

public sealed class GlfwException : Exception
{
    public GlfwException(string message)
        : base(message)
    {
    }
}
