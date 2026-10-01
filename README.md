# Sia.Window

[![Test](https://github.com/lunditoph-siago/Sia.Window/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/lunditoph-siago/Sia.Window/actions/workflows/test.yml)

A small window and input layer for [Sia.NET](https://github.com/lunditoph-siago/Sia.NET).
Window state lives in ECS components. Input arrives as typed Sia events. GLFW
owns the platform connection; your application owns the frame loop and rendering.

## Getting started

```sh
dotnet add package Sia.GLFW
dotnet add package Sia.GLFW.Native
```

The native package provides the GLFW binary. Browser builds use Emscripten's GLFW
implementation instead. See the [native example](Sia.Window.Example/Program.cs)
and [browser project](Sia.Window.Example/Sia.Window.Example.Browser.csproj).

```csharp
using Sia;
using Sia.GLFW;
using Sia.Input;
using Sia.Window;

using var world = new World();
using var stage = SystemChain.Empty.AddGlfw().CreateStage(world);

var window = world.CreateGlfwWindow(
    new WindowDescriptor(1280, 720, "Sia"),
    new GlfwWindowOptions(ClientApi.NoApi));

world.Dispatcher.Listen<InputEvents.KeyPressed>((entity, in input) => {
    if (input.Key == Key.Escape) {
        Glfw.RequestClose(entity.Get<GlfwWindow>());
    }
    return false;
});

while (window.IsValid && !window.Get<WindowState>().CloseRequested) {
    stage.Tick();
    // Update and render here. Use your application's frame pacing.
    Thread.Sleep(1);
}

if (window.IsValid) window.DestroyGlfwWindow();
```
