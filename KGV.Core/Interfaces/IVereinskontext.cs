using KGV.Core.Models;

namespace KGV.Core.Interfaces;

public interface IVereinskontext
{
    Vereinskontext? Aktuell { get; }
    bool IstAusgewaehlt { get; }
    void Setzen(Vereinskontext kontext);
    void Loeschen();
}
