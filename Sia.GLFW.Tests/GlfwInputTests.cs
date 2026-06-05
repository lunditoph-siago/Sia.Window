using Sia;
using Sia.Input;

namespace Sia.GLFW.Tests;

public sealed class GlfwInputTests
{
    [Fact]
    public void SendKeyTranslatesEveryActionIntoAMatchingEvent()
    {
        using var world = new World();
        var entity = world.Create(HList.From(new GlfwWindow(1)));
        var pressed = 0;
        var released = 0;
        var repeated = 0;

        world.Dispatcher.Listen<InputEvents.KeyPressed>((target, in @event) => {
            Assert.Equal(Key.Space, @event.Key);
            Assert.Equal(57, @event.ScanCode);
            Assert.Equal(KeyModifiers.Shift, @event.Modifiers);
            pressed++;
            return false;
        });
        world.Dispatcher.Listen<InputEvents.KeyReleased>((target, in @event) => {
            released++;
            return false;
        });
        world.Dispatcher.Listen<InputEvents.KeyRepeated>((target, in @event) => {
            repeated++;
            return false;
        });

        GlfwModule.SendKey(
            world, entity, Key.Space, 57, InputAction.Press, KeyModifiers.Shift);
        GlfwModule.SendKey(
            world, entity, Key.Space, 57, InputAction.Release, KeyModifiers.Shift);
        GlfwModule.SendKey(
            world, entity, Key.Space, 57, InputAction.Repeat, KeyModifiers.Shift);

        Assert.Equal(1, pressed);
        Assert.Equal(1, released);
        Assert.Equal(1, repeated);
    }

    [Fact]
    public void SendMouseButtonIgnoresRepeatActions()
    {
        using var world = new World();
        var entity = world.Create(HList.From(new GlfwWindow(1)));
        var pressed = 0;
        var released = 0;

        world.Dispatcher.Listen<InputEvents.MouseButtonPressed>((target, in @event) => {
            Assert.Equal(MouseButton.Left, @event.Button);
            pressed++;
            return false;
        });
        world.Dispatcher.Listen<InputEvents.MouseButtonReleased>((target, in @event) => {
            released++;
            return false;
        });

        GlfwModule.SendMouseButton(
            world, entity, MouseButton.Left, InputAction.Press, default);
        GlfwModule.SendMouseButton(
            world, entity, MouseButton.Left, InputAction.Release, default);
        GlfwModule.SendMouseButton(
            world, entity, MouseButton.Left, InputAction.Repeat, default);

        Assert.Equal(2, pressed + released);
        Assert.Equal(1, pressed);
        Assert.Equal(1, released);
    }
}
