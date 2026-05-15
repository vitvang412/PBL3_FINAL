namespace DaNangSafeMap.Models.DTOs
{
    public class ReportDto
    {
        public int TargetId { get; set; }
        public string TargetType { get; set; } = "MissingPerson";
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}