namespace Sia.Input;

/// <summary>State transition reported for a key or button.</summary>
public enum InputAction
{
    Release = 0,
    Press = 1,
    Repeat = 2,
}

[Flags]
public enum KeyModifiers
{
    Shift = 0x0001,
    Control = 0x0002,
    Alt = 0x0004,
    Super = 0x0008,
    CapsLock = 0x0010,
    NumLock = 0x0020,
}

public enum MouseButton
{
    Button1 = 0,
    Button2 = 1,
    Button3 = 2,
    Button4 = 3,
    Button5 = 4,
    Button6 = 5,
    Button7 = 6,
    Button8 = 7,
    Left = Button1,
    Right = Button2,
    Middle = Button3,
}

[Flags]
public enum JoystickHats
{
    Centered = 0,
    Up = 1,
    Right = 2,
    Down = 4,
    Left = 8,
    RightUp = Right | Up,
    RightDown = Right | Down,
    LeftUp = Left | Up,
    LeftDown = Left | Down,
}

public enum GamepadButton
{
    A = 0,
    B = 1,
    X = 2,
    Y = 3,
    LeftBumper = 4,
    RightBumper = 5,
    Back = 6,
    Start = 7,
    Guide = 8,
    LeftThumb = 9,
    RightThumb = 10,
    DpadUp = 11,
    DpadRight = 12,
    DpadDown = 13,
    DpadLeft = 14,
}

public enum GamepadAxis
{
    LeftX = 0,
    LeftY = 1,
    RightX = 2,
    RightY = 3,
    LeftTrigger = 4,
    RightTrigger = 5,
}
