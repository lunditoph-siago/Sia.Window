using Sia;

namespace Sia.Window;

/// <summary>Window events targeted at the entity that hosts the window.</summary>
public static class WindowEvents
{
    public readonly record struct Resized(WindowSize Size) : IEvent;

    public readonly record struct FramebufferResized(WindowSize Size) : IEvent;

    public readonly record struct CloseRequested : IEvent;

    public readonly record struct ContentScaleChanged(ContentScale Scale) : IEvent;
}
