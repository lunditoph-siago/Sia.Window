using System.Text;
using Sia.GLFW;
using Sia.Input;
using Sia.Window;

namespace Sia.Window.Input.Example;

internal sealed class WindowInputMonitor(ConsoleEventLog eventLog)
{
    private readonly Dictionary<EntityId, KeyboardState> _keyboards = [];
    private readonly List<Entity> _pendingCloses = [];

    public void Attach(World world)
    {
        world.Dispatcher.Listen<InputEvents.KeyPressed>(OnKeyPressed);
        world.Dispatcher.Listen<InputEvents.KeyReleased>(OnKeyReleased);
        world.Dispatcher.Listen<InputEvents.KeyRepeated>(OnKeyRepeated);
        world.Dispatcher.Listen<InputEvents.TextEntered>(OnTextEntered);
        world.Dispatcher.Listen<InputEvents.MouseButtonPressed>(OnMouseButtonPressed);
        world.Dispatcher.Listen<InputEvents.MouseButtonReleased>(OnMouseButtonReleased);
        world.Dispatcher.Listen<InputEvents.MouseMoved>(OnMouseMoved);
        world.Dispatcher.Listen<InputEvents.MouseScrolled>(OnMouseScrolled);
        world.Dispatcher.Listen<InputEvents.MouseEntered>(OnMouseEntered);
        world.Dispatcher.Listen<InputEvents.MouseExited>(OnMouseExited);
        world.Dispatcher.Listen<WindowEvents.Resized>(OnWindowResized);
        world.Dispatcher.Listen<WindowEvents.FramebufferResized>(OnFramebufferResized);
        world.Dispatcher.Listen<WorldEvents.Remove>(OnEntityRemoved);
    }

    public void PrintHelp()
    {
        Console.WriteLine("Sia.Window input example");
        Console.WriteLine("N opens another window · Escape closes the one you're focused on · F1 prints this help.");
        Console.WriteLine("Keyboard and text events are lossless; pointer motion is sampled for readability.");
        Console.WriteLine();
        eventLog.Write("window", "ready", ConsoleColor.Green);
    }

    public void FlushPendingCloses()
    {
        if (_pendingCloses.Count == 0) {
            return;
        }

        foreach (var entity in _pendingCloses) {
            if (entity.IsValid && entity.Contains<GlfwWindow>()) {
                entity.DestroyGlfwWindow();
            }
        }
        _pendingCloses.Clear();
    }

    private bool OnKeyPressed(Entity target, in InputEvents.KeyPressed @event)
    {
        if (!IsWindow(target, out var window)) {
            return false;
        }

        var keyboard = Keyboard(target);
        keyboard.Press(@event.Key);
        eventLog.Write(
            "keyboard",
            $"{Label(target)} press {@event.Key} · scan {@event.ScanCode} · mods {Describe(@event.Modifiers)} · down [{keyboard.DescribePressedKeys()}]",
            ConsoleColor.Green);

        if (@event.Key == Key.Escape) {
            eventLog.Write("window", $"{Label(target)} closing", ConsoleColor.Yellow);
            _pendingCloses.Add(target);
            return false;
        }
        if (@event.Key == Key.F1) {
            PrintHelp();
            return false;
        }

        Glfw.SetTitle(window, $"Sia.Window {Label(target)} · key down · {@event.Key}");
        return false;
    }

    private bool OnKeyReleased(Entity target, in InputEvents.KeyReleased @event)
    {
        if (!IsWindow(target, out var window)) {
            return false;
        }

        var keyboard = Keyboard(target);
        keyboard.Release(@event.Key);
        eventLog.Write(
            "keyboard",
            $"{Label(target)} release {@event.Key} · scan {@event.ScanCode} · mods {Describe(@event.Modifiers)} · down [{keyboard.DescribePressedKeys()}]",
            ConsoleColor.DarkGreen);
        Glfw.SetTitle(window, $"Sia.Window {Label(target)} · key up · {@event.Key}");
        return false;
    }

    private bool OnKeyRepeated(Entity target, in InputEvents.KeyRepeated @event)
    {
        if (!IsWindow(target, out var window)) {
            return false;
        }

        eventLog.Write(
            "keyboard",
            $"{Label(target)} repeat {@event.Key} · scan {@event.ScanCode} · mods {Describe(@event.Modifiers)} · down [{Keyboard(target).DescribePressedKeys()}]",
            ConsoleColor.Yellow);
        Glfw.SetTitle(window, $"Sia.Window {Label(target)} · key repeat · {@event.Key}");
        return false;
    }

    private bool OnTextEntered(Entity target, in InputEvents.TextEntered @event)
    {
        if (!IsWindow(target, out var window)) {
            return false;
        }

        var text = DescribeCodePoint(@event.CodePoint);
        eventLog.Write("text", $"{Label(target)} {text} · U+{@event.CodePoint:X4}", ConsoleColor.Magenta);
        Glfw.SetTitle(window, $"Sia.Window {Label(target)} · text · {text}");
        return false;
    }

    private bool OnMouseButtonPressed(Entity target, in InputEvents.MouseButtonPressed @event)
    {
        if (!IsWindow(target, out var window)) {
            return false;
        }

        eventLog.Write("mouse", $"{Label(target)} press {@event.Button} · mods {Describe(@event.Modifiers)}", ConsoleColor.Green);
        Glfw.SetTitle(window, $"Sia.Window {Label(target)} · mouse down · {@event.Button}");
        return false;
    }

    private bool OnMouseButtonReleased(Entity target, in InputEvents.MouseButtonReleased @event)
    {
        if (!IsWindow(target, out var window)) {
            return false;
        }

        eventLog.Write("mouse", $"{Label(target)} release {@event.Button} · mods {Describe(@event.Modifiers)}", ConsoleColor.DarkGreen);
        Glfw.SetTitle(window, $"Sia.Window {Label(target)} · mouse up · {@event.Button}");
        return false;
    }

    private bool OnMouseMoved(Entity target, in InputEvents.MouseMoved @event)
    {
        if (!IsWindow(target, out var window)) {
            return false;
        }

        eventLog.Write("pointer", $"{Label(target)} position ({@event.Position.X:0.0}, {@event.Position.Y:0.0})", ConsoleColor.DarkCyan);
        Glfw.SetTitle(window, $"Sia.Window {Label(target)} · pointer · {@event.Position.X:0}, {@event.Position.Y:0}");
        return false;
    }

    private bool OnMouseScrolled(Entity target, in InputEvents.MouseScrolled @event)
    {
        if (!IsWindow(target, out var window)) {
            return false;
        }

        eventLog.Write("mouse", $"{Label(target)} scroll ({@event.Delta.X:0.00}, {@event.Delta.Y:0.00})", ConsoleColor.Yellow);
        Glfw.SetTitle(window, $"Sia.Window {Label(target)} · scroll · {@event.Delta.X:0.0}, {@event.Delta.Y:0.0}");
        return false;
    }

    private bool OnMouseEntered(Entity target, in InputEvents.MouseEntered @event)
    {
        if (!IsWindow(target, out _)) {
            return false;
        }

        eventLog.Write("pointer", $"{Label(target)} entered", ConsoleColor.Cyan);
        return false;
    }

    private bool OnMouseExited(Entity target, in InputEvents.MouseExited @event)
    {
        if (!IsWindow(target, out _)) {
            return false;
        }

        eventLog.Write("pointer", $"{Label(target)} exited", ConsoleColor.DarkCyan);
        return false;
    }

    private bool OnWindowResized(Entity target, in WindowEvents.Resized @event)
    {
        if (!IsWindow(target, out _)) {
            return false;
        }

        eventLog.Write("window", $"{Label(target)} resized to {@event.Size.Width} × {@event.Size.Height}", ConsoleColor.Cyan);
        return false;
    }

    private bool OnFramebufferResized(Entity target, in WindowEvents.FramebufferResized @event)
    {
        if (!IsWindow(target, out _)) {
            return false;
        }

        eventLog.Write("framebuf", $"{Label(target)} resized to {@event.Size.Width} × {@event.Size.Height}", ConsoleColor.DarkCyan);
        return false;
    }

    private bool OnEntityRemoved(Entity target, in WorldEvents.Remove @event)
    {
        _keyboards.Remove(target.Id);
        return false;
    }

    private static bool IsWindow(Entity target, out GlfwWindow window)
    {
        if (target.IsValid && target.Contains<GlfwWindow>()) {
            window = target.Get<GlfwWindow>();
            return true;
        }
        window = default;
        return false;
    }

    private KeyboardState Keyboard(Entity target)
    {
        if (!_keyboards.TryGetValue(target.Id, out var keyboard)) {
            keyboard = new KeyboardState();
            _keyboards[target.Id] = keyboard;
        }
        return keyboard;
    }

    private static string Label(Entity target) =>
        target.Contains<WindowLabel>() ? target.Get<WindowLabel>().Name : "?";

    private static string Describe(KeyModifiers modifiers) => modifiers == 0
        ? "none"
        : modifiers.ToString();

    private static string DescribeCodePoint(uint codePoint)
    {
        if (!Rune.TryCreate((int)codePoint, out var rune)) {
            return "<invalid>";
        }

        return rune.Value switch {
            '\t' => "\\t",
            '\n' => "\\n",
            '\r' => "\\r",
            _ when Rune.IsControl(rune) => "<control>",
            _ => $"\"{rune}\"",
        };
    }
}
