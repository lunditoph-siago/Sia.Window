using System.Runtime.InteropServices;

using Sia.Input;

namespace Sia.GLFW;

/// <summary>Opaque window handle.</summary>
public struct WindowHandle;

/// <summary>Opaque monitor handle.</summary>
public struct Monitor;

/// <summary>Opaque cursor handle.</summary>
public struct Cursor;

[StructLayout(LayoutKind.Sequential)]
public struct VideoMode
{
    public int Width;
    public int Height;
    public int RedBits;
    public int GreenBits;
    public int BlueBits;
    public int RefreshRate;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Image
{
    public int Width;
    public int Height;
    public byte* Pixels;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct GammaRamp
{
    public ushort* Red;
    public ushort* Green;
    public ushort* Blue;
    public uint Size;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct GamepadState
{
    public fixed byte Buttons[15];
    public fixed float Axes[6];
}

/// <summary>GLFW error callback delegate.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void ErrorCallback(ErrorCode error, [MarshalAs(UnmanagedType.LPUTF8Str)] string description);

/// <summary>Window position callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void WindowPosCallback(WindowHandle* window, int x, int y);

/// <summary>Window size callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void WindowSizeCallback(WindowHandle* window, int width, int height);

/// <summary>Window close callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void WindowCloseCallback(WindowHandle* window);

/// <summary>Window refresh callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void WindowRefreshCallback(WindowHandle* window);

/// <summary>Window focus callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void WindowFocusCallback(WindowHandle* window, bool focused);

/// <summary>Window iconify callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void WindowIconifyCallback(WindowHandle* window, bool iconified);

/// <summary>Window maximize callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void WindowMaximizeCallback(WindowHandle* window, bool maximized);

/// <summary>Framebuffer size callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void FramebufferSizeCallback(WindowHandle* window, int width, int height);

/// <summary>Window content scale callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void WindowContentScaleCallback(WindowHandle* window, float xscale, float yscale);

/// <summary>Key callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void KeyCallback(WindowHandle* window, Key key, int scancode, InputAction action, KeyModifiers mods);

/// <summary>Character callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void CharCallback(WindowHandle* window, uint codepoint);

/// <summary>Character with mods callback (GLFW 3.4+).</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void CharModsCallback(WindowHandle* window, uint codepoint, KeyModifiers mods);

/// <summary>Mouse button callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void MouseButtonCallback(WindowHandle* window, MouseButton button, InputAction action, KeyModifiers mods);

/// <summary>Cursor position callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void CursorPosCallback(WindowHandle* window, double x, double y);

/// <summary>Cursor enter callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void CursorEnterCallback(WindowHandle* window, bool entered);

/// <summary>Scroll callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void ScrollCallback(WindowHandle* window, double xoffset, double yoffset);

/// <summary>Drop callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void DropCallback(WindowHandle* window, int count, byte** paths);

/// <summary>Monitor callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void MonitorCallback(Monitor* monitor, ConnectedState state);

/// <summary>Joystick callback.</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void JoystickCallback(int joystickId, ConnectedState state);
