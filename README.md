# Sia.Window

Sia.GLFW uses the platform default unmanaged calling convention for P/Invoke,
callbacks, and unmanaged function pointers. Browser WebAssembly and native x64,
ARM, and ARM64 are the intended targets; native x86 is not supported. Explicit
`UnmanagedCallConv(CallConvCdecl)` metadata triggers a Mono AOT compiler assertion
in the workspace's .NET 11 RC2 toolchain.
