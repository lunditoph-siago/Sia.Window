namespace Sia.GLFW;

public static unsafe partial class GlfwUnsafe
{
    public static void ClearWindowPosCallback(WindowHandle* window) => SetWindowPosCallback(window, null);
    public static void ClearWindowSizeCallback(WindowHandle* window) => SetWindowSizeCallback(window, null);
    public static void ClearWindowCloseCallback(WindowHandle* window) => SetWindowCloseCallback(window, null);
    public static void ClearWindowRefreshCallback(WindowHandle* window) => SetWindowRefreshCallback(window, null);
    public static void ClearWindowFocusCallback(WindowHandle* window) => SetWindowFocusCallback(window, null);
    public static void ClearWindowIconifyCallback(WindowHandle* window) => SetWindowIconifyCallback(window, null);
    public static void ClearWindowMaximizeCallback(WindowHandle* window) => SetWindowMaximizeCallback(window, null);
    public static void ClearFramebufferSizeCallback(WindowHandle* window) => SetFramebufferSizeCallback(window, null);
    public static void ClearWindowContentScaleCallback(WindowHandle* window) => SetWindowContentScaleCallback(window, null);
    public static void ClearKeyCallback(WindowHandle* window) => SetKeyCallback(window, null);
    public static void ClearCharCallback(WindowHandle* window) => SetCharCallback(window, null);
    public static void ClearMouseButtonCallback(WindowHandle* window) => SetMouseButtonCallback(window, null);
    public static void ClearCursorPosCallback(WindowHandle* window) => SetCursorPosCallback(window, null);
    public static void ClearCursorEnterCallback(WindowHandle* window) => SetCursorEnterCallback(window, null);
    public static void ClearScrollCallback(WindowHandle* window) => SetScrollCallback(window, null);
    public static void ClearDropCallback(WindowHandle* window) => SetDropCallback(window, null);
    public static void ClearMonitorCallback() => SetMonitorCallback(null);
    public static void ClearJoystickCallback() => SetJoystickCallback(null);
}
