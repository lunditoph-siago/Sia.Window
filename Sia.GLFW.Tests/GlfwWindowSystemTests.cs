using System.Runtime.CompilerServices;

using Sia;
using Sia.Window;

namespace Sia.GLFW.Tests;

public sealed class GlfwWindowSystemTests
{
    private readonly record struct Migrated;

    private sealed class ResizeReactionSystem(Action<Entity> onResize)
        : SystemBase(
            Matchers.Of<WindowState>(),
            EventUnion.Of<WindowEvents.Resized>())
    {
        public override void Execute(World world, IEntityQuery query)
        {
            foreach (var entity in query) {
                onResize(entity);
            }
        }
    }

    [Fact]
    public void WindowCapabilityIsAnUnmanagedEcsValue()
    {
        Assert.False(RuntimeHelpers.IsReferenceOrContainsReferences<GlfwWindow>());
        Assert.True(default(GlfwWindow).IsNull);
        Assert.False(new GlfwWindow(42, 0).IsNull);
    }

    [Fact]
    public void CommitWritesComponentsBeforeSendingSiaEvents()
    {
        using var world = new World();
        var initial = new WindowState(
            new WindowSize(640, 480),
            new WindowSize(640, 480),
            false);
        var entity = world.Create(HList.From(new GlfwWindow(1, 0), initial));
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

    [Fact]
    public void ReactiveQuerySurvivesHostMigrationAndRepeatedBatches()
    {
        const int entityCount = 512;
        using var world = new World();
        var observed = new Dictionary<EntityId, WindowSize>();
        var reaction = new ResizeReactionSystem(entity => {
            observed.Add(entity.Id, entity.Get<WindowState>().Size);
        });
        using var stage = SystemChain.Empty
            .Add<ResizeReactionSystem>(() => reaction)
            .CreateStage(world);

        var initial = new WindowState(
            new WindowSize(640, 480),
            new WindowSize(640, 480),
            false);
        var entities = Enumerable.Range(0, entityCount)
            .Select(index => world.Create(HList.From(
                new GlfwWindow(index + 1, 0), initial)))
            .ToArray();

        CommitBatch(world, entities, 800, 600);
        stage.Tick();

        Assert.Equal(entityCount, observed.Count);
        Assert.All(observed.Values, size => Assert.Equal(new WindowSize(800, 600), size));

        observed.Clear();
        foreach (var entity in entities) {
            entity.Add(new Migrated());
        }

        CommitBatch(world, entities, 1024, 768);
        stage.Tick();

        Assert.Equal(entityCount, observed.Count);
        Assert.All(observed.Values, size => Assert.Equal(new WindowSize(1024, 768), size));

        observed.Clear();
        stage.Tick();
        Assert.Empty(observed);
    }

    private static void CommitBatch(
        World world,
        IEnumerable<Entity> entities,
        int width,
        int height)
    {
        var next = new WindowState(
            new WindowSize(width, height),
            new WindowSize(width, height),
            false);

        foreach (var entity in entities) {
            ref var state = ref entity.Get<WindowState>();
            GlfwWindowSystem.Commit(world, entity, ref state, in next);
        }
    }
}
