using System.ComponentModel.DataAnnotations;

namespace DaNangSafeMap.Models.ViewModels.Alert
{
    /// <summary>
    /// ViewModel nhận dữ liệu từ form báo cáo sự cố (POST).
    /// </summary>
    public class CreateAlertViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn loại sự cố")]
        public int AlertTypeId { get; set; }

        [Required(ErrorMessage = "Vui lòng ghim vị trí trên bản đồ")]
        public decimal Latitude { get; set; }

        [Required(ErrorMessage = "Vui lòng ghim vị trí trên bản đồ")]
        public decimal Longitude { get; set; }

        [MaxLength(500)]
        public string? AddressText { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả chi tiết")]
        [MinLength(20, ErrorMessage = "Mô tả phải có ít nhất 20 ký tự")]
        public string Description { get; set; } = string.Empty;

        public DateTime? IncidentTime { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Vui lòng xác nhận trách nhiệm")]
        public bool UserConfirmed { get; set; }
    }
}
