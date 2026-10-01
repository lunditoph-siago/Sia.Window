using System.Runtime.InteropServices;
using Sia;
using Sia.Input;

namespace Sia.GLFW;

public sealed unsafe class GlfwModule : IAddon
{
    private sealed record InputRoute(GlfwModule Module, Entity Entity, GlfwWindow Window)
    {
        public World World => Module.OwnerWorld;
    }

    private enum PendingInputKind
    {
        Key,
        Character,
        MouseButton,
        MouseMoved,
        MouseEntered,
        MouseScrolled,
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct InputPayload
    {
        [FieldOffset(0)] public Key Key;
        [FieldOffset(4)] public int ScanCode;
        [FieldOffset(8)] public InputAction Action;
        [FieldOffset(12)] public KeyModifiers Modifiers;
        [FieldOffset(0)] public MouseButton MouseButton;
        [FieldOffset(0)] public uint CodePoint;
        [FieldOffset(0)] public double X;
        [FieldOffset(8)] public double Y;
        [FieldOffset(0)] public bool Entered;
    }

    private readonly struct PendingInputEvent
    {
        public InputRoute Route { get; }
        public PendingInputKind Kind { get; }
        private readonly InputPayload _payload;

        public Key Key => _payload.Key;
        public int ScanCode => _payload.ScanCode;
        public InputAction Action => _payload.Action;
        public KeyModifiers Modifiers => _payload.Modifiers;
        public MouseButton MouseButton => _payload.MouseButton;
        public uint CodePoint => _payload.CodePoint;
        public double X => _payload.X;
        public double Y => _payload.Y;
        public bool Entered => _payload.Entered;

        public PendingInputEvent(
            InputRoute route, PendingInputKind kind,
            Key Key = default, int ScanCode = 0, InputAction Action = default,
            KeyModifiers Modifiers = default, MouseButton MouseButton = default,
            uint CodePoint = 0, double X = 0, double Y = 0, bool Entered = false)
        {
            Route = route;
            Kind = kind;
            _payload = default;
            switch (kind) {
                case PendingInputKind.Key:
                    _payload.Key = Key;
                    _payload.ScanCode = ScanCode;
                    _payload.Action = Action;
                    _payload.Modifiers = Modifiers;
                    break;
                case PendingInputKind.Character:
                    _payload.CodePoint = CodePoint;
                    break;
                case PendingInputKind.MouseButton:
                    _payload.MouseButton = MouseButton;
                    _payload.Action = Action;
                    _payload.Modifiers = Modifiers;
                    break;
                case PendingInputKind.MouseMoved:
                case PendingInputKind.MouseScrolled:
                    _payload.X = X;
                    _payload.Y = Y;
                    break;
                case PendingInputKind.MouseEntered:
                    _payload.Entered = Entered;
                    break;
            }
        }
    }

    private const int MaxPendingInputEvents = 65_536;
    private const int RetainedInputCapacity = 4_096;
    private const int CapacityRecoveryBatches = 120;

    private static readonly Dictionary<nint, InputRoute> _routes = [];
    private static readonly object _registryGate = new();
    private static GlfwModule[] _activeModules = [];
    private static readonly List<Exception> _orphanCallbackErrors = [];

    private static readonly KeyCallback _keyCallback = OnKey;
    private static readonly CharCallback _charCallback = OnChar;
    private static readonly MouseButtonCallback _mouseButtonCallback = OnMouseButton;
    private static readonly CursorPosCallback _cursorPosCallback = OnCursorPos;
    private static readonly CursorEnterCallback _cursorEnterCallback = OnCursorEnter;
    private static readonly ScrollCallback _scrollCallback = OnScroll;

    private readonly Dictionary<EntityId, GlfwWindow> _windows = [];
    private List<PendingInputEvent> _pendingInputEvents = [];
    private List<PendingInputEvent> _dispatchingInputEvents = [];
    private int _droppedInputEvents;
    private int _smallInputBatches;
    private bool _dispatching;
    private readonly List<Exception> _callbackErrors = [];

    private WorldDispatcher.Listener<WorldEvents.Remove>? _removeEntity;
    private WorldDispatcher.Listener<WorldEvents.Remove<GlfwWindow>>? _removeWindow;
    private World? _world;
    private bool _initialized;

    internal World OwnerWorld => _world!;

    public void OnInitialize(World world)
    {
        Glfw.Initialize();
        _world = world;
        _removeEntity = OnEntityRemoved;
        _removeWindow = OnWindowRemoved;

        var entityListenerRegistered = false;
        try {
            world.Dispatcher.Listen(_removeEntity);
            entityListenerRegistered = true;
            world.Dispatcher.Listen(_removeWindow);
            _initialized = true;

            RegisterModule(this);
        }
        catch {
            if (entityListenerRegistered && _removeEntity is not null) {
                world.Dispatcher.Unlisten(_removeEntity);
            }

            _removeEntity = null;
            _removeWindow = null;
            _world = null;
            Glfw.Terminate();
            throw;
        }
    }

    public void OnUninitialize(World world)
    {
        if (!_initialized) {
            return;
        }

        UnregisterModule(this);

        ClearPendingEvents();

        List<Exception>? cleanupErrors = null;

        if (_removeEntity is not null) {
            try {
                world.Dispatcher.Unlisten(_removeEntity);
                _removeEntity = null;
            }
            catch (Exception exception) {
                (cleanupErrors ??= []).Add(exception);
            }
        }

        if (_removeWindow is not null) {
            try {
                world.Dispatcher.Unlisten(_removeWindow);
                _removeWindow = null;
            }
            catch (Exception exception) {
                (cleanupErrors ??= []).Add(exception);
            }
        }

        foreach (var entityId in _windows.Keys.ToArray()) {
            try {
                Release(entityId);
            }
            catch (Exception exception) {
                (cleanupErrors ??= []).Add(exception);
            }
        }

        if (_windows.Count == 0) {
            try {
                Glfw.Terminate();
                _world = null;
                _initialized = false;
            }
            catch (Exception exception) {
                (cleanupErrors ??= []).Add(exception);
            }
        }

        if (cleanupErrors is not null) {
            throw new AggregateException(
                "One or more GLFW resources could not be released cleanly.",
                cleanupErrors);
        }
    }

    internal void Own(Entity entity, GlfwWindow window)
    {
        if (!_windows.TryAdd(entity.Id, window)) {
            throw new InvalidOperationException(
                $"Entity {entity.Id} already owns a GLFW window.");
        }

        var routeAdded = false;
        try {
            if (!_routes.TryAdd(window.Handle, new InputRoute(this, entity, window))) {
                throw new InvalidOperationException(
                    $"GLFW window handle {window.Handle} is already registered.");
            }
            routeAdded = true;

            var handle = (WindowHandle*)window.Handle;
            GlfwUnsafe.SetKeyCallback(handle, _keyCallback);
            GlfwUnsafe.SetCharCallback(handle, _charCallback);
            GlfwUnsafe.SetMouseButtonCallback(handle, _mouseButtonCallback);
            GlfwUnsafe.SetCursorPosCallback(handle, _cursorPosCallback);
            GlfwUnsafe.SetCursorEnterCallback(handle, _cursorEnterCallback);
            GlfwUnsafe.SetScrollCallback(handle, _scrollCallback);
        }
        catch {
            if (routeAdded) {
                _routes.Remove(window.Handle);
            }
            _windows.Remove(entity.Id);
            throw;
        }
    }

    internal static void DispatchPendingInputEvents()
    {
        var modules = Volatile.Read(ref _activeModules);

        List<Exception>? errors = null;
        foreach (var module in modules) {
            module.DispatchPending(ref errors);
        }

        lock (_registryGate) {
            if (_orphanCallbackErrors.Count != 0) {
                (errors ??= []).AddRange(_orphanCallbackErrors);
                _orphanCallbackErrors.Clear();
            }
        }

        if (errors is not null) {
            throw new AggregateException(
                "One or more input event listeners failed.", errors);
        }
    }

    private void DispatchPending(ref List<Exception>? errors)
    {
        // A listener may poll again. Its newly queued input belongs to the next
        // batch, rather than swapping the lists underneath the current iteration.
        if (_dispatching) return;
        _dispatching = true;
        (_pendingInputEvents, _dispatchingInputEvents) =
            (_dispatchingInputEvents, _pendingInputEvents);
        var batchCount = _dispatchingInputEvents.Count;

        try {
            foreach (var input in _dispatchingInputEvents) {
                try {
                    Dispatch(in input);
                }
                catch (Exception exception) {
                    (errors ??= []).Add(exception);
                }
            }
        }
        finally {
            _dispatchingInputEvents.Clear();
            _dispatching = false;
            RecoverInputCapacity(batchCount);
        }

        if (_droppedInputEvents != 0) {
            (errors ??= []).Add(new GlfwException(
                $"{_droppedInputEvents} GLFW input events were dropped."));
            _droppedInputEvents = 0;
        }

        if (_callbackErrors.Count != 0) {
            (errors ??= []).AddRange(_callbackErrors);
            _callbackErrors.Clear();
        }
    }

    internal static void RegisterModule(GlfwModule module)
    {
        lock (_registryGate) {
            var modules = _activeModules;
            var next = new GlfwModule[modules.Length + 1];
            modules.CopyTo(next, 0);
            next[^1] = module;
            Volatile.Write(ref _activeModules, next);
        }
    }

    internal static void UnregisterModule(GlfwModule module)
    {
        lock (_registryGate) {
            var modules = _activeModules;
            var index = Array.IndexOf(modules, module);
            if (index < 0) return;
            var next = modules.Length == 1 ? [] : new GlfwModule[modules.Length - 1];
            Array.Copy(modules, 0, next, 0, index);
            Array.Copy(modules, index + 1, next, index, modules.Length - index - 1);
            Volatile.Write(ref _activeModules, next);
        }
    }

    private void RecoverInputCapacity(int batchCount)
    {
        if (batchCount > RetainedInputCapacity ||
            _pendingInputEvents.Count > RetainedInputCapacity) {
            _smallInputBatches = 0;
            return;
        }
        if (_dispatchingInputEvents.Capacity <= RetainedInputCapacity &&
            _pendingInputEvents.Capacity <= RetainedInputCapacity) {
            _smallInputBatches = 0;
            return;
        }
        if (++_smallInputBatches < CapacityRecoveryBatches) return;

        // Recover one empty buffer at a time, with a cooldown between allocations.
        if (_dispatchingInputEvents.Capacity > RetainedInputCapacity) {
            _dispatchingInputEvents.Capacity = RetainedInputCapacity;
            _smallInputBatches = 0;
        }
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
        if (!_windows.TryGetValue(entityId, out var window)) {
            return;
        }

        var routeRemoved = _routes.Remove(window.Handle, out var route);

        try {
            Glfw.DestroyWindow(ref window);
        }
        catch {
            if (routeRemoved && route is not null) {
                _routes.TryAdd(route.Window.Handle, route);
            }
            throw;
        }

        _windows.Remove(entityId);
    }

    private void ClearPendingEvents()
    {
        _pendingInputEvents.Clear();
        if (!_dispatching) _dispatchingInputEvents.Clear();
        _smallInputBatches = 0;
    }

    private static bool TryCaptureRoute(WindowHandle* window, out InputRoute route) =>
        _routes.TryGetValue((nint)window, out route!);

    private static bool IsCurrent(in InputRoute route)
    {
        if (route.Module._world is null || !route.Entity.IsValid ||
            !route.Entity.Contains<GlfwWindow>()) {
            return false;
        }

        return route.Entity.Get<GlfwWindow>() == route.Window;
    }

    private static void Dispatch(in PendingInputEvent input)
    {
        var route = input.Route;
        if (!IsCurrent(in route)) {
            return;
        }

        var world = route.World;
        var entity = route.Entity;

        switch (input.Kind) {
            case PendingInputKind.Key:
                SendKey(world, entity, input.Key, input.ScanCode, input.Action, input.Modifiers);
                break;
            case PendingInputKind.Character:
                world.Send(entity, new InputEvents.TextEntered(input.CodePoint));
                break;
            case PendingInputKind.MouseButton:
                SendMouseButton(world, entity, input.MouseButton, input.Action, input.Modifiers);
                break;
            case PendingInputKind.MouseMoved:
                world.Send(entity, new InputEvents.MouseMoved(new MousePosition(input.X, input.Y)));
                break;
            case PendingInputKind.MouseEntered:
                if (input.Entered) {
                    world.Send(entity, new InputEvents.MouseEntered());
                } else {
                    world.Send(entity, new InputEvents.MouseExited());
                }
                break;
            case PendingInputKind.MouseScrolled:
                world.Send(entity, new InputEvents.MouseScrolled(new ScrollDelta(input.X, input.Y)));
                break;
        }
    }

    private void Enqueue(in PendingInputEvent input)
    {
        if (input.Kind == PendingInputKind.MouseMoved &&
            _pendingInputEvents.Count != 0) {
            var last = _pendingInputEvents[^1];

            if (last.Kind == PendingInputKind.MouseMoved &&
                last.Route.Window == input.Route.Window) {
                _pendingInputEvents[^1] = input;
                return;
            }
        }

        if (_pendingInputEvents.Count >= MaxPendingInputEvents) {
            _droppedInputEvents++;
            return;
        }

        _pendingInputEvents.Add(input);
    }

    private void CaptureCallbackError(Exception exception)
    {
        try {
            _callbackErrors.Add(exception);
        }
        catch {}
    }

    private static void CaptureCallbackError(WindowHandle* window, Exception exception)
    {
        try {
            if (TryCaptureRoute(window, out var route)) {
                route.Module.CaptureCallbackError(exception);
                return;
            }

            lock (_registryGate) {
                _orphanCallbackErrors.Add(exception);
            }
        }
        catch {}
    }

    private static void OnKey(
        WindowHandle* window, Key key, int scancode, InputAction action, KeyModifiers mods)
    {
        try {
            if (TryCaptureRoute(window, out var route)) {
                route.Module.Enqueue(new PendingInputEvent(
                    route,
                    PendingInputKind.Key,
                    Key: key,
                    ScanCode: scancode,
                    Action: action,
                    Modifiers: mods));
            }
        }
        catch (Exception exception) {
            CaptureCallbackError(window, exception);
        }
    }

    private static void OnChar(WindowHandle* window, uint codepoint)
    {
        try {
            if (TryCaptureRoute(window, out var route)) {
                route.Module.Enqueue(new PendingInputEvent(
                    route,
                    PendingInputKind.Character,
                    CodePoint: codepoint));
            }
        }
        catch (Exception exception) {
            CaptureCallbackError(window, exception);
        }
    }

    private static void OnMouseButton(
        WindowHandle* window, MouseButton button, InputAction action, KeyModifiers mods)
    {
        try {
            if (TryCaptureRoute(window, out var route)) {
                route.Module.Enqueue(new PendingInputEvent(
                    route,
                    PendingInputKind.MouseButton,
                    Action: action,
                    Modifiers: mods,
                    MouseButton: button));
            }
        }
        catch (Exception exception) {
            CaptureCallbackError(window, exception);
        }
    }

    private static void OnCursorPos(WindowHandle* window, double x, double y)
    {
        try {
            if (TryCaptureRoute(window, out var route)) {
                route.Module.Enqueue(new PendingInputEvent(
                    route,
                    PendingInputKind.MouseMoved,
                    X: x,
                    Y: y));
            }
        }
        catch (Exception exception) {
            CaptureCallbackError(window, exception);
        }
    }

    private static void OnCursorEnter(WindowHandle* window, bool entered)
    {
        try {
            if (TryCaptureRoute(window, out var route)) {
                route.Module.Enqueue(new PendingInputEvent(
                    route,
                    PendingInputKind.MouseEntered,
                    Entered: entered));
            }
        }
        catch (Exception exception) {
            CaptureCallbackError(window, exception);
        }
    }

    private static void OnScroll(WindowHandle* window, double xoffset, double yoffset)
    {
        try {
            if (TryCaptureRoute(window, out var route)) {
                route.Module.Enqueue(new PendingInputEvent(
                    route,
                    PendingInputKind.MouseScrolled,
                    X: xoffset,
                    Y: yoffset));
            }
        }
        catch (Exception exception) {
            CaptureCallbackError(window, exception);
        }
    }
}
