using System.Threading;
using System.Threading.Tasks;
using KGV.Core.Models;

namespace KGV.Core.Interfaces;

public interface IPdfTemplateProvider
{
    Task<PdfTemplateAsset> GetActiveAsync(string templateKey, string expectedFieldSpecVersion, string expectedRendererVersion, CancellationToken cancellationToken = default);
}
