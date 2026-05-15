using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DaNangSafeMap.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AlertModerationController : Controller
    {
        // TODO: Thêm logic kiểm duyệt cảnh báo tại đây
    }
}