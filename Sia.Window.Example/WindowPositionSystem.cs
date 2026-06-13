using Sia;
using Sia.GLFW;

namespace Sia.Window.Input.Example;

internal sealed class WindowPositionSystem(ConsoleEventLog eventLog)
    : SystemBase(Matchers.Of<GlfwWindow, WindowMovementTracker>())
{
    public override void Execute(World world, IEntityQuery query)
    {
        foreach (var entity in query) {
            if (!entity.IsValid || !entity.Contains<GlfwWindow>()) {
                continue;
            }

            var window = entity.Get<GlfwWindow>();
            var position = Glfw.GetPosition(window);
            ref var tracker = ref entity.Get<WindowMovementTracker>();

            if (tracker.LastPosition == position) {
                continue;
            }

            var wasObserved = tracker.LastPosition is not null;
            tracker.LastPosition = position;
            if (!wasObserved) {
                continue;
            }

            var label = Label(entity);
            eventLog.Write("window", $"{label} moved to ({position.X}, {position.Y})", ConsoleColor.Cyan);
            Glfw.SetTitle(window, $"Sia.Window {label} · moved · {position.X}, {position.Y}");
        }
    }

    private static string Label(Entity entity) =>
        entity.Contains<WindowLabel>() ? entity.Get<WindowLabel>().Name : "?";
}
