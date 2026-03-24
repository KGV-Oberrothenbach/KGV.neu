using KGV.Core.Models;

namespace KGV.Maui.State;

public sealed class MemberContextState
{
    public MemberDTO? SelectedMember { get; private set; }

    public void SetSelectedMember(MemberDTO? member)
    {
        SelectedMember = member?.Clone();
    }

    public void Clear()
    {
        SelectedMember = null;
    }
}
