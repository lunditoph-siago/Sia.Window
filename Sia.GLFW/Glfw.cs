using System.Collections.Concurrent;

using Sia.Input;
using Sia.Window;

namespace Sia.GLFW;

public static unsafe class Glfw
{
    private static readonly object _lifetimeLock = new();
    private static int _ownerThreadId;
    private static int _referenceCount;
    private static long _generation;

    private static readonly ConcurrentDictionary<nint, long> _liveWindows = [];

    internal static bool IsInitialized => Volatile.Read(ref _referenceCount) != 0;

    public static void Initialize()
    {
        var threadId = Environment.CurrentManagedThreadId;

        lock (_lifetimeLock) {
            if (_referenceCount != 0 && _ownerThreadId != threadId) {
                throw new InvalidOperationException(
                    "The active GLFW lifetime belongs to another thread.");
            }

            if (_referenceCount == 0) {
                if (!GlfwUnsafe.Init()) {
                    throw new GlfwException($"GLFW initialization failed: {DescribeError(GlfwUnsafe.GetError())}");
                }

                try {
#if !BROWSER
                    GlfwUnsafe.GetVersion(out var major, out var minor, out var revision);
                    if (major != 3 || minor < 3) {
                        throw new GlfwException(
                            $"GLFW 3.3 or newer is required, found {major}.{minor}.{revision}.");
                    }
#endif
                    Volatile.Write(ref _generation, _generation + 1);
                    _ownerThreadId = threadId;
                }
                catch {
                    GlfwUnsafe.Terminate();
                    throw;
                }
            }

            checked {
                Volatile.Write(ref _referenceCount, _referenceCount + 1);
            }
        }
    }

    public static void Terminate()
    {
        var threadId = Environment.CurrentManagedThreadId;

        lock (_lifetimeLock) {
            if (_referenceCount == 0) {
                return;
            }
            if (_ownerThreadId != threadId) {
                throw new InvalidOperationException(
                    "GLFW must be terminated on its initialization thread.");
            }

            if (_referenceCount > 1) {
                Volatile.Write(ref _referenceCount, _referenceCount - 1);
                return;
            }

            GlfwUnsafe.Terminate();

            _liveWindows.Clear();
            _ownerThreadId = 0;
            Volatile.Write(ref _referenceCount, 0);
        }
    }

    public static GlfwWindow CreateWindow(
        in WindowDescriptor descriptor,
        in GlfwWindowOptions options = default)
    {
        VerifyMainThread();
        WindowDescriptor.Validate(in descriptor);
        GlfwWindowOptions.Validate(in options);
        ApplyWindowHints(in descriptor, in options);

        var sharedContext = options.SharedContext.IsNull
            ? null
            : GetMainThreadPointer(options.SharedContext);

        var handle = GlfwUnsafe.CreateWindow(
            descriptor.Width,
            descriptor.Height,
            descriptor.Title,
            (Monitor*)options.Monitor.Handle,
            sharedContext);

        if (handle == null) {
            throw new GlfwException($"GLFW window creation failed: {DescribeError(GlfwUnsafe.GetError())}");
        }

        var window = new GlfwWindow((nint)handle, Volatile.Read(ref _generation));

        if (!_liveWindows.TryAdd(window.Handle, window.Generation)) {
            GlfwUnsafe.DestroyWindow(handle);
            throw new InvalidOperationException(
                "The native window handle is already registered.");
        }

        return window;
    }

    public static void DestroyWindow(ref GlfwWindow window)
    {
        VerifyMainThread();
        if (window.IsNull) {
            return;
        }

        ValidateWindow(window);

        GlfwUnsafe.DestroyWindow((WindowHandle*)window.Handle);

        _liveWindows.TryRemove(window.Handle, out _);
        window = default;
    }

    public static void PollEvents()
    {
        VerifyMainThread();
        GlfwUnsafe.PollEvents();
        GlfwModule.DispatchPendingInputEvents();
    }

    public static void WaitEvents()
    {
        VerifyMainThread();
        GlfwUnsafe.WaitEvents();
        GlfwModule.DispatchPendingInputEvents();
    }

    public static bool ShouldClose(GlfwWindow window) =>
        GlfwUnsafe.WindowShouldClose(GetAnyThreadPointer(window));

    public static void RequestClose(GlfwWindow window) =>
        GlfwUnsafe.SetWindowShouldClose(GetAnyThreadPointer(window), true);

    public static WindowSize GetSize(GlfwWindow window)
    {
        GlfwUnsafe.GetWindowSize(GetMainThreadPointer(window), out var width, out var height);
        return new WindowSize(width, height);
    }

    public static WindowSize GetFramebufferSize(GlfwWindow window)
    {
        GlfwUnsafe.GetFramebufferSize(GetMainThreadPointer(window), out var width, out var height);
        return new WindowSize(width, height);
    }

    public static WindowPoint GetPosition(GlfwWindow window)
    {
        GlfwUnsafe.GetWindowPos(GetMainThreadPointer(window), out var x, out var y);
        return new WindowPoint(x, y);
    }

    public static WindowFrameSize GetFrameSize(GlfwWindow window)
    {
        GlfwUnsafe.GetWindowFrameSize(
            GetMainThreadPointer(window), out var left, out var top, out var right, out var bottom);
        return new WindowFrameSize(left, top, right, bottom);
    }

    public static ContentScale GetContentScale(GlfwWindow window)
    {
        GlfwUnsafe.GetWindowContentScale(GetMainThreadPointer(window), out var xscale, out var yscale);
        return new ContentScale(xscale, yscale);
    }

    public static MousePosition GetCursorPosition(GlfwWindow window)
    {
        GlfwUnsafe.GetCursorPos(GetMainThreadPointer(window), out var x, out var y);
        return new MousePosition(x, y);
    }

    public static InputAction GetKey(GlfwWindow window, Key key) =>
        GlfwUnsafe.GetKey(GetMainThreadPointer(window), key);

    public static InputAction GetMouseButton(GlfwWindow window, MouseButton button) =>
        GlfwUnsafe.GetMouseButton(GetMainThreadPointer(window), button);

    public static void SetTitle(GlfwWindow window, string title)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        GlfwUnsafe.SetWindowTitle(GetMainThreadPointer(window), title);
    }

    public static void SetSize(GlfwWindow window, WindowSize size)
    {
        if (size.Width <= 0 || size.Height <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(size), "Window dimensions must be positive.");
        }

        GlfwUnsafe.SetWindowSize(GetMainThreadPointer(window), size.Width, size.Height);
    }

    public static void SetPosition(GlfwWindow window, WindowPoint position) =>
        GlfwUnsafe.SetWindowPos(GetMainThreadPointer(window), position.X, position.Y);

    public static void SetCursorPosition(
        GlfwWindow window,
        MousePosition position) =>
        GlfwUnsafe.SetCursorPos(GetMainThreadPointer(window), position.X, position.Y);

    public static void Show(GlfwWindow window) =>
        GlfwUnsafe.ShowWindow(GetMainThreadPointer(window));

    public static void Hide(GlfwWindow window) =>
        GlfwUnsafe.HideWindow(GetMainThreadPointer(window));

    public static void MakeContextCurrent(GlfwWindow window) =>
        GlfwUnsafe.MakeContextCurrent(GetAnyThreadPointer(window));

    public static void ClearCurrentContext()
    {
        VerifyInitialized();
        GlfwUnsafe.MakeContextCurrent(null);
    }

    public static void SwapBuffers(GlfwWindow window) =>
        GlfwUnsafe.SwapBuffers(GetAnyThreadPointer(window));

#if !BROWSER

    public static nint GetProcAddress(string name)
    {
        VerifyInitialized();
        ArgumentException.ThrowIfNullOrEmpty(name);
        return GlfwUnsafe.GetProcAddress(name);
    }

    public static nint GetWin32Window(GlfwWindow window)
    {
        VerifyMainThread();
        RequireNativeExport(OperatingSystem.IsWindows(), "glfwGetWin32Window", "Win32");
        return GlfwUnsafe.GetWin32Window(GetMainThreadPointer(window));
    }

    public static nint GetCocoaWindow(GlfwWindow window)
    {
        VerifyMainThread();
        RequireNativeExport(OperatingSystem.IsMacOS(), "glfwGetCocoaWindow", "Cocoa");
        return GlfwUnsafe.GetCocoaWindow(GetMainThreadPointer(window));
    }

    public static nuint GetX11Window(GlfwWindow window)
    {
        VerifyMainThread();
        RequireNativeExport(OperatingSystem.IsLinux(), "glfwGetX11Window", "X11");
        return GlfwUnsafe.GetX11Window(GetMainThreadPointer(window));
    }

    public static nint GetX11Display()
    {
        VerifyMainThread();
        RequireNativeExport(OperatingSystem.IsLinux(), "glfwGetX11Display", "X11");
        return GlfwUnsafe.GetX11Display();
    }

    public static nint GetWaylandWindow(GlfwWindow window)
    {
        VerifyMainThread();
        RequireNativeExport(OperatingSystem.IsLinux(), "glfwGetWaylandWindow", "Wayland");
        return GlfwUnsafe.GetWaylandWindow(GetMainThreadPointer(window));
    }

    public static nint GetWaylandDisplay()
    {
        VerifyMainThread();
        RequireNativeExport(OperatingSystem.IsLinux(), "glfwGetWaylandDisplay", "Wayland");
        return GlfwUnsafe.GetWaylandDisplay();
    }

    private static void RequireNativeExport(bool platformMatches, string entryPoint, string platformName)
    {
        if (!platformMatches || !GlfwUnsafe.HasExport(entryPoint)) {
            throw new PlatformNotSupportedException(
                $"The loaded GLFW library does not expose {platformName} native access.");
        }
    }

#endif

    internal static WindowState ReadWindowState(GlfwWindow window) =>
        new(
            GetSize(window),
            GetFramebufferSize(window),
            ShouldClose(window),
            GetContentScale(window));

    private static void VerifyInitialized()
    {
        if (Volatile.Read(ref _referenceCount) == 0) {
            throw new InvalidOperationException("GLFW has not been initialized.");
        }
    }

    private static void VerifyMainThread()
    {
        VerifyInitialized();

        if (_ownerThreadId != Environment.CurrentManagedThreadId) {
            throw new InvalidOperationException(
                "This GLFW operation must run on the initialization thread.");
        }
    }

    private static void ValidateWindow(GlfwWindow window)
    {
        if (window.IsNull) {
            throw new ArgumentException("The GLFW window is null.", nameof(window));
        }

        if (window.Generation != Volatile.Read(ref _generation) ||
            !_liveWindows.TryGetValue(window.Handle, out var generation) ||
            generation != window.Generation) {
            throw new ObjectDisposedException(
                nameof(GlfwWindow),
                "The GLFW window has been destroyed or belongs to an old GLFW lifetime.");
        }
    }

    internal static WindowHandle* GetMainThreadPointer(GlfwWindow window)
    {
        VerifyMainThread();
        ValidateWindow(window);
        return (WindowHandle*)window.Handle;
    }

    internal static WindowHandle* GetAnyThreadPointer(GlfwWindow window)
    {
        VerifyInitialized();
        ValidateWindow(window);
        return (WindowHandle*)window.Handle;
    }

    private static string DescribeError((ErrorCode Code, string? Description) error) =>
        error.Description is null ? error.Code.ToString() : $"{error.Code}: {error.Description}";

    private static void ApplyWindowHints(
        in WindowDescriptor descriptor,
        in GlfwWindowOptions options)
    {
        GlfwUnsafe.DefaultWindowHints();
        GlfwUnsafe.WindowHint(WindowHintClientApi.ClientApi, options.ClientApi);
        GlfwUnsafe.WindowHint(WindowHintBool.Visible, descriptor.Visible);
        GlfwUnsafe.WindowHint(WindowHintBool.Resizable, descriptor.Resizable);
        GlfwUnsafe.WindowHint(WindowHintBool.Decorated, descriptor.Decorated);
        GlfwUnsafe.WindowHint(WindowHintBool.Floating, descriptor.Floating);
        GlfwUnsafe.WindowHint(WindowHintBool.Focused, descriptor.Focused);

        if (options.ContextVersionMajor is { } major) {
            GlfwUnsafe.WindowHint(WindowHintClientApi.ContextVersionMajor, major);
        }
        if (options.ContextVersionMinor is { } minor) {
            GlfwUnsafe.WindowHint(WindowHintClientApi.ContextVersionMinor, minor);
        }
        if (options.OpenGlProfile is { } profile) {
            GlfwUnsafe.WindowHint(WindowHintClientApi.OpenGlProfile, profile);
        }
        if (options.OpenGlForwardCompatible) {
            GlfwUnsafe.WindowHint((int)WindowHintClientApi.OpenGlForwardCompat, 1);
        }
        if (options.OpenGlDebugContext) {
            GlfwUnsafe.WindowHint((int)WindowHintClientApi.OpenGlDebugContext, 1);
        }
    }
}
