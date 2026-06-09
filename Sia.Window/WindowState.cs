namespace Sia.Window;

/// <summary>Observable window state stored independently from the native capability.</summary>
public record struct WindowState(
    WindowSize Size,
    WindowSize FramebufferSize,
    bool CloseRequested,
    ContentScale ContentScale = default)
{
    public WindowChanges Apply(in WindowState next)
    {
        var changes = GetChanges(in this, in next);
        this = next;
        return changes;
    }

    public static WindowChanges GetChanges(
        in WindowState previous,
        in WindowState current)
    {
        var changes = WindowChanges.None;
        if (previous.Size != current.Size) {
            changes |= WindowChanges.Size;
        }
        if (previous.FramebufferSize != current.FramebufferSize) {
            changes |= WindowChanges.FramebufferSize;
        }
        if (!previous.CloseRequested && current.CloseRequested) {
            changes |= WindowChanges.CloseRequested;
        }
        if (previous.ContentScale != current.ContentScale) {
            changes |= WindowChanges.ContentScale;
        }

        return changes;
    }
}
