using Sia;

namespace Sia.Input;

/// <summary>Input events targeted at the entity that hosts the source window.</summary>
public static class InputEvents
{
    public readonly record struct KeyPressed(
        Key Key, int ScanCode, KeyModifiers Modifiers) : IEvent;

    public readonly record struct KeyReleased(
        Key Key, int ScanCode, KeyModifiers Modifiers) : IEvent;

    public readonly record struct KeyRepeated(
        Key Key, int ScanCode, KeyModifiers Modifiers) : IEvent;

    /// <summary>A Unicode code point produced by text input.</summary>
    public readonly record struct TextEntered(uint CodePoint) : IEvent;

    public readonly record struct MouseButtonPressed(
        MouseButton Button, KeyModifiers Modifiers) : IEvent;

    public readonly record struct MouseButtonReleased(
        MouseButton Button, KeyModifiers Modifiers) : IEvent;

    public readonly record struct MouseMoved(MousePosition Position) : IEvent;

    public readonly record struct MouseScrolled(ScrollDelta Delta) : IEvent;

    public readonly record struct MouseEntered : IEvent;

    public readonly record struct MouseExited : IEvent;
}
