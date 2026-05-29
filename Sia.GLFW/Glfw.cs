using Sia.Input;
using Sia.Window;

namespace Sia.GLFW;

/// <summary>Provides explicit, functional operations over GLFW capabilities.</summary>
public static unsafe class Glfw
{
    private static readonly object _lifetimeLock = new();
    private static int _ownerThreadId;
    private static int _referenceCount;

    /// <summary>Acquires one process-wide GLFW initialization reference.</summary>
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
                    throw new GlfwException("GLFW initialization failed.");
                }

                _ownerThreadId = threadId;
            }

            checked {
                Volatile.Write(ref _referenceCount, _referenceCount + 1);
            }
        }
    }

    /// <summary>Releases one initialization reference and terminates GLFW at zero.</summary>
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

            Volatile.Write(ref _referenceCount, _referenceCount - 1);
            if (_referenceCount == 0) {
                GlfwUnsafe.Terminate();
                _ownerThreadId = 0;
            }
        }
    }

    public static GlfwWindow CreateWindow(
        in WindowDescriptor descriptor,
        in GlfwWindowOptions options = default)
    {
        VerifyAccess();
        WindowDescriptor.Validate(in descriptor);
        ValidateWindowOptions(in options);
        ApplyWindowHints(in descriptor, in options);

        var handle = GlfwUnsafe.CreateWindow(
            descriptor.Width,
            descriptor.Height,
            descriptor.Title,
            (Monitor*)options.Monitor.Handle,
            (WindowHandle*)options.SharedContext.Handle);

        return handle == null
            ? throw new GlfwException("GLFW window creation failed.")
            : new GlfwWindow((nint)handle);
    }

    /// <summary>Destroys a window and clears the caller's canonical capability.</summary>
    public static void DestroyWindow(ref GlfwWindow window)
    {
        VerifyAccess();
        if (window.IsNull) {
            return;
        }

        var handle = window;
        window = default;
        GlfwUnsafe.DestroyWindow((WindowHandle*)handle.Handle);
    }

    public static void ValidateWindowOptions(in GlfwWindowOptions options)
    {
        if (options.ContextVersionMajor is < 0) {
            throw new ArgumentOutOfRangeException(
                nameof(options), "Context major version cannot be negative.");
        }
        if (options.ContextVersionMinor is < 0) {
            throw new ArgumentOutOfRangeException(
                nameof(options), "Context minor version cannot be negative.");
        }
    }

    public static void PollEvents()
    {
        VerifyAccess();
        GlfwUnsafe.PollEvents();
    }

    public static void WaitEvents()
    {
        VerifyAccess();
        GlfwUnsafe.WaitEvents();
    }

    public static bool ShouldClose(GlfwWindow window) =>
        GlfwUnsafe.WindowShouldClose(GetPointer(window));

    public static void RequestClose(GlfwWindow window) =>
        GlfwUnsafe.SetWindowShouldClose(GetPointer(window), true);

    public static WindowSize GetSize(GlfwWindow window)
    {
        GlfwUnsafe.GetWindowSize(GetPointer(window), out var width, out var height);
        return new WindowSize(width, height);
    }

    public static WindowSize GetFramebufferSize(GlfwWindow window)
    {
        GlfwUnsafe.GetFramebufferSize(GetPointer(window), out var width, out var height);
        return new WindowSize(width, height);
    }

    public static WindowPoint GetPosition(GlfwWindow window)
    {
        GlfwUnsafe.GetWindowPos(GetPointer(window), out var x, out var y);
        return new WindowPoint(x, y);
    }

    public static MousePosition GetCursorPosition(GlfwWindow window)
    {
        GlfwUnsafe.GetCursorPos(GetPointer(window), out var x, out var y);
        return new MousePosition(x, y);
    }

    public static InputAction GetKey(GlfwWindow window, Key key) =>
        GlfwUnsafe.GetKey(GetPointer(window), key);

    public static InputAction GetMouseButton(GlfwWindow window, MouseButton button) =>
        GlfwUnsafe.GetMouseButton(GetPointer(window), button);

    public static void SetTitle(GlfwWindow window, string title)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        GlfwUnsafe.SetWindowTitle(GetPointer(window), title);
    }

    public static void SetSize(GlfwWindow window, WindowSize size)
    {
        if (size.Width <= 0 || size.Height <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(size), "Window dimensions must be positive.");
        }

        GlfwUnsafe.SetWindowSize(GetPointer(window), size.Width, size.Height);
    }

    public static void SetPosition(GlfwWindow window, WindowPoint position) =>
        GlfwUnsafe.SetWindowPos(GetPointer(window), position.X, position.Y);

    public static void SetCursorPosition(
        GlfwWindow window,
        MousePosition position) =>
        GlfwUnsafe.SetCursorPos(GetPointer(window), position.X, position.Y);

    public static void Show(GlfwWindow window) =>
        GlfwUnsafe.ShowWindow(GetPointer(window));

    public static void Hide(GlfwWindow window) =>
        GlfwUnsafe.HideWindow(GetPointer(window));

    public static void MakeContextCurrent(GlfwWindow window) =>
        GlfwUnsafe.MakeContextCurrent(GetPointer(window));

    public static void SwapBuffers(GlfwWindow window) =>
        GlfwUnsafe.SwapBuffers(GetPointer(window));

    public static nint GetProcAddress(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return GlfwUnsafe.GetProcAddress(name);
    }

    public static nint GetWin32Window(GlfwWindow window) =>
        GlfwUnsafe.GetWin32Window(GetPointer(window));

    public static nint GetCocoaWindow(GlfwWindow window) =>
        GlfwUnsafe.GetCocoaWindow(GetPointer(window));

    public static nint GetX11Window(GlfwWindow window) =>
        GlfwUnsafe.GetX11Window(GetPointer(window));

    public static nint GetX11Display() => GlfwUnsafe.GetX11Display();

    public static nint GetWaylandWindow(GlfwWindow window) =>
        GlfwUnsafe.GetWaylandWindow(GetPointer(window));

    public static nint GetWaylandDisplay() => GlfwUnsafe.GetWaylandDisplay();

    internal static WindowState ReadWindowState(GlfwWindow window) =>
        new(GetSize(window), GetFramebufferSize(window), ShouldClose(window));

    internal static WindowHandle* GetPointer(GlfwWindow window)
    {
        VerifyAccess();
        return window.IsNull
            ? throw new ArgumentException("The GLFW window is null.", nameof(window))
            : (WindowHandle*)window.Handle;
    }

    private static void VerifyAccess()
    {
        // Lock-free: _ownerThreadId is published before the count under
        // _lifetimeLock, and only the owner thread may pass this check anyway.
        if (Volatile.Read(ref _referenceCount) == 0) {
            throw new InvalidOperationException("GLFW has not been initialized.");
        }
        if (_ownerThreadId != Environment.CurrentManagedThreadId) {
            throw new InvalidOperationException(
                "GLFW must be accessed from its initialization thread.");
        }
    }

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
            GlfwUnsafe.WindowHint((int)WindowHintClientApi.ContextVersionMajor, major);
        }
        if (options.ContextVersionMinor is { } minor) {
            GlfwUnsafe.WindowHint((int)WindowHintClientApi.ContextVersionMinor, minor);
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
