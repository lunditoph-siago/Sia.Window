using Sia.Window;

namespace Sia.Window.Input.Example;

internal readonly record struct WindowLabel(string Name);

internal record struct WindowMovementTracker(WindowPoint? LastPosition = null);
