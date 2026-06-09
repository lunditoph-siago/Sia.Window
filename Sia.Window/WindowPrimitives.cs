namespace Sia.Window;

public readonly record struct WindowSize(int Width, int Height);

public readonly record struct WindowPoint(int X, int Y);

public readonly record struct ContentScale(float X, float Y);

public readonly record struct WindowFrameSize(int Left, int Top, int Right, int Bottom);
