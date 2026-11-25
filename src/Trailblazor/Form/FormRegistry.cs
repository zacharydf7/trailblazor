namespace Trailblazor;

// The forms on the page by id, so a submit or reset button whose form attribute names a form, inside it or not, can
// follow that form's busy and disabled state.
internal sealed class FormRegistry
{
    private readonly Dictionary<string, TbForm> _forms = new(StringComparer.Ordinal);

    // Raised with a form's id when the form comes or goes, starts or stops submitting, or turns on or off.
    public event Action<string>? Changed;

    public TbForm? Find(string id) => _forms.GetValueOrDefault(id);

    public void Add(string id, TbForm form)
    {
        _forms[id] = form;
        Changed?.Invoke(id);
    }

    // A form removes only itself, so one that took the id after it keeps it.
    public void Remove(string id, TbForm form)
    {
        if (_forms.TryGetValue(id, out var known) && known == form)
        {
            _forms.Remove(id);
            Changed?.Invoke(id);
        }
    }

    public void Change(string id) => Changed?.Invoke(id);
}
