using Data.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class AccountController : Controller
{
    private readonly Medihub4rumDbContext _context;

    public AccountController(Medihub4rumDbContext context)
    {
        _context = context;
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
        var user = await _context.UserAccount
         .FirstOrDefaultAsync(u => u.UserName == userName && u.Password == password);

        if (user != null)
        {
            var sponsorUser = await _context.SponsorUser
                .FirstOrDefaultAsync(su => su.UserId == user.Id);

            if (sponsorUser != null)
            {
                var sponsor = await _context.Sponsor
                    .FirstOrDefaultAsync(s => s.Id == sponsorUser.SponsorId);

                if (sponsor != null)
                {
                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("SponsorId", sponsorUser.SponsorId.ToString());
                    HttpContext.Session.SetString("SponsorName", sponsor.Name);
                    return RedirectToAction("Index", "Home");
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

        return View();
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Json(new { success = true, redirectUrl = Url.Action("Login", "Account") });
    }
}