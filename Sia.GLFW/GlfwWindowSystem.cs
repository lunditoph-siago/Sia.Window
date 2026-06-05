using Sia;
using Sia.Window;

namespace Sia.GLFW;

/// <summary>Polls GLFW once and projects native state changes into Sia events.</summary>
public sealed class GlfwWindowSystem()
    : SystemBase(Matchers.Of<GlfwWindow, WindowState>())
{
    public override void Initialize(World world)
    {
        world.AcquireAddon<GlfwModule>();
    }

    public override void Execute(World world, IEntityQuery query)
    {
        if (query.Count == 0) {
            return;
        }

        Glfw.PollEvents();
        GlfwModule.ThrowPendingCallbackErrors();

        foreach (var entity in query) {
            ref var window = ref entity.Get<GlfwWindow>();
            ref var state = ref entity.Get<WindowState>();
            var next = Glfw.ReadWindowState(window);
            Commit(world, entity, ref state, in next);
        }
    }

    internal static void Commit(
        World world,
        Entity entity,
        ref WindowState state,
        in WindowState next)
    {
        var changes = state.Apply(in next);
        if ((changes & WindowChanges.Size) != 0) {
            world.Send(entity, new WindowEvents.Resized(next.Size));
        }
        if ((changes & WindowChanges.FramebufferSize) != 0) {
            world.Send(entity, new WindowEvents.FramebufferResized(next.FramebufferSize));
        }
        if ((changes & WindowChanges.CloseRequested) != 0) {
            world.Send(entity, new WindowEvents.CloseRequested());
        }
    }
}
