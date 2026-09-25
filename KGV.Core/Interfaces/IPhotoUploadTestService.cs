using System.Threading.Tasks;
using KGV.Core.Models;

namespace KGV.Core.Interfaces
{
    public interface IPhotoUploadTestService
    {
        Task<PhotoUploadTestResult> UploadAsync(PhotoUploadTestRequest request);
        /// <summary>
        /// Lädt ein Ablesungsfoto über den geschützten KGV-Drive-Proxy. Es wird
        /// dabei nicht an eine externe Google-Drive-App übergeben.
        /// </summary>
        Task<byte[]?> DownloadAblesungPhotoAsync(long ablesungId);
    }
}
