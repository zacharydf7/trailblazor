namespace Trailblazor;

// The hotkeys on the page, in the order they first rendered, which a TbHotkeysHelp lists.
internal sealed class HotkeyRegistry
{
    private readonly List<TbHotkey> _hotkeys = [];

    public event Action? Changed;

    public IReadOnlyList<TbHotkey> Hotkeys => _hotkeys;

    public void Add(TbHotkey hotkey)
    {
        _hotkeys.Add(hotkey);
        Changed?.Invoke();
    }

    public void Remove(TbHotkey hotkey)
    {
        if (_hotkeys.Remove(hotkey))
        {
            Changed?.Invoke();
        }
    }

    // A hotkey's keys or description changed.
    public void Update() => Changed?.Invoke();
}
