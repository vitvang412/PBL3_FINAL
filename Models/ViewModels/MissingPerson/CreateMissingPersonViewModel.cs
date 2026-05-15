using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DaNangSafeMap.Models.ViewModels.MissingPerson
{
    public class CreateMissingPersonViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [MaxLength(100, ErrorMessage = "Họ tên không quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        [Display(Name = "Giới tính")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tuổi")]
        [Range(0, 150, ErrorMessage = "Tuổi không hợp lệ")]
        [Display(Name = "Tuổi")]
        public int Age { get; set; }

        [Display(Name = "Chiều cao (cm)")]
        public int? Height { get; set; }

        [Display(Name = "Đặc điểm nhận dạng")]
        public string? Features { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa điểm mất tích")]
        [MaxLength(500, ErrorMessage = "Địa điểm không quá 500 ký tự")]
        [Display(Name = "Địa điểm mất tích")]
        public string LastSeenLocation { get; set; } = string.Empty;

        // Tọa độ bản đồ (tự điền qua map picker)
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [Required(ErrorMessage = "Vui lòng upload ảnh người mất tích")]
        [Display(Name = "Ảnh người mất tích")]
        public IFormFile? Photo { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thông tin liên hệ")]
        [MaxLength(500)]
        [Display(Name = "Thông tin liên hệ")]
        public string ContactInfo { get; set; } = string.Empty;

        [Display(Name = "Nội dung bài viết")]
        public string? Description { get; set; }
    }
}