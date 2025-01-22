using Data.Authentication.Models;
using Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PMCNet8.Controllers
{
    public class AccountController : Controller
    {
        private readonly Medihub4rumDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly JwtSettings _jwtSettings;

        public AccountController(
            Medihub4rumDbContext context,
            ILogger<AccountController> logger,
            IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _logger = logger;
            _jwtSettings = jwtSettings.Value;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                var hasHub = bool.Parse(User.FindFirst("HasHub")?.Value ?? "false");
                return RedirectToAction("Index", hasHub ? "Home" : "Sale");
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
                    .FirstOrDefaultAsync(u => u.UserName == userName &&
                                            u.Password == password);

                if (user == null)
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
                    return View();
                }

                var sponsorUser = user.SponsorUsers.FirstOrDefault();
                if (sponsorUser?.Sponsor == null)
                {
                    ModelState.AddModelError("", "Tài khoản không phải là nhà tài trợ");
                    return View();
                }

                var hasHub = await _context.SponsorHub
                    .AnyAsync(sh => sh.SponsorId == sponsorUser.Sponsor.Id);

                // Generate JWT Token
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim("SponsorId", sponsorUser.Sponsor.Id.ToString()),
                    new Claim("SponsorName", sponsorUser.Sponsor.Name),
                    new Claim("HasHub", hasHub.ToString())
                };

                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationInMinutes),
                    signingCredentials: creds
                );

                var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
                var refreshToken = GenerateRefreshToken();

                // Save refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays);
                await _context.SaveChangesAsync();

                // Set cookies
                SetTokenCookies(accessToken, refreshToken);

                return RedirectToAction("Index", hasHub ? "Home" : "Sale");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình đăng nhập");
                ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình đăng nhập");
                return View();
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.UserAccount.FindAsync(Guid.Parse(userId));
                if (user != null)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = null;
                    await _context.SaveChangesAsync();
                }
            }

            RemoveTokenCookies();
            return Json(new { success = true, redirectUrl = Url.Action("Login", "Account") });
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private void SetTokenCookies(string accessToken, string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            };

            cookieOptions.Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationInMinutes);
            Response.Cookies.Append("X-Access-Token", accessToken, cookieOptions);

            cookieOptions.Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays);
            Response.Cookies.Append("X-Refresh-Token", refreshToken, cookieOptions);
        }

        private void RemoveTokenCookies()
        {
            Response.Cookies.Delete("X-Access-Token");
            Response.Cookies.Delete("X-Refresh-Token");
        }
    }
}