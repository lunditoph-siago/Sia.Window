using System.Runtime.InteropServices;

using Sia.Input;

namespace Sia.GLFW;

/// <summary>
/// Exposes the GLFW C ABI directly. Callers are responsible for main-thread affinity,
/// native pointer validity, callback rooting, and GLFW initialization.
/// </summary>
public static unsafe partial class GlfwUnsafe
{
#if BROWSER
    private const string _libraryName = "__Internal_emscripten";
#else
    private const string _libraryName = "glfw3";

    static GlfwUnsafe()
    {
        NativeLibrary.SetDllImportResolver(
            typeof(GlfwUnsafe).Assembly,
            static (name, assembly, searchPath) => {
                if (name != _libraryName) {
                    return 0;
                }

                // Linux distributions ship libglfw.so.3 without a
                // version-less symlink, which default probing never tries.
                ReadOnlySpan<string> candidates =
                    ["glfw3", "glfw", "libglfw.so.3", "libglfw.3.dylib"];
                foreach (var candidate in candidates) {
                    if (NativeLibrary.TryLoad(
                        candidate, assembly, searchPath, out var handle)) {
                        return handle;
                    }
                }

                return 0;
            });
    }
#endif

    /// <summary>
    /// Copies a UTF-8 string owned by GLFW. The runtime must never free these
    /// pointers, so bindings return raw pointers instead of marshalled strings.
    /// </summary>
    private static string? ReadUtf8(nint pointer) => Marshal.PtrToStringUTF8(pointer);

    // ── Lifecycle ────────────────────────────────────────────

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwInit")]
    private static extern int _init();
    public static bool Init() => _init() == 1;

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwTerminate")]
    private static extern void _terminate();
    public static void Terminate() => _terminate();

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwInitHint")]
    private static extern void _initHint(int hint, int value);
    public static void InitHint(int hint, int value) => _initHint(hint, value);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetVersion")]
    private static extern void _getVersion(out int major, out int minor, out int rev);
    public static void GetVersion(out int major, out int minor, out int rev) => _getVersion(out major, out minor, out rev);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetVersionString")]
    private static extern nint _getVersionString();
    public static string GetVersionString() => ReadUtf8(_getVersionString()) ?? string.Empty;

    // ── Window ──────────────────────────────────────────────

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwCreateWindow")]
    private static extern WindowHandle* _createWindow(int width, int height, [MarshalAs(UnmanagedType.LPUTF8Str)] string title, Monitor* monitor, WindowHandle* share);
    public static WindowHandle* CreateWindow(int width, int height, string title, Monitor* monitor, WindowHandle* share) =>
        _createWindow(width, height, title, monitor, share);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwDestroyWindow")]
    private static extern void _destroyWindow(WindowHandle* window);
    public static void DestroyWindow(WindowHandle* window) => _destroyWindow(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwWindowShouldClose")]
    private static extern int _windowShouldClose(WindowHandle* window);
    public static bool WindowShouldClose(WindowHandle* window) => _windowShouldClose(window) == 1;

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowShouldClose")]
    private static extern void _setWindowShouldClose(WindowHandle* window, int value);
    public static void SetWindowShouldClose(WindowHandle* window, bool value) => _setWindowShouldClose(window, value ? 1 : 0);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwWindowHint")]
    private static extern void _windowHint(int hint, int value);
    public static void WindowHint(int hint, int value) => _windowHint(hint, value);
    public static void WindowHint(WindowHintClientApi hint, ClientApi value) => _windowHint((int)hint, (int)value);
    public static void WindowHint(WindowHintClientApi hint, ContextCreationApi value) => _windowHint((int)hint, (int)value);
    public static void WindowHint(WindowHintClientApi hint, ContextRobustness value) => _windowHint((int)hint, (int)value);
    public static void WindowHint(WindowHintClientApi hint, OpenGlProfile value) => _windowHint((int)hint, (int)value);
    public static void WindowHint(WindowHintClientApi hint, ReleaseBehavior value) => _windowHint((int)hint, (int)value);
    public static void WindowHint(WindowHintBool hint, bool value) => _windowHint((int)hint, value ? 1 : 0);
    public static void WindowHint(WindowHintInt hint, int value) => _windowHint((int)hint, value);
    public static void WindowHint(WindowHintString hint, string value) => _windowHintString((int)hint, value);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwWindowHintString")]
    private static extern void _windowHintString(int hint, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwDefaultWindowHints")]
    private static extern void _defaultWindowHints();
    public static void DefaultWindowHints() => _defaultWindowHints();

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowTitle")]
    private static extern void _setWindowTitle(WindowHandle* window, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);
    public static void SetWindowTitle(WindowHandle* window, string title) => _setWindowTitle(window, title);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetWindowSize")]
    private static extern void _getWindowSize(WindowHandle* window, out int width, out int height);
    public static void GetWindowSize(WindowHandle* window, out int width, out int height) => _getWindowSize(window, out width, out height);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowSize")]
    private static extern void _setWindowSize(WindowHandle* window, int width, int height);
    public static void SetWindowSize(WindowHandle* window, int width, int height) => _setWindowSize(window, width, height);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetFramebufferSize")]
    private static extern void _getFramebufferSize(WindowHandle* window, out int width, out int height);
    public static void GetFramebufferSize(WindowHandle* window, out int width, out int height) => _getFramebufferSize(window, out width, out height);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowPos")]
    private static extern void _setWindowPos(WindowHandle* window, int x, int y);
    public static void SetWindowPos(WindowHandle* window, int x, int y) => _setWindowPos(window, x, y);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetWindowPos")]
    private static extern void _getWindowPos(WindowHandle* window, out int x, out int y);
    public static void GetWindowPos(WindowHandle* window, out int x, out int y) => _getWindowPos(window, out x, out y);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowSizeLimits")]
    private static extern void _setWindowSizeLimits(WindowHandle* window, int minWidth, int minHeight, int maxWidth, int maxHeight);
    public static void SetWindowSizeLimits(WindowHandle* window, int minWidth, int minHeight, int maxWidth, int maxHeight) =>
        _setWindowSizeLimits(window, minWidth, minHeight, maxWidth, maxHeight);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowAspectRatio")]
    private static extern void _setWindowAspectRatio(WindowHandle* window, int numer, int denom);
    public static void SetWindowAspectRatio(WindowHandle* window, int numer, int denom) => _setWindowAspectRatio(window, numer, denom);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetWindowOpacity")]
    private static extern float _getWindowOpacity(WindowHandle* window);
    public static float GetWindowOpacity(WindowHandle* window) => _getWindowOpacity(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowOpacity")]
    private static extern void _setWindowOpacity(WindowHandle* window, float opacity);
    public static void SetWindowOpacity(WindowHandle* window, float opacity) => _setWindowOpacity(window, opacity);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwIconifyWindow")]
    private static extern void _iconifyWindow(WindowHandle* window);
    public static void IconifyWindow(WindowHandle* window) => _iconifyWindow(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwRestoreWindow")]
    private static extern void _restoreWindow(WindowHandle* window);
    public static void RestoreWindow(WindowHandle* window) => _restoreWindow(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwMaximizeWindow")]
    private static extern void _maximizeWindow(WindowHandle* window);
    public static void MaximizeWindow(WindowHandle* window) => _maximizeWindow(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwShowWindow")]
    private static extern void _showWindow(WindowHandle* window);
    public static void ShowWindow(WindowHandle* window) => _showWindow(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwHideWindow")]
    private static extern void _hideWindow(WindowHandle* window);
    public static void HideWindow(WindowHandle* window) => _hideWindow(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwFocusWindow")]
    private static extern void _focusWindow(WindowHandle* window);
    public static void FocusWindow(WindowHandle* window) => _focusWindow(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwRequestWindowAttention")]
    private static extern void _requestWindowAttention(WindowHandle* window);
    public static void RequestWindowAttention(WindowHandle* window) => _requestWindowAttention(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetWindowMonitor")]
    private static extern Monitor* _getWindowMonitor(WindowHandle* window);
    public static Monitor* GetWindowMonitor(WindowHandle* window) => _getWindowMonitor(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowMonitor")]
    private static extern void _setWindowMonitor(WindowHandle* window, Monitor* monitor, int xpos, int ypos, int width, int height, int refreshRate);
    public static void SetWindowMonitor(WindowHandle* window, Monitor* monitor, int xpos, int ypos, int width, int height, int refreshRate) =>
        _setWindowMonitor(window, monitor, xpos, ypos, width, height, refreshRate);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetWindowAttrib")]
    private static extern int _getWindowAttrib(WindowHandle* window, int attrib);
    public static int GetWindowAttrib(WindowHandle* window, int attrib) => _getWindowAttrib(window, attrib);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowAttrib")]
    private static extern void _setWindowAttrib(WindowHandle* window, int attrib, int value);
    public static void SetWindowAttrib(WindowHandle* window, int attrib, int value) => _setWindowAttrib(window, attrib, value);

    // ── Input ───────────────────────────────────────────────

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwPollEvents")]
    private static extern void _pollEvents();
    public static void PollEvents() => _pollEvents();

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwWaitEvents")]
    private static extern void _waitEvents();
    public static void WaitEvents() => _waitEvents();

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwWaitEventsTimeout")]
    private static extern void _waitEventsTimeout(double timeout);
    public static void WaitEventsTimeout(double timeout) => _waitEventsTimeout(timeout);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwPostEmptyEvent")]
    private static extern void _postEmptyEvent();
    public static void PostEmptyEvent() => _postEmptyEvent();

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetKey")]
    private static extern InputAction _getKey(WindowHandle* window, Key key);
    public static InputAction GetKey(WindowHandle* window, Key key) => _getKey(window, key);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetMouseButton")]
    private static extern InputAction _getMouseButton(WindowHandle* window, MouseButton button);
    public static InputAction GetMouseButton(WindowHandle* window, MouseButton button) => _getMouseButton(window, button);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetCursorPos")]
    private static extern void _getCursorPos(WindowHandle* window, out double xpos, out double ypos);
    public static void GetCursorPos(WindowHandle* window, out double xpos, out double ypos) => _getCursorPos(window, out xpos, out ypos);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetCursorPos")]
    private static extern void _setCursorPos(WindowHandle* window, double xpos, double ypos);
    public static void SetCursorPos(WindowHandle* window, double xpos, double ypos) => _setCursorPos(window, xpos, ypos);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetKeyName")]
    private static extern nint _getKeyName(Key key, int scancode);
    public static string? GetKeyName(Key key, int scancode) => ReadUtf8(_getKeyName(key, scancode));

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetKeyScancode")]
    private static extern int _getKeyScancode(Key key);
    public static int GetKeyScancode(Key key) => _getKeyScancode(key);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetInputMode")]
    private static extern int _getInputMode(WindowHandle* window, InputMode mode);
    public static int GetInputMode(WindowHandle* window, InputMode mode) => _getInputMode(window, mode);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetInputMode")]
    private static extern void _setInputMode(WindowHandle* window, InputMode mode, int value);
    public static void SetInputMode(WindowHandle* window, InputMode mode, int value) => _setInputMode(window, mode, value);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwRawMouseMotionSupported")]
    private static extern int _rawMouseMotionSupported();
    public static bool RawMouseMotionSupported() => _rawMouseMotionSupported() == 1;

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwJoystickPresent")]
    private static extern int _joystickPresent(int jid);
    public static bool JoystickPresent(int jid) => _joystickPresent(jid) == 1;

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetJoystickAxes")]
    private static extern float* _getJoystickAxes(int jid, out int count);
    public static float* GetJoystickAxes(int jid, out int count) => _getJoystickAxes(jid, out count);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetJoystickButtons")]
    private static extern byte* _getJoystickButtons(int jid, out int count);
    public static byte* GetJoystickButtons(int jid, out int count) => _getJoystickButtons(jid, out count);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetJoystickHats")]
    private static extern JoystickHats* _getJoystickHats(int jid, out int count);
    public static JoystickHats* GetJoystickHats(int jid, out int count) => _getJoystickHats(jid, out count);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetJoystickName")]
    private static extern nint _getJoystickName(int jid);
    public static string? GetJoystickName(int jid) => ReadUtf8(_getJoystickName(jid));

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwJoystickIsGamepad")]
    private static extern int _joystickIsGamepad(int jid);
    public static bool JoystickIsGamepad(int jid) => _joystickIsGamepad(jid) == 1;

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetGamepadState")]
    private static extern int _getGamepadState(int jid, out GamepadState state);
    public static bool GetGamepadState(int jid, out GamepadState state) => _getGamepadState(jid, out state) == 1;

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwUpdateGamepadMappings")]
    private static extern int _updateGamepadMappings([MarshalAs(UnmanagedType.LPUTF8Str)] string mappings);
    public static bool UpdateGamepadMappings(string mappings) => _updateGamepadMappings(mappings) == 1;

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetGamepadName")]
    private static extern nint _getGamepadName(int jid);
    public static string? GetGamepadName(int jid) => ReadUtf8(_getGamepadName(jid));

    // ── Callbacks ───────────────────────────────────────────

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetErrorCallback")]
    private static extern ErrorCallback? _setErrorCallback(ErrorCallback? callback);
    public static ErrorCallback? SetErrorCallback(ErrorCallback callback) =>
        _setErrorCallback(callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowPosCallback")]
    private static extern WindowPosCallback? _setWindowPosCallback(WindowHandle* window, WindowPosCallback? callback);
    public static WindowPosCallback? SetWindowPosCallback(WindowHandle* window, WindowPosCallback callback) =>
        _setWindowPosCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowSizeCallback")]
    private static extern WindowSizeCallback? _setWindowSizeCallback(WindowHandle* window, WindowSizeCallback? callback);
    public static WindowSizeCallback? SetWindowSizeCallback(WindowHandle* window, WindowSizeCallback callback) =>
        _setWindowSizeCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowCloseCallback")]
    private static extern WindowCloseCallback? _setWindowCloseCallback(WindowHandle* window, WindowCloseCallback? callback);
    public static WindowCloseCallback? SetWindowCloseCallback(WindowHandle* window, WindowCloseCallback callback) =>
        _setWindowCloseCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowRefreshCallback")]
    private static extern WindowRefreshCallback? _setWindowRefreshCallback(WindowHandle* window, WindowRefreshCallback? callback);
    public static WindowRefreshCallback? SetWindowRefreshCallback(WindowHandle* window, WindowRefreshCallback callback) =>
        _setWindowRefreshCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowFocusCallback")]
    private static extern WindowFocusCallback? _setWindowFocusCallback(WindowHandle* window, WindowFocusCallback? callback);
    public static WindowFocusCallback? SetWindowFocusCallback(WindowHandle* window, WindowFocusCallback callback) =>
        _setWindowFocusCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowIconifyCallback")]
    private static extern WindowIconifyCallback? _setWindowIconifyCallback(WindowHandle* window, WindowIconifyCallback? callback);
    public static WindowIconifyCallback? SetWindowIconifyCallback(WindowHandle* window, WindowIconifyCallback callback) =>
        _setWindowIconifyCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowMaximizeCallback")]
    private static extern WindowMaximizeCallback? _setWindowMaximizeCallback(WindowHandle* window, WindowMaximizeCallback? callback);
    public static WindowMaximizeCallback? SetWindowMaximizeCallback(WindowHandle* window, WindowMaximizeCallback callback) =>
        _setWindowMaximizeCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetFramebufferSizeCallback")]
    private static extern FramebufferSizeCallback? _setFramebufferSizeCallback(WindowHandle* window, FramebufferSizeCallback? callback);
    public static FramebufferSizeCallback? SetFramebufferSizeCallback(WindowHandle* window, FramebufferSizeCallback callback) =>
        _setFramebufferSizeCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetWindowContentScaleCallback")]
    private static extern WindowContentScaleCallback? _setWindowContentScaleCallback(WindowHandle* window, WindowContentScaleCallback? callback);
    public static WindowContentScaleCallback? SetWindowContentScaleCallback(WindowHandle* window, WindowContentScaleCallback callback) =>
        _setWindowContentScaleCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetKeyCallback")]
    private static extern KeyCallback? _setKeyCallback(WindowHandle* window, KeyCallback? callback);
    public static KeyCallback? SetKeyCallback(WindowHandle* window, KeyCallback callback) =>
        _setKeyCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetCharCallback")]
    private static extern CharCallback? _setCharCallback(WindowHandle* window, CharCallback? callback);
    public static CharCallback? SetCharCallback(WindowHandle* window, CharCallback callback) =>
        _setCharCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetCharModsCallback")]
    private static extern CharModsCallback? _setCharModsCallback(WindowHandle* window, CharModsCallback? callback);
    public static CharModsCallback? SetCharModsCallback(WindowHandle* window, CharModsCallback callback) =>
        _setCharModsCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetMouseButtonCallback")]
    private static extern MouseButtonCallback? _setMouseButtonCallback(WindowHandle* window, MouseButtonCallback? callback);
    public static MouseButtonCallback? SetMouseButtonCallback(WindowHandle* window, MouseButtonCallback callback) =>
        _setMouseButtonCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetCursorPosCallback")]
    private static extern CursorPosCallback? _setCursorPosCallback(WindowHandle* window, CursorPosCallback? callback);
    public static CursorPosCallback? SetCursorPosCallback(WindowHandle* window, CursorPosCallback callback) =>
        _setCursorPosCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetCursorEnterCallback")]
    private static extern CursorEnterCallback? _setCursorEnterCallback(WindowHandle* window, CursorEnterCallback? callback);
    public static CursorEnterCallback? SetCursorEnterCallback(WindowHandle* window, CursorEnterCallback callback) =>
        _setCursorEnterCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetScrollCallback")]
    private static extern ScrollCallback? _setScrollCallback(WindowHandle* window, ScrollCallback? callback);
    public static ScrollCallback? SetScrollCallback(WindowHandle* window, ScrollCallback callback) =>
        _setScrollCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetDropCallback")]
    private static extern DropCallback? _setDropCallback(WindowHandle* window, DropCallback? callback);
    public static DropCallback? SetDropCallback(WindowHandle* window, DropCallback callback) =>
        _setDropCallback(window, callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetMonitorCallback")]
    private static extern MonitorCallback? _setMonitorCallback(MonitorCallback? callback);
    public static MonitorCallback? SetMonitorCallback(MonitorCallback callback) =>
        _setMonitorCallback(callback);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetJoystickCallback")]
    private static extern JoystickCallback? _setJoystickCallback(JoystickCallback? callback);
    public static JoystickCallback? SetJoystickCallback(JoystickCallback callback) =>
        _setJoystickCallback(callback);

    // ── Cursor ──────────────────────────────────────────────

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwCreateCursor")]
    private static extern Cursor* _createCursor(Image* image, int xhot, int yhot);
    public static Cursor* CreateCursor(Image* image, int xhot, int yhot) => _createCursor(image, xhot, yhot);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwCreateStandardCursor")]
    private static extern Cursor* _createStandardCursor(CursorShape shape);
    public static Cursor* CreateStandardCursor(CursorShape shape) => _createStandardCursor(shape);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwDestroyCursor")]
    private static extern void _destroyCursor(Cursor* cursor);
    public static void DestroyCursor(Cursor* cursor) => _destroyCursor(cursor);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetCursor")]
    private static extern void _setCursor(WindowHandle* window, Cursor* cursor);
    public static void SetCursor(WindowHandle* window, Cursor* cursor) => _setCursor(window, cursor);

    // ── Clipboard ───────────────────────────────────────────

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetClipboardString")]
    private static extern nint _getClipboardString(WindowHandle* window);
    public static string? GetClipboardString(WindowHandle* window) => ReadUtf8(_getClipboardString(window));

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetClipboardString")]
    private static extern void _setClipboardString(WindowHandle* window, [MarshalAs(UnmanagedType.LPUTF8Str)] string text);
    public static void SetClipboardString(WindowHandle* window, string text) => _setClipboardString(window, text);

    // ── Context ─────────────────────────────────────────────

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwMakeContextCurrent")]
    private static extern void _makeContextCurrent(WindowHandle* window);
    public static void MakeContextCurrent(WindowHandle* window) => _makeContextCurrent(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSwapBuffers")]
    private static extern void _swapBuffers(WindowHandle* window);
    public static void SwapBuffers(WindowHandle* window) => _swapBuffers(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSwapInterval")]
    private static extern void _swapInterval(int interval);
    public static void SwapInterval(int interval) => _swapInterval(interval);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetProcAddress")]
    private static extern nint _getProcAddress([MarshalAs(UnmanagedType.LPUTF8Str)] string procname);
    public static nint GetProcAddress(string procname) => _getProcAddress(procname);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwExtensionSupported")]
    private static extern int _extensionSupported([MarshalAs(UnmanagedType.LPUTF8Str)] string extension);
    public static bool ExtensionSupported(string extension) => _extensionSupported(extension) == 1;

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetCurrentContext")]
    private static extern WindowHandle* _getCurrentContext();
    public static WindowHandle* GetCurrentContext() => _getCurrentContext();

    // ── Monitor ─────────────────────────────────────────────

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetMonitors")]
    private static extern Monitor** _getMonitors(out int count);
    public static Monitor** GetMonitors(out int count) => _getMonitors(out count);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetPrimaryMonitor")]
    private static extern Monitor* _getPrimaryMonitor();
    public static Monitor* GetPrimaryMonitor() => _getPrimaryMonitor();

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetMonitorPos")]
    private static extern void _getMonitorPos(Monitor* monitor, out int xpos, out int ypos);
    public static void GetMonitorPos(Monitor* monitor, out int xpos, out int ypos) => _getMonitorPos(monitor, out xpos, out ypos);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetMonitorWorkarea")]
    private static extern void _getMonitorWorkarea(Monitor* monitor, out int xpos, out int ypos, out int width, out int height);
    public static void GetMonitorWorkarea(Monitor* monitor, out int xpos, out int ypos, out int width, out int height) =>
        _getMonitorWorkarea(monitor, out xpos, out ypos, out width, out height);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetMonitorPhysicalSize")]
    private static extern void _getMonitorPhysicalSize(Monitor* monitor, out int widthMM, out int heightMM);
    public static void GetMonitorPhysicalSize(Monitor* monitor, out int widthMM, out int heightMM) => _getMonitorPhysicalSize(monitor, out widthMM, out heightMM);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetMonitorContentScale")]
    private static extern void _getMonitorContentScale(Monitor* monitor, out float xscale, out float yscale);
    public static void GetMonitorContentScale(Monitor* monitor, out float xscale, out float yscale) => _getMonitorContentScale(monitor, out xscale, out yscale);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetMonitorName")]
    private static extern nint _getMonitorName(Monitor* monitor);
    public static string? GetMonitorName(Monitor* monitor) => ReadUtf8(_getMonitorName(monitor));

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetVideoMode")]
    private static extern VideoMode* _getVideoMode(Monitor* monitor);
    public static VideoMode* GetVideoMode(Monitor* monitor) => _getVideoMode(monitor);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetVideoModes")]
    private static extern VideoMode* _getVideoModes(Monitor* monitor, out int count);
    public static VideoMode* GetVideoModes(Monitor* monitor, out int count) => _getVideoModes(monitor, out count);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetGamma")]
    private static extern void _setGamma(Monitor* monitor, float gamma);
    public static void SetGamma(Monitor* monitor, float gamma) => _setGamma(monitor, gamma);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetGammaRamp")]
    private static extern GammaRamp* _getGammaRamp(Monitor* monitor);
    public static GammaRamp* GetGammaRamp(Monitor* monitor) => _getGammaRamp(monitor);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetGammaRamp")]
    private static extern void _setGammaRamp(Monitor* monitor, GammaRamp* ramp);
    public static void SetGammaRamp(Monitor* monitor, GammaRamp* ramp) => _setGammaRamp(monitor, ramp);

    // ── Time ────────────────────────────────────────────────

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetTime")]
    private static extern double _getTime();
    public static double GetTime() => _getTime();

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwSetTime")]
    private static extern void _setTime(double time);
    public static void SetTime(double time) => _setTime(time);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetTimerValue")]
    private static extern ulong _getTimerValue();
    public static ulong GetTimerValue() => _getTimerValue();

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetTimerFrequency")]
    private static extern ulong _getTimerFrequency();
    public static ulong GetTimerFrequency() => _getTimerFrequency();

    // ── Native handles ──────────────────────────────────────

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetWin32Window")]
    private static extern nint _getWin32Window(WindowHandle* window);
    public static nint GetWin32Window(WindowHandle* window) => _getWin32Window(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetCocoaWindow")]
    private static extern nint _getCocoaWindow(WindowHandle* window);
    public static nint GetCocoaWindow(WindowHandle* window) => _getCocoaWindow(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetX11Window")]
    private static extern nint _getX11Window(WindowHandle* window);
    public static nint GetX11Window(WindowHandle* window) => _getX11Window(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetX11Display")]
    private static extern nint _getX11Display();
    public static nint GetX11Display() => _getX11Display();

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetWaylandWindow")]
    private static extern nint _getWaylandWindow(WindowHandle* window);
    public static nint GetWaylandWindow(WindowHandle* window) => _getWaylandWindow(window);

    [DllImport(_libraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "glfwGetWaylandDisplay")]
    private static extern nint _getWaylandDisplay();
    public static nint GetWaylandDisplay() => _getWaylandDisplay();
}
