using DaNangSafeMap.Models.DTOs;
using DaNangSafeMap.Models.Entities;
using DaNangSafeMap.Models.ViewModels.Admin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DaNangSafeMap.Services.Interfaces
{
    public interface IReportService
    {
        Task<Report> CreateReportAsync(ReportDto dto, int? reporterId);

        // Admin methods
        Task<(List<ReportAdminViewModel> Items, int TotalCount)> GetAllReportsAsync(
            string? search, int? status, int page, int pageSize);

        /// <summary>
        /// Xử lý báo cáo: soft-delete bài đăng + gửi thông báo cho chủ bài + đánh dấu report đã giải quyết.
        /// </summary>
        Task<bool> ResolveAndDeleteAsync(int reportId);

        /// <summary>
        /// Bỏ qua báo cáo (không xoá bài, chỉ đóng báo cáo).
        /// </summary>
        Task<bool> DismissReportAsync(int reportId);
    }
}