namespace Sia.Window.Tests;

public sealed class WindowStateTests
{
    [Fact]
    public void ApplyReportsOnlyRisingCloseEdgeAndChangedSizes()
    {
        var state = new WindowState(
            new WindowSize(640, 480),
            new WindowSize(640, 480),
            false);
        var next = new WindowState(
            new WindowSize(800, 600),
            new WindowSize(1600, 1200),
            true);

        var changes = state.Apply(in next);

        Assert.Equal(
            WindowChanges.Size |
            WindowChanges.FramebufferSize |
            WindowChanges.CloseRequested,
            changes);
        Assert.Equal(next, state);
        Assert.Equal(WindowChanges.None, state.Apply(in next));
    }

    [Fact]
    public void GetChangesIgnoresSustainedCloseRequest()
    {
        var closed = new WindowState(
            new WindowSize(640, 480),
            new WindowSize(640, 480),
            true);

        Assert.Equal(
            WindowChanges.None,
            WindowState.GetChanges(in closed, in closed));
    }
}
