using Sia;
using Sia.Window;

namespace Sia.GLFW;

public static class GlfwWorldExtensions
{
    public static Entity CreateGlfwWindow(
        this World world,
        in WindowDescriptor descriptor,
        in GlfwWindowOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(world);
        var module = world.AcquireAddon<GlfwModule>();
        var window = Glfw.CreateWindow(in descriptor, in options);

        Entity entity = default!;
        var entityCreated = false;

        try {
            var state = Glfw.ReadWindowState(window);
            entity = world.Create(HList.From(window, state));
            entityCreated = true;
            module.Own(entity, window);
            return entity;
        }
        catch (Exception creationError) {
            List<Exception>? cleanupErrors = null;

            if (entityCreated && entity.IsValid) {
                try {
                    entity.Destroy();
                }
                catch (Exception cleanupError) {
                    (cleanupErrors ??= []).Add(cleanupError);
                }
            }

            try {
                Glfw.DestroyWindow(ref window);
            }
            catch (Exception cleanupError) {
                (cleanupErrors ??= []).Add(cleanupError);
            }

            if (cleanupErrors is not null) {
                cleanupErrors.Insert(0, creationError);
                throw new AggregateException(
                    "GLFW window creation failed and rollback was incomplete.",
                    cleanupErrors);
            }

            throw;
        }
    }

    public static void DestroyGlfwWindow(this Entity entity)
    {
        if (!entity.IsValid) {
            return;
        }
        if (!entity.Contains<GlfwWindow>()) {
            throw new ArgumentException(
                "The entity does not contain a GLFW window.", nameof(entity));
        }

        entity.Destroy();
    }

    public static SystemChain AddGlfw(this SystemChain chain) =>
        chain.Add<GlfwWindowSystem>();
}
