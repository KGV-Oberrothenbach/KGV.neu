using System.Threading.Tasks;
using KGV.Core.Models;

namespace KGV.Core.Interfaces
{
    public interface IPhotoUploadTestService
    {
        Task<PhotoUploadTestResult> UploadAsync(PhotoUploadTestRequest request);
    }
}
