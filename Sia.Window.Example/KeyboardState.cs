using Sia.Input;

namespace Sia.Window.Input.Example;

internal sealed class KeyboardState
{
    private readonly SortedSet<Key> _down = [];

    public bool Press(Key key) => _down.Add(key);

    public bool Release(Key key) => _down.Remove(key);

    public string DescribePressedKeys() => _down.Count == 0
        ? "none"
        : string.Join(" + ", _down);
}
