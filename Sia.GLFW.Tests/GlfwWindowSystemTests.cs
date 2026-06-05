using System.Runtime.CompilerServices;

using Sia;
using Sia.Window;

namespace Sia.GLFW.Tests;

public sealed class GlfwWindowSystemTests
{
    [Fact]
    public void WindowCapabilityIsAnUnmanagedEcsValue()
    {
        Assert.False(RuntimeHelpers.IsReferenceOrContainsReferences<GlfwWindow>());
        Assert.True(default(GlfwWindow).IsNull);
        Assert.False(new GlfwWindow(42).IsNull);
    }

    [Fact]
    public void CommitWritesComponentsBeforeSendingSiaEvents()
    {
        using var world = new World();
        var initial = new WindowState(
            new WindowSize(640, 480),
            new WindowSize(640, 480),
            false);
        var entity = world.Create(HList.From(new GlfwWindow(1), initial));
        var resizeCount = 0;
        var framebufferResizeCount = 0;
        var closeCount = 0;

        world.Dispatcher.Listen<WindowEvents.Resized>((target, in @event) => {
            Assert.Equal(@event.Size, target.Get<WindowState>().Size);
            resizeCount++;
            return false;
        });
        world.Dispatcher.Listen<WindowEvents.FramebufferResized>((target, in @event) => {
            Assert.Equal(@event.Size, target.Get<WindowState>().FramebufferSize);
            framebufferResizeCount++;
            return false;
        });
        world.Dispatcher.Listen<WindowEvents.CloseRequested>((target, in @event) => {
            Assert.True(target.Get<WindowState>().CloseRequested);
            closeCount++;
            return false;
        });

        var next = new WindowState(
            new WindowSize(800, 600),
            new WindowSize(1600, 1200),
            true);
        ref var state = ref entity.Get<WindowState>();
        GlfwWindowSystem.Commit(world, entity, ref state, in next);

        Assert.Equal(1, resizeCount);
        Assert.Equal(1, framebufferResizeCount);
        Assert.Equal(1, closeCount);
    }
}
