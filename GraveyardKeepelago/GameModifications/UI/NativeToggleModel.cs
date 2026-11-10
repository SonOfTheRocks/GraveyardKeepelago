using System;

namespace GraveyardKeepelago.GameModifications.UI;

public sealed class NativeToggleModel
{
    public bool Value { get; set; }
    public bool Editable { get; set; } = true;
    public string DisplayText => Value ? "Enabled" : "Disabled";
    public event Action<bool> Changed;

    // In a two-choice selector both arrows wrap to the other state.
    public void Toggle()
    {
        if (!Editable)
            return;
        Value = !Value;
        Changed?.Invoke(Value);
    }
}
