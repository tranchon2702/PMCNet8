using Data.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

public class AccountController : Controller
{
    private readonly Medihub4rumDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(Medihub4rumDbContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string userName, string password)
    {
        try
        {
            var user = await _context.UserAccount
                .Include(u => u.SponsorUsers)
                    .ThenInclude(su => su.Sponsor)
                .FirstOrDefaultAsync(u => u.UserName == userName && u.Password == password);

            if (user != null)
            {
                var sponsorUser = user.SponsorUsers.FirstOrDefault();
                if (sponsorUser != null)
                {
                    var sponsor = sponsorUser.Sponsor;
                    if (sponsor != null)
                    {
                        HttpContext.Session.SetString("UserId", user.Id.ToString());
                        HttpContext.Session.SetString("SponsorId", sponsor.Id.ToString());
                        HttpContext.Session.SetString("SponsorName", sponsor.Name);
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        _logger.LogWarning($"Sponsor not found for SponsorUser with UserId: {user.Id}");
                        ModelState.AddModelError("", "Không tìm thấy thông tin nhà tài trợ.");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Tài khoản không phải là nhà tài trợ.");
                }
            }
            else
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong quá trình đăng nhập");
            ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại sau.");
        }

        return View();
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Json(new { success = true, redirectUrl = Url.Action("Login", "Account") });
    }
}