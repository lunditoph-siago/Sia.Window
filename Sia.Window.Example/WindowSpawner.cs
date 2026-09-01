using Sia;
using Sia.GLFW;
using Sia.Input;
using Sia.Window;

namespace Sia.Window.Input.Example;

internal sealed class WindowSpawner(ConsoleEventLog eventLog, string tag = "", int originX = 120)
{
    private int _count;

    public void Attach(World world) =>
        world.Dispatcher.Listen<InputEvents.KeyPressed>((target, in @event) => {
            if (@event.Key == Key.N) {
                Spawn(world);
            }
            return false;
        });

    public Entity Spawn(World world)
    {
        var index = ++_count;
        var offset = (index - 1) * 40;
        var label = $"{tag}#{index}";

        var entity = world.CreateGlfwWindow(
            new WindowDescriptor(
                Width: 640,
                Height: 400,
                Title: $"Sia.Window {label}",
                Resizable: true),
            HList.From(new WindowLabel(label), new WindowMovementTracker()),
            new GlfwWindowOptions(ClientApi.NoApi));

        Glfw.SetPosition(entity.Get<GlfwWindow>(), new WindowPoint(originX + offset, 120 + offset));
        eventLog.Write("window", $"{label} opened · N opens another, Escape closes it", ConsoleColor.Green);
        return entity;
    }
}
