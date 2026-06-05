using Sia;
using Sia.Window;

namespace Sia.GLFW;

public static class GlfwWorldExtensions
{
    /// <summary>Creates a GLFW-backed window entity owned by the world.</summary>
    public static Entity CreateGlfwWindow(
        this World world,
        in WindowDescriptor descriptor,
        in GlfwWindowOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(world);
        var module = world.AcquireAddon<GlfwModule>();
        var window = Glfw.CreateWindow(in descriptor, in options);

        try {
            var state = Glfw.ReadWindowState(window);
            var entity = world.Create(HList.From(window, state));
            module.Own(entity, window);
            return entity;
        }
        catch {
            Glfw.DestroyWindow(ref window);
            throw;
        }
    }

    /// <summary>Destroys the resource entity and its owned native window.</summary>
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
