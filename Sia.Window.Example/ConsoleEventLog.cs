using System.Diagnostics;

namespace Sia.Window.Input.Example;

internal sealed class ConsoleEventLog
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private int _sequence;

    public void Write(string category, string message, ConsoleColor color)
    {
        var prefix = $"[{++_sequence:0000} {_clock.Elapsed.TotalSeconds,8:0.000}]";
#if !BROWSER
        if (!Console.IsOutputRedirected) {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(prefix);
            Console.ForegroundColor = color;
            Console.Write($" {category,-8}");
            Console.ForegroundColor = previousColor;
            Console.WriteLine($" {message}");
            return;
        }
#endif
        Console.WriteLine($"{prefix} {category,-8} {message}");
    }
}
