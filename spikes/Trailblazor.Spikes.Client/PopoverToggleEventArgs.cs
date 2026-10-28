using Microsoft.AspNetCore.Components;

namespace Trailblazor.Spikes.Client;

[EventHandler("onpopovertoggle", typeof(PopoverToggleEventArgs))]
public static class EventHandlers;

public class PopoverToggleEventArgs : EventArgs
{
    public string NewState { get; set; } = "";
}
