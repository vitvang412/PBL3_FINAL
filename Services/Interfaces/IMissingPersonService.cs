using DaNangSafeMap.Models.Entities;
using DaNangSafeMap.Models.ViewModels.MissingPerson;

namespace DaNangSafeMap.Services.Interfaces
{
    public interface IMissingPersonService
    {
        Task<List<MissingPerson>> GetAllActiveAsync();
        Task<MissingPersonIndexViewModel> SearchAsync(string? keyword, string? ageGroup, string? gender, int? daysAgo, string sortBy, int? currentUserId = null);
        Task<MissingPerson?> GetByIdAsync(int id);
        Task<MissingPerson> CreateAsync(CreateMissingPersonViewModel model, int userId, string imageUrl);
        Task<bool> DeleteAsync(int id, int userId);
        Task<bool> MarkResolvedAsync(int id, int userId);   // Chức năng 5: đánh dấu đã tìm thấy
    }
}