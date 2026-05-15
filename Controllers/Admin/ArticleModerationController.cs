using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DaNangSafeMap.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class ArticleModerationController : Controller
    {
        // TODO: Thêm logic kiểm duyệt bài viết tại đây
    }
}