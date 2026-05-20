namespace Sia.GLFW;

public static unsafe partial class GlfwUnsafe
{
    public static void ClearWindowPosCallback(WindowHandle* window) => _setWindowPosCallback(window, null);
    public static void ClearWindowSizeCallback(WindowHandle* window) => _setWindowSizeCallback(window, null);
    public static void ClearWindowCloseCallback(WindowHandle* window) => _setWindowCloseCallback(window, null);
    public static void ClearWindowRefreshCallback(WindowHandle* window) => _setWindowRefreshCallback(window, null);
    public static void ClearWindowFocusCallback(WindowHandle* window) => _setWindowFocusCallback(window, null);
    public static void ClearWindowIconifyCallback(WindowHandle* window) => _setWindowIconifyCallback(window, null);
    public static void ClearWindowMaximizeCallback(WindowHandle* window) => _setWindowMaximizeCallback(window, null);
    public static void ClearFramebufferSizeCallback(WindowHandle* window) => _setFramebufferSizeCallback(window, null);
    public static void ClearWindowContentScaleCallback(WindowHandle* window) => _setWindowContentScaleCallback(window, null);
    public static void ClearKeyCallback(WindowHandle* window) => _setKeyCallback(window, null);
    public static void ClearCharCallback(WindowHandle* window) => _setCharCallback(window, null);
    public static void ClearCharModsCallback(WindowHandle* window) => _setCharModsCallback(window, null);
    public static void ClearMouseButtonCallback(WindowHandle* window) => _setMouseButtonCallback(window, null);
    public static void ClearCursorPosCallback(WindowHandle* window) => _setCursorPosCallback(window, null);
    public static void ClearCursorEnterCallback(WindowHandle* window) => _setCursorEnterCallback(window, null);
    public static void ClearScrollCallback(WindowHandle* window) => _setScrollCallback(window, null);
    public static void ClearDropCallback(WindowHandle* window) => _setDropCallback(window, null);
    public static void ClearMonitorCallback() => _setMonitorCallback(null);
    public static void ClearJoystickCallback() => _setJoystickCallback(null);
}
