namespace Sia.GLFW;

public readonly record struct GlfwWindowOptions(
    ClientApi ClientApi = ClientApi.NoApi,
    int? ContextVersionMajor = null,
    int? ContextVersionMinor = null,
    OpenGlProfile? OpenGlProfile = null,
    bool OpenGlForwardCompatible = false,
    bool OpenGlDebugContext = false,
    GlfwWindow SharedContext = default,
    GlfwMonitor Monitor = default)
{
    public static void Validate(in GlfwWindowOptions options)
    {
        if (options.ContextVersionMajor is <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(options), "Context major version must be greater than zero.");
        }
        if (options.ContextVersionMinor is < 0) {
            throw new ArgumentOutOfRangeException(
                nameof(options), "Context minor version cannot be negative.");
        }

        if (options.ContextVersionMinor is not null &&
            options.ContextVersionMajor is null) {
            throw new ArgumentException(
                "A context minor version requires a major version.");
        }

        if (options.ClientApi == ClientApi.NoApi &&
            (options.ContextVersionMajor is not null ||
             options.OpenGlProfile is not null ||
             options.OpenGlForwardCompatible ||
             options.OpenGlDebugContext ||
             !options.SharedContext.IsNull)) {
            throw new ArgumentException(
                "Context options cannot be used with ClientApi.NoApi.");
        }

        if (options.OpenGlProfile is not null &&
            options.ClientApi != ClientApi.OpenGL) {
            throw new ArgumentException(
                "OpenGL profiles are valid only for the OpenGL client API.");
        }
    }
}
