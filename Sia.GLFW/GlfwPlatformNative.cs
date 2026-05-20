using System.Runtime.InteropServices;

namespace Sia.GLFW;

public static partial class GlfwPlatformNative
{
    public static nint GetCurrentWin32ModuleHandle()
    {
        if (!OperatingSystem.IsWindows()) {
            throw new PlatformNotSupportedException("Win32 module handles are available only on Windows.");
        }

        return GetModuleHandle(null);
    }

    [LibraryImport("kernel32", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint GetModuleHandle(string? moduleName);
}
