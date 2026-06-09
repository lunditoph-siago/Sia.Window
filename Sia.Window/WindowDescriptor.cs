namespace Sia.Window;

/// <summary>Describes a backend-agnostic window creation operation.</summary>
public readonly record struct WindowDescriptor(
    int Width = 1280,
    int Height = 720,
    string Title = "",
    bool Visible = true,
    bool Resizable = true,
    bool Decorated = true,
    bool Floating = false,
    bool Focused = true)
{
    public static void Validate(in WindowDescriptor descriptor)
    {
        if (descriptor.Width <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(descriptor), "Window width must be positive.");
        }
        if (descriptor.Height <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(descriptor), "Window height must be positive.");
        }

        ArgumentNullException.ThrowIfNull(descriptor.Title);
    }
}
