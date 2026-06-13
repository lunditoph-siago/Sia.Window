using System.Text;
using Sia;
using Sia.GLFW;

namespace Sia.Window.Input.Example;

public static class Program
{
#if !BROWSER
    public static int Main()
    {
        try {
            Console.OutputEncoding = Encoding.UTF8;
            Run();
            return 0;
        }
        catch (Exception exception) {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Run()
    {
        using var world = new World();
        var (eventLog, monitor, stage, liveWindows) = Setup(world);

        using (stage) {
            while (world.Query(liveWindows).Count > 0) {
                Tick(stage, monitor);
                Thread.Sleep(8);
            }
        }

        eventLog.Write("app", "all windows closed, exiting", ConsoleColor.DarkGray);
    }
#else
    public static async Task<int> Main()
    {
        try {
            await RunAsync();
            return 0;
        }
        catch (Exception exception) {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static async Task RunAsync()
    {
        using var world = new World();
        var (eventLog, monitor, stage, liveWindows) = Setup(world);

        using (stage) {
            while (world.Query(liveWindows).Count > 0) {
                Tick(stage, monitor);
                await Task.Delay(8);
            }
        }

        eventLog.Write("app", "all windows closed, exiting", ConsoleColor.DarkGray);
    }
#endif

    private static (
        ConsoleEventLog EventLog,
        WindowInputMonitor Monitor,
        SystemStage Stage,
        IEntityMatcher LiveWindows) Setup(World world)
    {
        var eventLog = new ConsoleEventLog();
        var monitor = new WindowInputMonitor(eventLog);
        var spawner = new WindowSpawner(eventLog);
        monitor.Attach(world);
        spawner.Attach(world);

        var stage = SystemChain.Empty
            .AddGlfw()
            .Add<WindowPositionSystem>(() => new WindowPositionSystem(eventLog))
            .CreateStage(world);

        spawner.Spawn(world);
        monitor.PrintHelp();

        return (eventLog, monitor, stage, Matchers.Of<GlfwWindow>());
    }

    private static void Tick(SystemStage stage, WindowInputMonitor monitor)
    {
        stage.Tick();
        monitor.FlushPendingCloses();
    }
}
