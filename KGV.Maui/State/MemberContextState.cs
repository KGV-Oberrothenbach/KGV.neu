using KGV.Core.Models;

namespace KGV.Maui.State;

public sealed class MemberContextState
{
    public event EventHandler? Changed;

    public MemberDTO? SelectedMember { get; private set; }

    public void SetSelectedMember(MemberDTO? member)
    {
        SelectedMember = member?.Clone();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        SelectedMember = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
