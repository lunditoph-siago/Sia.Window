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
        if (Environment.GetEnvironmentVariable("SIA_WINDOW_MULTI_WORLD") == "1") {
            RunMultiWorld();
            return;
        }

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

    private static void RunMultiWorld()
    {
        using var worldA = new World();
        using var worldB = new World();

        var (eventLogA, monitorA, stageA, liveWindowsA) = Setup(worldA, "A", 120);
        var (eventLogB, monitorB, stageB, liveWindowsB) = Setup(worldB, "B", 900);

        using (stageA)
        using (stageB) {
            while (worldA.Query(liveWindowsA).Count > 0 || worldB.Query(liveWindowsB).Count > 0) {
                Tick(stageA, monitorA);
                Tick(stageB, monitorB);
                Thread.Sleep(8);
            }
        }

        eventLogA.Write("app", "world A: all windows closed", ConsoleColor.DarkGray);
        eventLogB.Write("app", "world B: all windows closed", ConsoleColor.DarkGray);
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
        IEntityMatcher LiveWindows) Setup(World world, string tag = "", int originX = 120)
    {
        var eventLog = new ConsoleEventLog();
        var monitor = new WindowInputMonitor(eventLog);
        var spawner = new WindowSpawner(eventLog, tag, originX);
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
