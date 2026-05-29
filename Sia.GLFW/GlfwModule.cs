using Sia;
using Sia.Input;

namespace Sia.GLFW;

/// <summary>Owns the GLFW lifetime and windows created for one Sia world.</summary>
public sealed unsafe class GlfwModule : IAddon
{
    // Window handles are process-unique and every GLFW operation is confined
    // to the initialization thread, so static routing tables are safe.
    private static readonly Dictionary<nint, (World World, Entity Entity)> _routes = [];
    private static readonly List<Exception> _callbackErrors = [];

    // Rooted for the process lifetime so GLFW never calls a collected delegate.
    private static readonly KeyCallback _keyCallback = OnKey;
    private static readonly CharCallback _charCallback = OnChar;
    private static readonly MouseButtonCallback _mouseButtonCallback = OnMouseButton;
    private static readonly CursorPosCallback _cursorPosCallback = OnCursorPos;
    private static readonly CursorEnterCallback _cursorEnterCallback = OnCursorEnter;
    private static readonly ScrollCallback _scrollCallback = OnScroll;

    private readonly Dictionary<EntityId, GlfwWindow> _windows = [];
    private WorldDispatcher.Listener<WorldEvents.Remove>? _removeEntity;
    private WorldDispatcher.Listener<WorldEvents.Remove<GlfwWindow>>? _removeWindow;
    private World? _world;
    private bool _initialized;

    public void OnInitialize(World world)
    {
        Glfw.Initialize();
        _initialized = true;
        _world = world;

        _removeEntity = OnEntityRemoved;
        _removeWindow = OnWindowRemoved;
        world.Dispatcher.Listen(_removeEntity);
        world.Dispatcher.Listen(_removeWindow);
    }

    public void OnUninitialize(World world)
    {
        if (!_initialized) {
            return;
        }

        if (_removeEntity is not null) {
            world.Dispatcher.Unlisten(_removeEntity);
        }
        if (_removeWindow is not null) {
            world.Dispatcher.Unlisten(_removeWindow);
        }

        foreach (var pair in _windows) {
            var window = pair.Value;
            _routes.Remove(window.Handle);
            Glfw.DestroyWindow(ref window);
        }
        _windows.Clear();

        _world = null;
        Glfw.Terminate();
        _initialized = false;
    }

    internal void Own(Entity entity, GlfwWindow window)
    {
        if (!_windows.TryAdd(entity.Id, window)) {
            throw new InvalidOperationException(
                $"Entity {entity.Id} already owns a GLFW window.");
        }

        _routes[window.Handle] = (_world!, entity);

        var handle = (WindowHandle*)window.Handle;
        GlfwUnsafe.SetKeyCallback(handle, _keyCallback);
        GlfwUnsafe.SetCharCallback(handle, _charCallback);
        GlfwUnsafe.SetMouseButtonCallback(handle, _mouseButtonCallback);
        GlfwUnsafe.SetCursorPosCallback(handle, _cursorPosCallback);
        GlfwUnsafe.SetCursorEnterCallback(handle, _cursorEnterCallback);
        GlfwUnsafe.SetScrollCallback(handle, _scrollCallback);
    }

    /// <summary>
    /// Rethrows listener exceptions captured while native callbacks were on the
    /// stack, after the surrounding poll has safely returned to managed code.
    /// </summary>
    internal static void ThrowPendingCallbackErrors()
    {
        if (_callbackErrors.Count == 0) {
            return;
        }

        var errors = _callbackErrors.ToArray();
        _callbackErrors.Clear();
        throw new AggregateException(
            "One or more input event listeners failed.", errors);
    }

    internal static void SendKey(
        World world,
        Entity entity,
        Key key,
        int scanCode,
        InputAction action,
        KeyModifiers modifiers)
    {
        switch (action) {
            case InputAction.Press:
                world.Send(entity, new InputEvents.KeyPressed(key, scanCode, modifiers));
                break;
            case InputAction.Release:
                world.Send(entity, new InputEvents.KeyReleased(key, scanCode, modifiers));
                break;
            case InputAction.Repeat:
                world.Send(entity, new InputEvents.KeyRepeated(key, scanCode, modifiers));
                break;
        }
    }

    internal static void SendMouseButton(
        World world,
        Entity entity,
        MouseButton button,
        InputAction action,
        KeyModifiers modifiers)
    {
        switch (action) {
            case InputAction.Press:
                world.Send(entity, new InputEvents.MouseButtonPressed(button, modifiers));
                break;
            case InputAction.Release:
                world.Send(entity, new InputEvents.MouseButtonReleased(button, modifiers));
                break;
        }
    }

    private bool OnEntityRemoved(Entity entity, in WorldEvents.Remove @event)
    {
        Release(entity.Id);
        return false;
    }

    private bool OnWindowRemoved(
        Entity entity,
        in WorldEvents.Remove<GlfwWindow> @event)
    {
        Release(entity.Id);
        return false;
    }

    private void Release(EntityId entityId)
    {
        if (!_windows.Remove(entityId, out var window)) {
            return;
        }

        _routes.Remove(window.Handle);
        Glfw.DestroyWindow(ref window);
    }

    private static bool TryRoute(
        WindowHandle* window,
        out (World World, Entity Entity) route) =>
        _routes.TryGetValue((nint)window, out route);

    private static void OnKey(
        WindowHandle* window, Key key, int scancode, InputAction action, KeyModifiers mods)
    {
        if (!TryRoute(window, out var route)) {
            return;
        }

        try {
            SendKey(route.World, route.Entity, key, scancode, action, mods);
        }
        catch (Exception exception) {
            _callbackErrors.Add(exception);
        }
    }

    private static void OnChar(WindowHandle* window, uint codepoint)
    {
        if (!TryRoute(window, out var route)) {
            return;
        }

        try {
            route.World.Send(route.Entity, new InputEvents.TextEntered(codepoint));
        }
        catch (Exception exception) {
            _callbackErrors.Add(exception);
        }
    }

    private static void OnMouseButton(
        WindowHandle* window, MouseButton button, InputAction action, KeyModifiers mods)
    {
        if (!TryRoute(window, out var route)) {
            return;
        }

        try {
            SendMouseButton(route.World, route.Entity, button, action, mods);
        }
        catch (Exception exception) {
            _callbackErrors.Add(exception);
        }
    }

    private static void OnCursorPos(WindowHandle* window, double x, double y)
    {
        if (!TryRoute(window, out var route)) {
            return;
        }

        try {
            route.World.Send(
                route.Entity, new InputEvents.MouseMoved(new MousePosition(x, y)));
        }
        catch (Exception exception) {
            _callbackErrors.Add(exception);
        }
    }

    private static void OnCursorEnter(WindowHandle* window, bool entered)
    {
        if (!TryRoute(window, out var route)) {
            return;
        }

        try {
            if (entered) {
                route.World.Send(route.Entity, new InputEvents.MouseEntered());
            } else {
                route.World.Send(route.Entity, new InputEvents.MouseExited());
            }
        }
        catch (Exception exception) {
            _callbackErrors.Add(exception);
        }
    }

    private static void OnScroll(WindowHandle* window, double xoffset, double yoffset)
    {
        if (!TryRoute(window, out var route)) {
            return;
        }

        try {
            route.World.Send(
                route.Entity,
                new InputEvents.MouseScrolled(new ScrollDelta(xoffset, yoffset)));
        }
        catch (Exception exception) {
            _callbackErrors.Add(exception);
        }
    }
}
