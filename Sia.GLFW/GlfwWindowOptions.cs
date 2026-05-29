namespace Sia.GLFW;

/// <summary>GLFW-specific creation options applied on top of a window descriptor.</summary>
public readonly record struct GlfwWindowOptions(
    ClientApi ClientApi = ClientApi.NoApi,
    int? ContextVersionMajor = null,
    int? ContextVersionMinor = null,
    OpenGlProfile? OpenGlProfile = null,
    bool OpenGlForwardCompatible = false,
    bool OpenGlDebugContext = false,
    GlfwWindow SharedContext = default,
    GlfwMonitor Monitor = default);
