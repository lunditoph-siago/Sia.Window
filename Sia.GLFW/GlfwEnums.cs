namespace Sia.GLFW;

public enum ConnectedState
{
    Connected = 0x00040001,
    Disconnected = 0x00040002,
}

public enum ErrorCode
{
    NoError = 0,
    NotInitialized = 0x00010001,
    NoCurrentContext = 0x00010002,
    InvalidEnum = 0x00010003,
    InvalidValue = 0x00010004,
    OutOfMemory = 0x00010005,
    ApiUnavailable = 0x00010006,
    VersionUnavailable = 0x00010007,
    PlatformError = 0x00010008,
    FormatUnavailable = 0x00010009,
    NoWindowContext = 0x0001000A,
}

public enum CursorModeValue
{
    Normal = 0x00034001,
    Hidden = 0x00034002,
    Disabled = 0x00034003,
}

public enum CursorShape
{
    Arrow = 0x00036001,
    IBeam = 0x00036002,
    Crosshair = 0x00036003,
    Hand = 0x00036004,
    HResize = 0x00036005,
    VResize = 0x00036006,
}

public enum ClientApi
{
    NoApi = 0,
    OpenGL = 0x00030001,
    OpenGLES = 0x00030002,
}

public enum ContextCreationApi
{
    Native = 0x00036001,
    EGL = 0x00036002,
    OSMesa = 0x00036003,
}

public enum ContextRobustness
{
    NoRobustness = 0,
    NoResetNotification = 0x00031001,
    LoseContextOnReset = 0x00031002,
}

public enum OpenGlProfile
{
    Any = 0,
    Core = 0x00032001,
    Compat = 0x00032002,
}

public enum ReleaseBehavior
{
    Any = 0,
    Flush = 0x00035001,
    None = 0x00035002,
}

public enum WindowHintClientApi
{
    ClientApi = 0x00022001,
    ContextVersionMajor = 0x00022002,
    ContextVersionMinor = 0x00022003,
    ContextRobustness = 0x00022005,
    OpenGlForwardCompat = 0x00022006,
    OpenGlDebugContext = 0x00022007,
    OpenGlProfile = 0x00022008,
    ContextReleaseBehavior = 0x00022009,
    ContextNoError = 0x0002200A,
    ContextCreationApi = 0x0002200B,
}

public enum WindowHintBool
{
    Focused = 0x00020001,
    Resizable = 0x00020003,
    Visible = 0x00020004,
    Decorated = 0x00020005,
    AutoIconify = 0x00020006,
    Floating = 0x00020007,
    Maximized = 0x00020008,
    CenterCursor = 0x00020009,
    TransparentFramebuffer = 0x0002000A,
    FocusOnShow = 0x0002000C,

    Stereo = 0x0002100C,
    SrgbCapable = 0x0002100E,
    DoubleBuffer = 0x00021010,

    ContextNoError = 0x0002200A,
    ScaleToMonitor = 0x0002200C,

    CocoaRetinaFramebuffer = 0x00023001,
    CocoaGraphicsSwitching = 0x00023003,
}

public enum WindowHintInt
{
    RedBits = 0x00021001,
    GreenBits = 0x00021002,
    BlueBits = 0x00021003,
    AlphaBits = 0x00021004,
    DepthBits = 0x00021005,
    StencilBits = 0x00021006,
    AccumRedBits = 0x00021007,
    AccumGreenBits = 0x00021008,
    AccumBlueBits = 0x00021009,
    AccumAlphaBits = 0x0002100A,
    AuxBuffers = 0x0002100B,
    Samples = 0x0002100D,
    RefreshRate = 0x0002100F,
}

public enum WindowHintString
{
    CocoaFrameName = 0x00023002,
    X11ClassName = 0x00024001,
    X11InstanceName = 0x00024002,
}

public enum WindowAttribute
{
    Focused = 0x00020001,
    Iconified = 0x00020002,
    Resizable = 0x00020003,
    Visible = 0x00020004,
    Decorated = 0x00020005,
    AutoIconify = 0x00020006,
    Floating = 0x00020007,
    Maximized = 0x00020008,
    TransparentFramebuffer = 0x0002000A,
    Hovered = 0x0002000B,
    FocusOnShow = 0x0002000C,

    ClientApi = 0x00022001,
    ContextVersionMajor = 0x00022002,
    ContextVersionMinor = 0x00022003,
    ContextRevision = 0x00022004,
    ContextRobustness = 0x00022005,
    OpenGlForwardCompat = 0x00022006,
    OpenGlDebugContext = 0x00022007,
    OpenGlProfile = 0x00022008,
    ContextReleaseBehavior = 0x00022009,
    ContextNoError = 0x0002200A,
    ContextCreationApi = 0x0002200B,
}

public enum InputMode
{
    Cursor = 0x00033001,
    StickyKeys = 0x00033002,
    StickyMouseButtons = 0x00033003,
    LockKeyMods = 0x00033004,
    RawMouseMotion = 0x00033005,
}
