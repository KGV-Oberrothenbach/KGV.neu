using System.Threading;
using System.Threading.Tasks;
using KGV.Core.Models;

namespace KGV.Core.Interfaces;

public interface IPdfTemplateProvider
{
    Task<PdfTemplateAsset> GetActiveAsync(PdfTemplateRequest request, CancellationToken cancellationToken = default);
}
