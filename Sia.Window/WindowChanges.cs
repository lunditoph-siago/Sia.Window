namespace Sia.Window;

[Flags]
public enum WindowChanges
{
    None = 0,
    Size = 1 << 0,
    FramebufferSize = 1 << 1,
    CloseRequested = 1 << 2,
}
