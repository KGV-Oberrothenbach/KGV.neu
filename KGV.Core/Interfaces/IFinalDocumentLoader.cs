using KGV.Core.Models;

namespace KGV.Core.Interfaces;

public interface IFinalDocumentLoader
{
    Task<LoadedDocument> LoadDokumentByIdAsync(long documentId);
}
