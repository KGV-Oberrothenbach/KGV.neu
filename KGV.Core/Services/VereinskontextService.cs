using KGV.Core.Interfaces;
using KGV.Core.Models;

namespace KGV.Core.Services;

public sealed class VereinskontextService : IVereinskontext
{
    public Vereinskontext? Aktuell { get; private set; }
    public bool IstAusgewaehlt => Aktuell is not null;

    public void Setzen(Vereinskontext kontext)
    {
        ArgumentNullException.ThrowIfNull(kontext);
        Aktuell = kontext;
    }

    public void Loeschen() => Aktuell = null;
}
