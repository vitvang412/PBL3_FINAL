using System;
using System.Collections.Generic;

namespace DaNangSafeMap.Models.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public IReadOnlyList<AdminDashboardUserItemViewModel> Users { get; set; } = Array.Empty<AdminDashboardUserItemViewModel>();

        public string Search { get; set; } = string.Empty;
        public string RoleFilter { get; set; } = string.Empty;
        public string StatusFilter { get; set; } = string.Empty;

        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int FilteredCount { get; set; }

        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int BannedUsers { get; set; }
        public int AdminUsers { get; set; }
    }

    public class AdminDashboardUserItemViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string AuthProvider { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsCurrentAdmin { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
    }
}
