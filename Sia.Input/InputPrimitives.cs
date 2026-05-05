namespace Sia.Input;

/// <summary>A cursor position in window content coordinates.</summary>
public readonly record struct MousePosition(double X, double Y);

/// <summary>A scroll offset reported by one wheel or touchpad gesture.</summary>
public readonly record struct ScrollDelta(double X, double Y);
