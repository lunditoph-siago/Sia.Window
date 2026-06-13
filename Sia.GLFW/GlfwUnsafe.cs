using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Sia.Input;

namespace Sia.GLFW;

public static unsafe partial class GlfwUnsafe
{
#if BROWSER
    private const string _libraryName = "__Internal_emscripten";
#else
    private const string _libraryName = "glfw3";

    private static nint _loadedLibrary;

    static GlfwUnsafe()
    {
        NativeLibrary.SetDllImportResolver(
            typeof(GlfwUnsafe).Assembly,
            static (name, assembly, searchPath) => {
                if (name != _libraryName) return 0;

                ReadOnlySpan<string> candidates =
                    ["glfw3", "glfw", "libglfw.so.3", "libglfw.3.dylib"];
                foreach (var candidate in candidates) {
                    if (NativeLibrary.TryLoad(
                            candidate,
                            assembly,
                            DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories,
                            out nint handle)) {
                        Volatile.Write(ref _loadedLibrary, handle);
                        return handle;
                    }
                }

                return 0;
            });
    }

    internal static bool HasExport(string symbol)
    {
        var library = Volatile.Read(ref _loadedLibrary);
        return library != 0 && NativeLibrary.TryGetExport(library, symbol, out _);
    }
#endif

    private static string? ReadUtf8(nint pointer) => Marshal.PtrToStringUTF8(pointer);

    private static readonly ConcurrentDictionary<(nint Window, string Slot), Delegate> _rootedCallbacks = new();

    private static TCallback? SetWindowCallback<TCallback>(
        WindowHandle* window, string slot, TCallback? callback,
        delegate*<WindowHandle*, nint, nint> setter, nint trampoline)
        where TCallback : Delegate
    {
        var key = ((nint)window, slot);
        _rootedCallbacks.TryGetValue(key, out var rootedPrevious);

        if (callback is null) {
            _rootedCallbacks.TryRemove(key, out _);
        } else {
            _rootedCallbacks[key] = callback;
        }

        setter(window, callback is null ? 0 : trampoline);
        return rootedPrevious as TCallback;
    }

    private static TCallback? SetGlobalCallback<TCallback>(
        string slot, TCallback? callback, delegate*<nint, nint> setter, nint trampoline)
        where TCallback : Delegate
    {
        var key = ((nint)0, slot);
        _rootedCallbacks.TryGetValue(key, out var rootedPrevious);

        if (callback is null) {
            _rootedCallbacks.TryRemove(key, out _);
        } else {
            _rootedCallbacks[key] = callback;
        }

        setter(callback is null ? 0 : trampoline);
        return rootedPrevious as TCallback;
    }

    private static void UnrootWindowCallbacks(nint window)
    {
        foreach (var key in _rootedCallbacks.Keys) {
            if (key.Window == window) {
                _rootedCallbacks.TryRemove(key, out _);
            }
        }
    }

    private static void UnrootTerminatedCallbacks()
    {
        foreach (var key in _rootedCallbacks.Keys) {
            if (key.Window != 0 || key.Slot != nameof(SetErrorCallback)) {
                _rootedCallbacks.TryRemove(key, out _);
            }
        }
    }

    // Error handling

#if !BROWSER

    [LibraryImport(_libraryName, EntryPoint = "glfwGetError")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _getError(out nint description);
    public static (ErrorCode Code, string? Description) GetError()
    {
        var code = (ErrorCode)_getError(out var descriptionPtr);
        return (code, ReadUtf8(descriptionPtr));
    }

#else

    public static (ErrorCode Code, string? Description) GetError() =>
        (ErrorCode.PlatformError, "glfwGetError is not implemented by Emscripten's GLFW port.");

#endif

    // Lifecycle

    [LibraryImport(_libraryName, EntryPoint = "glfwInit")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _init();
    public static bool Init() => _init() == 1;

    [LibraryImport(_libraryName, EntryPoint = "glfwTerminate")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _terminate();
    public static void Terminate()
    {
        try {
            _terminate();
        } finally {
            UnrootTerminatedCallbacks();
        }
    }

    [LibraryImport(_libraryName, EntryPoint = "glfwInitHint")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _initHint(int hint, int value);
    public static void InitHint(int hint, int value) => _initHint(hint, value);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetVersion")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _getVersion(out int major, out int minor, out int rev);
    public static void GetVersion(out int major, out int minor, out int rev) => _getVersion(out major, out minor, out rev);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetVersionString")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getVersionString();
    public static string GetVersionString() => ReadUtf8(_getVersionString()) ?? string.Empty;

    // Window

    [LibraryImport(_libraryName, EntryPoint = "glfwCreateWindow", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial WindowHandle* _createWindow(int width, int height, string title, Monitor* monitor, WindowHandle* share);
    public static WindowHandle* CreateWindow(int width, int height, string title, Monitor* monitor, WindowHandle* share) =>
        _createWindow(width, height, title, monitor, share);

    [LibraryImport(_libraryName, EntryPoint = "glfwDestroyWindow")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _destroyWindow(WindowHandle* window);
    public static void DestroyWindow(WindowHandle* window)
    {
        _destroyWindow(window);
        UnrootWindowCallbacks((nint)window);
    }

    [LibraryImport(_libraryName, EntryPoint = "glfwWindowShouldClose")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _windowShouldClose(WindowHandle* window);
    public static bool WindowShouldClose(WindowHandle* window) => _windowShouldClose(window) == 1;

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowShouldClose")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setWindowShouldClose(WindowHandle* window, int value);
    public static void SetWindowShouldClose(WindowHandle* window, bool value) => _setWindowShouldClose(window, value ? 1 : 0);

    [LibraryImport(_libraryName, EntryPoint = "glfwWindowHint")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _windowHint(int hint, int value);
    public static void WindowHint(int hint, int value) => _windowHint(hint, value);
    public static void WindowHint(WindowHintClientApi hint, int value) => _windowHint((int)hint, value);
    public static void WindowHint(WindowHintClientApi hint, ClientApi value) => _windowHint((int)hint, (int)value);
    public static void WindowHint(WindowHintClientApi hint, ContextCreationApi value) => _windowHint((int)hint, (int)value);
    public static void WindowHint(WindowHintClientApi hint, ContextRobustness value) => _windowHint((int)hint, (int)value);
    public static void WindowHint(WindowHintClientApi hint, OpenGlProfile value) => _windowHint((int)hint, (int)value);
    public static void WindowHint(WindowHintClientApi hint, ReleaseBehavior value) => _windowHint((int)hint, (int)value);
    public static void WindowHint(WindowHintBool hint, bool value) => _windowHint((int)hint, value ? 1 : 0);
    public static void WindowHint(WindowHintInt hint, int value) => _windowHint((int)hint, value);
    public static void WindowHint(WindowHintString hint, string value) => _windowHintString((int)hint, value);

    [LibraryImport(_libraryName, EntryPoint = "glfwWindowHintString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _windowHintString(int hint, string value);

    [LibraryImport(_libraryName, EntryPoint = "glfwDefaultWindowHints")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _defaultWindowHints();
    public static void DefaultWindowHints() => _defaultWindowHints();

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowTitle", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setWindowTitle(WindowHandle* window, string title);
    public static void SetWindowTitle(WindowHandle* window, string title) => _setWindowTitle(window, title);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetWindowSize")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _getWindowSize(WindowHandle* window, out int width, out int height);
    public static void GetWindowSize(WindowHandle* window, out int width, out int height) => _getWindowSize(window, out width, out height);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowSize")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setWindowSize(WindowHandle* window, int width, int height);
    public static void SetWindowSize(WindowHandle* window, int width, int height) => _setWindowSize(window, width, height);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetFramebufferSize")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _getFramebufferSize(WindowHandle* window, out int width, out int height);
    public static void GetFramebufferSize(WindowHandle* window, out int width, out int height) => _getFramebufferSize(window, out width, out height);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowPos")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setWindowPos(WindowHandle* window, int x, int y);
    public static void SetWindowPos(WindowHandle* window, int x, int y) => _setWindowPos(window, x, y);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetWindowPos")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _getWindowPos(WindowHandle* window, out int x, out int y);
    public static void GetWindowPos(WindowHandle* window, out int x, out int y) => _getWindowPos(window, out x, out y);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowSizeLimits")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setWindowSizeLimits(WindowHandle* window, int minWidth, int minHeight, int maxWidth, int maxHeight);
    public static void SetWindowSizeLimits(WindowHandle* window, int minWidth, int minHeight, int maxWidth, int maxHeight) =>
        _setWindowSizeLimits(window, minWidth, minHeight, maxWidth, maxHeight);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowAspectRatio")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setWindowAspectRatio(WindowHandle* window, int numer, int denom);
    public static void SetWindowAspectRatio(WindowHandle* window, int numer, int denom) => _setWindowAspectRatio(window, numer, denom);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetWindowOpacity")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial float _getWindowOpacity(WindowHandle* window);
    public static float GetWindowOpacity(WindowHandle* window) => _getWindowOpacity(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowOpacity")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setWindowOpacity(WindowHandle* window, float opacity);
    public static void SetWindowOpacity(WindowHandle* window, float opacity) => _setWindowOpacity(window, opacity);

    [LibraryImport(_libraryName, EntryPoint = "glfwIconifyWindow")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _iconifyWindow(WindowHandle* window);
    public static void IconifyWindow(WindowHandle* window) => _iconifyWindow(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwRestoreWindow")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _restoreWindow(WindowHandle* window);
    public static void RestoreWindow(WindowHandle* window) => _restoreWindow(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwMaximizeWindow")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _maximizeWindow(WindowHandle* window);
    public static void MaximizeWindow(WindowHandle* window) => _maximizeWindow(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwShowWindow")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _showWindow(WindowHandle* window);
    public static void ShowWindow(WindowHandle* window) => _showWindow(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwHideWindow")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _hideWindow(WindowHandle* window);
    public static void HideWindow(WindowHandle* window) => _hideWindow(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwFocusWindow")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _focusWindow(WindowHandle* window);
    public static void FocusWindow(WindowHandle* window) => _focusWindow(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwRequestWindowAttention")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _requestWindowAttention(WindowHandle* window);
    public static void RequestWindowAttention(WindowHandle* window) => _requestWindowAttention(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetWindowMonitor")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial Monitor* _getWindowMonitor(WindowHandle* window);
    public static Monitor* GetWindowMonitor(WindowHandle* window) => _getWindowMonitor(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowMonitor")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setWindowMonitor(WindowHandle* window, Monitor* monitor, int xpos, int ypos, int width, int height, int refreshRate);
    public static void SetWindowMonitor(WindowHandle* window, Monitor* monitor, int xpos, int ypos, int width, int height, int refreshRate) =>
        _setWindowMonitor(window, monitor, xpos, ypos, width, height, refreshRate);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetWindowAttrib")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _getWindowAttrib(WindowHandle* window, int attrib);
    public static int GetWindowAttrib(WindowHandle* window, int attrib) => _getWindowAttrib(window, attrib);
    public static int GetWindowAttrib(WindowHandle* window, WindowAttribute attrib) =>
        _getWindowAttrib(window, (int)attrib);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowAttrib")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setWindowAttrib(WindowHandle* window, int attrib, int value);
    public static void SetWindowAttrib(WindowHandle* window, int attrib, int value) => _setWindowAttrib(window, attrib, value);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetWindowFrameSize")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _getWindowFrameSize(
        WindowHandle* window, out int left, out int top, out int right, out int bottom);
    public static void GetWindowFrameSize(
        WindowHandle* window, out int left, out int top, out int right, out int bottom) =>
        _getWindowFrameSize(window, out left, out top, out right, out bottom);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetWindowContentScale")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _getWindowContentScale(WindowHandle* window, out float xscale, out float yscale);
    public static void GetWindowContentScale(WindowHandle* window, out float xscale, out float yscale) =>
        _getWindowContentScale(window, out xscale, out yscale);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowUserPointer")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setWindowUserPointer(WindowHandle* window, nint pointer);
    public static void SetWindowUserPointer(WindowHandle* window, nint pointer) => _setWindowUserPointer(window, pointer);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetWindowUserPointer")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getWindowUserPointer(WindowHandle* window);
    public static nint GetWindowUserPointer(WindowHandle* window) => _getWindowUserPointer(window);

    // Input

    [LibraryImport(_libraryName, EntryPoint = "glfwPollEvents")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _pollEvents();
    public static void PollEvents() => _pollEvents();

    [LibraryImport(_libraryName, EntryPoint = "glfwWaitEvents")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _waitEvents();
    public static void WaitEvents() => _waitEvents();

    [LibraryImport(_libraryName, EntryPoint = "glfwWaitEventsTimeout")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _waitEventsTimeout(double timeout);
    public static void WaitEventsTimeout(double timeout) => _waitEventsTimeout(timeout);

    [LibraryImport(_libraryName, EntryPoint = "glfwPostEmptyEvent")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _postEmptyEvent();
    public static void PostEmptyEvent() => _postEmptyEvent();

    [LibraryImport(_libraryName, EntryPoint = "glfwGetKey")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _getKey(WindowHandle* window, int key);
    public static InputAction GetKey(WindowHandle* window, Key key) => (InputAction)_getKey(window, (int)key);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetMouseButton")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _getMouseButton(WindowHandle* window, int button);
    public static InputAction GetMouseButton(WindowHandle* window, MouseButton button) => (InputAction)_getMouseButton(window, (int)button);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetCursorPos")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _getCursorPos(WindowHandle* window, out double xpos, out double ypos);
    public static void GetCursorPos(WindowHandle* window, out double xpos, out double ypos) => _getCursorPos(window, out xpos, out ypos);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetCursorPos")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setCursorPos(WindowHandle* window, double xpos, double ypos);
    public static void SetCursorPos(WindowHandle* window, double xpos, double ypos) => _setCursorPos(window, xpos, ypos);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetKeyName")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getKeyName(int key, int scancode);
    public static string? GetKeyName(Key key, int scancode) => ReadUtf8(_getKeyName((int)key, scancode));

    [LibraryImport(_libraryName, EntryPoint = "glfwGetKeyScancode")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _getKeyScancode(int key);
    public static int GetKeyScancode(Key key) => _getKeyScancode((int)key);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetInputMode")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _getInputMode(WindowHandle* window, int mode);
    public static int GetInputMode(WindowHandle* window, InputMode mode) => _getInputMode(window, (int)mode);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetInputMode")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setInputMode(WindowHandle* window, int mode, int value);
    public static void SetInputMode(WindowHandle* window, InputMode mode, int value) => _setInputMode(window, (int)mode, value);

    [LibraryImport(_libraryName, EntryPoint = "glfwRawMouseMotionSupported")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _rawMouseMotionSupported();
    public static bool RawMouseMotionSupported() => _rawMouseMotionSupported() == 1;

    [LibraryImport(_libraryName, EntryPoint = "glfwJoystickPresent")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _joystickPresent(int jid);
    public static bool JoystickPresent(int jid) => _joystickPresent(jid) == 1;

    [LibraryImport(_libraryName, EntryPoint = "glfwGetJoystickAxes")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial float* _getJoystickAxes(int jid, out int count);
    public static float* GetJoystickAxes(int jid, out int count) => _getJoystickAxes(jid, out count);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetJoystickButtons")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial byte* _getJoystickButtons(int jid, out int count);
    public static byte* GetJoystickButtons(int jid, out int count) => _getJoystickButtons(jid, out count);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetJoystickHats")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial byte* _getJoystickHats(int jid, out int count);
    public static byte* GetJoystickHats(int jid, out int count) => _getJoystickHats(jid, out count);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetJoystickName")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getJoystickName(int jid);
    public static string? GetJoystickName(int jid) => ReadUtf8(_getJoystickName(jid));

    [LibraryImport(_libraryName, EntryPoint = "glfwJoystickIsGamepad")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _joystickIsGamepad(int jid);
    public static bool JoystickIsGamepad(int jid) => _joystickIsGamepad(jid) == 1;

    [LibraryImport(_libraryName, EntryPoint = "glfwGetGamepadState")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _getGamepadState(int jid, GamepadState* state);
    public static bool GetGamepadState(int jid, out GamepadState state)
    {
        state = default;
        fixed (GamepadState* statePtr = &state) {
            return _getGamepadState(jid, statePtr) == 1;
        }
    }

    [LibraryImport(_libraryName, EntryPoint = "glfwUpdateGamepadMappings", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _updateGamepadMappings(string mappings);
    public static bool UpdateGamepadMappings(string mappings) => _updateGamepadMappings(mappings) == 1;

    [LibraryImport(_libraryName, EntryPoint = "glfwGetGamepadName")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getGamepadName(int jid);
    public static string? GetGamepadName(int jid) => ReadUtf8(_getGamepadName(jid));

    // Callbacks

    [LibraryImport(_libraryName, EntryPoint = "glfwSetErrorCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setErrorCallback(nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _errorCallbackThunk(int error, nint description)
    {
        if (_rootedCallbacks.TryGetValue((0, nameof(SetErrorCallback)), out var callback)) {
            ((ErrorCallback)callback)((ErrorCode)error, ReadUtf8(description) ?? string.Empty);
        }
    }
    public static ErrorCallback? SetErrorCallback(ErrorCallback? callback) =>
        SetGlobalCallback(nameof(SetErrorCallback), callback, &_setErrorCallback,
            (nint)(delegate* unmanaged[Cdecl]<int, nint, void>)&_errorCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowPosCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setWindowPosCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _windowPosCallbackThunk(WindowHandle* window, int x, int y)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetWindowPosCallback)), out var callback)) {
            ((WindowPosCallback)callback)(window, x, y);
        }
    }
    public static WindowPosCallback? SetWindowPosCallback(WindowHandle* window, WindowPosCallback? callback) =>
        SetWindowCallback(window, nameof(SetWindowPosCallback), callback, &_setWindowPosCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, int, int, void>)&_windowPosCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowSizeCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setWindowSizeCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _windowSizeCallbackThunk(WindowHandle* window, int width, int height)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetWindowSizeCallback)), out var callback)) {
            ((WindowSizeCallback)callback)(window, width, height);
        }
    }
    public static WindowSizeCallback? SetWindowSizeCallback(WindowHandle* window, WindowSizeCallback? callback) =>
        SetWindowCallback(window, nameof(SetWindowSizeCallback), callback, &_setWindowSizeCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, int, int, void>)&_windowSizeCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowCloseCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setWindowCloseCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _windowCloseCallbackThunk(WindowHandle* window)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetWindowCloseCallback)), out var callback)) {
            ((WindowCloseCallback)callback)(window);
        }
    }
    public static WindowCloseCallback? SetWindowCloseCallback(WindowHandle* window, WindowCloseCallback? callback) =>
        SetWindowCallback(window, nameof(SetWindowCloseCallback), callback, &_setWindowCloseCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, void>)&_windowCloseCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowRefreshCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setWindowRefreshCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _windowRefreshCallbackThunk(WindowHandle* window)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetWindowRefreshCallback)), out var callback)) {
            ((WindowRefreshCallback)callback)(window);
        }
    }
    public static WindowRefreshCallback? SetWindowRefreshCallback(WindowHandle* window, WindowRefreshCallback? callback) =>
        SetWindowCallback(window, nameof(SetWindowRefreshCallback), callback, &_setWindowRefreshCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, void>)&_windowRefreshCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowFocusCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setWindowFocusCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _windowFocusCallbackThunk(WindowHandle* window, int focused)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetWindowFocusCallback)), out var callback)) {
            ((WindowFocusCallback)callback)(window, focused != 0);
        }
    }
    public static WindowFocusCallback? SetWindowFocusCallback(WindowHandle* window, WindowFocusCallback? callback) =>
        SetWindowCallback(window, nameof(SetWindowFocusCallback), callback, &_setWindowFocusCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, int, void>)&_windowFocusCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowIconifyCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setWindowIconifyCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _windowIconifyCallbackThunk(WindowHandle* window, int iconified)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetWindowIconifyCallback)), out var callback)) {
            ((WindowIconifyCallback)callback)(window, iconified != 0);
        }
    }
    public static WindowIconifyCallback? SetWindowIconifyCallback(WindowHandle* window, WindowIconifyCallback? callback) =>
        SetWindowCallback(window, nameof(SetWindowIconifyCallback), callback, &_setWindowIconifyCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, int, void>)&_windowIconifyCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowMaximizeCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setWindowMaximizeCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _windowMaximizeCallbackThunk(WindowHandle* window, int maximized)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetWindowMaximizeCallback)), out var callback)) {
            ((WindowMaximizeCallback)callback)(window, maximized != 0);
        }
    }
    public static WindowMaximizeCallback? SetWindowMaximizeCallback(WindowHandle* window, WindowMaximizeCallback? callback) =>
        SetWindowCallback(window, nameof(SetWindowMaximizeCallback), callback, &_setWindowMaximizeCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, int, void>)&_windowMaximizeCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetFramebufferSizeCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setFramebufferSizeCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _framebufferSizeCallbackThunk(WindowHandle* window, int width, int height)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetFramebufferSizeCallback)), out var callback)) {
            ((FramebufferSizeCallback)callback)(window, width, height);
        }
    }
    public static FramebufferSizeCallback? SetFramebufferSizeCallback(WindowHandle* window, FramebufferSizeCallback? callback) =>
        SetWindowCallback(window, nameof(SetFramebufferSizeCallback), callback, &_setFramebufferSizeCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, int, int, void>)&_framebufferSizeCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetWindowContentScaleCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setWindowContentScaleCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _windowContentScaleCallbackThunk(WindowHandle* window, float xscale, float yscale)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetWindowContentScaleCallback)), out var callback)) {
            ((WindowContentScaleCallback)callback)(window, xscale, yscale);
        }
    }
    public static WindowContentScaleCallback? SetWindowContentScaleCallback(WindowHandle* window, WindowContentScaleCallback? callback) =>
        SetWindowCallback(window, nameof(SetWindowContentScaleCallback), callback, &_setWindowContentScaleCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, float, float, void>)&_windowContentScaleCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetKeyCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setKeyCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _keyCallbackThunk(WindowHandle* window, int key, int scancode, int action, int mods)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetKeyCallback)), out var callback)) {
            ((KeyCallback)callback)(window, (Key)key, scancode, (InputAction)action, (KeyModifiers)mods);
        }
    }
    public static KeyCallback? SetKeyCallback(WindowHandle* window, KeyCallback? callback) =>
        SetWindowCallback(window, nameof(SetKeyCallback), callback, &_setKeyCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, int, int, int, int, void>)&_keyCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetCharCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setCharCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _charCallbackThunk(WindowHandle* window, uint codepoint)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetCharCallback)), out var callback)) {
            ((CharCallback)callback)(window, codepoint);
        }
    }
    public static CharCallback? SetCharCallback(WindowHandle* window, CharCallback? callback) =>
        SetWindowCallback(window, nameof(SetCharCallback), callback, &_setCharCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, uint, void>)&_charCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetMouseButtonCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setMouseButtonCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _mouseButtonCallbackThunk(WindowHandle* window, int button, int action, int mods)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetMouseButtonCallback)), out var callback)) {
            ((MouseButtonCallback)callback)(window, (MouseButton)button, (InputAction)action, (KeyModifiers)mods);
        }
    }
    public static MouseButtonCallback? SetMouseButtonCallback(WindowHandle* window, MouseButtonCallback? callback) =>
        SetWindowCallback(window, nameof(SetMouseButtonCallback), callback, &_setMouseButtonCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, int, int, int, void>)&_mouseButtonCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetCursorPosCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setCursorPosCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _cursorPosCallbackThunk(WindowHandle* window, double x, double y)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetCursorPosCallback)), out var callback)) {
            ((CursorPosCallback)callback)(window, x, y);
        }
    }
    public static CursorPosCallback? SetCursorPosCallback(WindowHandle* window, CursorPosCallback? callback) =>
        SetWindowCallback(window, nameof(SetCursorPosCallback), callback, &_setCursorPosCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, double, double, void>)&_cursorPosCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetCursorEnterCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setCursorEnterCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _cursorEnterCallbackThunk(WindowHandle* window, int entered)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetCursorEnterCallback)), out var callback)) {
            ((CursorEnterCallback)callback)(window, entered != 0);
        }
    }
    public static CursorEnterCallback? SetCursorEnterCallback(WindowHandle* window, CursorEnterCallback? callback) =>
        SetWindowCallback(window, nameof(SetCursorEnterCallback), callback, &_setCursorEnterCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, int, void>)&_cursorEnterCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetScrollCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setScrollCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _scrollCallbackThunk(WindowHandle* window, double xoffset, double yoffset)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetScrollCallback)), out var callback)) {
            ((ScrollCallback)callback)(window, xoffset, yoffset);
        }
    }
    public static ScrollCallback? SetScrollCallback(WindowHandle* window, ScrollCallback? callback) =>
        SetWindowCallback(window, nameof(SetScrollCallback), callback, &_setScrollCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, double, double, void>)&_scrollCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetDropCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setDropCallback(WindowHandle* window, nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _dropCallbackThunk(WindowHandle* window, int count, byte** paths)
    {
        if (_rootedCallbacks.TryGetValue(((nint)window, nameof(SetDropCallback)), out var callback)) {
            ((DropCallback)callback)(window, count, paths);
        }
    }
    public static DropCallback? SetDropCallback(WindowHandle* window, DropCallback? callback) =>
        SetWindowCallback(window, nameof(SetDropCallback), callback, &_setDropCallback,
            (nint)(delegate* unmanaged[Cdecl]<WindowHandle*, int, byte**, void>)&_dropCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetMonitorCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setMonitorCallback(nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _monitorCallbackThunk(Monitor* monitor, int state)
    {
        if (_rootedCallbacks.TryGetValue((0, nameof(SetMonitorCallback)), out var callback)) {
            ((MonitorCallback)callback)(monitor, (ConnectedState)state);
        }
    }
    public static MonitorCallback? SetMonitorCallback(MonitorCallback? callback) =>
        SetGlobalCallback(nameof(SetMonitorCallback), callback, &_setMonitorCallback,
            (nint)(delegate* unmanaged[Cdecl]<Monitor*, int, void>)&_monitorCallbackThunk);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetJoystickCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _setJoystickCallback(nint callback);
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void _joystickCallbackThunk(int jid, int state)
    {
        if (_rootedCallbacks.TryGetValue((0, nameof(SetJoystickCallback)), out var callback)) {
            ((JoystickCallback)callback)(jid, (ConnectedState)state);
        }
    }
    public static JoystickCallback? SetJoystickCallback(JoystickCallback? callback) =>
        SetGlobalCallback(nameof(SetJoystickCallback), callback, &_setJoystickCallback,
            (nint)(delegate* unmanaged[Cdecl]<int, int, void>)&_joystickCallbackThunk);

    // Cursor

    [LibraryImport(_libraryName, EntryPoint = "glfwCreateCursor")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial Cursor* _createCursor(Image* image, int xhot, int yhot);
    public static Cursor* CreateCursor(Image* image, int xhot, int yhot) => _createCursor(image, xhot, yhot);

    [LibraryImport(_libraryName, EntryPoint = "glfwCreateStandardCursor")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial Cursor* _createStandardCursor(int shape);
    public static Cursor* CreateStandardCursor(CursorShape shape) => _createStandardCursor((int)shape);

    [LibraryImport(_libraryName, EntryPoint = "glfwDestroyCursor")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _destroyCursor(Cursor* cursor);
    public static void DestroyCursor(Cursor* cursor) => _destroyCursor(cursor);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetCursor")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setCursor(WindowHandle* window, Cursor* cursor);
    public static void SetCursor(WindowHandle* window, Cursor* cursor) => _setCursor(window, cursor);

    // Clipboard

    [LibraryImport(_libraryName, EntryPoint = "glfwGetClipboardString")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getClipboardString(WindowHandle* window);
    public static string? GetClipboardString(WindowHandle* window) => ReadUtf8(_getClipboardString(window));

    [LibraryImport(_libraryName, EntryPoint = "glfwSetClipboardString", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setClipboardString(WindowHandle* window, string text);
    public static void SetClipboardString(WindowHandle* window, string text) => _setClipboardString(window, text);

    // Context

    [LibraryImport(_libraryName, EntryPoint = "glfwMakeContextCurrent")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _makeContextCurrent(WindowHandle* window);
    public static void MakeContextCurrent(WindowHandle* window) => _makeContextCurrent(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwSwapBuffers")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _swapBuffers(WindowHandle* window);
    public static void SwapBuffers(WindowHandle* window) => _swapBuffers(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwSwapInterval")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _swapInterval(int interval);
    public static void SwapInterval(int interval) => _swapInterval(interval);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetProcAddress", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getProcAddress(string procname);
    public static nint GetProcAddress(string procname) => _getProcAddress(procname);

    [LibraryImport(_libraryName, EntryPoint = "glfwExtensionSupported", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int _extensionSupported(string extension);
    public static bool ExtensionSupported(string extension) => _extensionSupported(extension) == 1;

    [LibraryImport(_libraryName, EntryPoint = "glfwGetCurrentContext")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial WindowHandle* _getCurrentContext();
    public static WindowHandle* GetCurrentContext() => _getCurrentContext();

    // Monitor

    [LibraryImport(_libraryName, EntryPoint = "glfwGetMonitors")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial Monitor** _getMonitors(out int count);
    public static Monitor** GetMonitors(out int count) => _getMonitors(out count);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetPrimaryMonitor")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial Monitor* _getPrimaryMonitor();
    public static Monitor* GetPrimaryMonitor() => _getPrimaryMonitor();

    [LibraryImport(_libraryName, EntryPoint = "glfwGetMonitorPos")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _getMonitorPos(Monitor* monitor, out int xpos, out int ypos);
    public static void GetMonitorPos(Monitor* monitor, out int xpos, out int ypos) => _getMonitorPos(monitor, out xpos, out ypos);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetMonitorWorkarea")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _getMonitorWorkarea(Monitor* monitor, out int xpos, out int ypos, out int width, out int height);
    public static void GetMonitorWorkarea(Monitor* monitor, out int xpos, out int ypos, out int width, out int height) =>
        _getMonitorWorkarea(monitor, out xpos, out ypos, out width, out height);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetMonitorPhysicalSize")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _getMonitorPhysicalSize(Monitor* monitor, out int widthMM, out int heightMM);
    public static void GetMonitorPhysicalSize(Monitor* monitor, out int widthMM, out int heightMM) => _getMonitorPhysicalSize(monitor, out widthMM, out heightMM);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetMonitorContentScale")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _getMonitorContentScale(Monitor* monitor, out float xscale, out float yscale);
    public static void GetMonitorContentScale(Monitor* monitor, out float xscale, out float yscale) => _getMonitorContentScale(monitor, out xscale, out yscale);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetMonitorName")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getMonitorName(Monitor* monitor);
    public static string? GetMonitorName(Monitor* monitor) => ReadUtf8(_getMonitorName(monitor));

    [LibraryImport(_libraryName, EntryPoint = "glfwGetVideoMode")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial VideoMode* _getVideoMode(Monitor* monitor);
    public static VideoMode* GetVideoMode(Monitor* monitor) => _getVideoMode(monitor);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetVideoModes")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial VideoMode* _getVideoModes(Monitor* monitor, out int count);
    public static VideoMode* GetVideoModes(Monitor* monitor, out int count) => _getVideoModes(monitor, out count);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetGamma")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setGamma(Monitor* monitor, float gamma);
    public static void SetGamma(Monitor* monitor, float gamma) => _setGamma(monitor, gamma);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetGammaRamp")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial GammaRamp* _getGammaRamp(Monitor* monitor);
    public static GammaRamp* GetGammaRamp(Monitor* monitor) => _getGammaRamp(monitor);

    [LibraryImport(_libraryName, EntryPoint = "glfwSetGammaRamp")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setGammaRamp(Monitor* monitor, GammaRamp* ramp);
    public static void SetGammaRamp(Monitor* monitor, GammaRamp* ramp) => _setGammaRamp(monitor, ramp);

    // Time

    [LibraryImport(_libraryName, EntryPoint = "glfwGetTime")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial double _getTime();
    public static double GetTime() => _getTime();

    [LibraryImport(_libraryName, EntryPoint = "glfwSetTime")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void _setTime(double time);
    public static void SetTime(double time) => _setTime(time);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetTimerValue")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ulong _getTimerValue();
    public static ulong GetTimerValue() => _getTimerValue();

    [LibraryImport(_libraryName, EntryPoint = "glfwGetTimerFrequency")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial ulong _getTimerFrequency();
    public static ulong GetTimerFrequency() => _getTimerFrequency();

    // Native handles

#if !BROWSER

    [LibraryImport(_libraryName, EntryPoint = "glfwGetWin32Window")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getWin32Window(WindowHandle* window);
    public static nint GetWin32Window(WindowHandle* window) => _getWin32Window(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetCocoaWindow")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getCocoaWindow(WindowHandle* window);
    public static nint GetCocoaWindow(WindowHandle* window) => _getCocoaWindow(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetX11Window")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nuint _getX11Window(WindowHandle* window);
    public static nuint GetX11Window(WindowHandle* window) => _getX11Window(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetX11Display")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getX11Display();
    public static nint GetX11Display() => _getX11Display();

    [LibraryImport(_libraryName, EntryPoint = "glfwGetWaylandWindow")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getWaylandWindow(WindowHandle* window);
    public static nint GetWaylandWindow(WindowHandle* window) => _getWaylandWindow(window);

    [LibraryImport(_libraryName, EntryPoint = "glfwGetWaylandDisplay")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint _getWaylandDisplay();
    public static nint GetWaylandDisplay() => _getWaylandDisplay();

#endif
}
