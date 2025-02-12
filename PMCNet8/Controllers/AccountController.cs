﻿using Azure.Core;
using Azure;
using Data.Authentication.Models;
using Data.Data;
using Data.Medihub4rumEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using Microsoft.EntityFrameworkCore;

public class AccountController : Controller
{
    private readonly Medihub4rumDbContext _context;
    private readonly ILogger<AccountController> _logger;
    private readonly JwtSettings _jwtSettings;

    public AccountController(Medihub4rumDbContext context, ILogger<AccountController> logger, IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        try
        {
            var refreshToken = Request.Cookies["X-Refresh-Token"];
            var rememberMe = Request.Cookies["X-Remember-Me"] == "True";

            if (!string.IsNullOrEmpty(refreshToken))
            {
                if (!rememberMe)
                {
                    RemoveTokenCookies();
                    return View();
                }

                var user = await _context.UserAccount
                    .Include(u => u.SponsorUsers)
                        .ThenInclude(su => su.Sponsor)
                    .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken &&
                        u.RefreshTokenExpiryTime > DateTime.Now);

                if (user != null)
                {
                    var sponsorUser = user.SponsorUsers.FirstOrDefault();
                    if (sponsorUser?.Sponsor != null)
                    {
                        var hasHub = await _context.SponsorHub
                            .AnyAsync(sh => sh.SponsorId == sponsorUser.Sponsor.Id);

                        var (accessToken, newRefreshToken) = GenerateTokens(user, sponsorUser.Sponsor, hasHub);

                        user.RefreshToken = newRefreshToken;
                        user.RefreshTokenExpiryTime = DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpirationInDays);
                        await _context.SaveChangesAsync();

                        SetTokenCookies(accessToken, newRefreshToken, rememberMe);

                        return RedirectToAction("Index", hasHub ? "Home" : "Sale");
                    }
                }
            }
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trong quá trình tự động đăng nhập");
            return View();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Login(string userName, string password, bool rememberMe)
    {
        try
        {
            var user = await _context.UserAccount
                .Include(u => u.SponsorUsers)
                    .ThenInclude(su => su.Sponsor)
                .FirstOrDefaultAsync(u => u.UserName == userName && u.Password == password);

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

            var (accessToken, refreshToken) = GenerateTokens(user, sponsorUser.Sponsor, hasHub);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpirationInDays);
            await _context.SaveChangesAsync();

            SetTokenCookies(accessToken, refreshToken, rememberMe);

            return RedirectToAction("Index", hasHub ? "Home" : "Sale");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trong quá trình đăng nhập");
            ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình đăng nhập");
            return View();
        }
    }

    private (string accessToken, string refreshToken) GenerateTokens(UserAccount user, Sponsor sponsor, bool hasHub)
    {
        var claims = new List<Claim>
       {
           new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
           new Claim(ClaimTypes.Name, user.UserName),
           new Claim("SponsorId", sponsor.Id.ToString()),
           new Claim("SponsorName", sponsor.Name),
           new Claim("HasHub", hasHub.ToString())
       };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(_jwtSettings.AccessTokenExpirationInMinutes),
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), GenerateRefreshToken());
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

    private void SetTokenCookies(string accessToken, string refreshToken, bool rememberMe)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = rememberMe ? DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpirationInDays) : null
        };

        Response.Cookies.Append("X-Remember-Me", rememberMe.ToString(), cookieOptions);
        Response.Cookies.Append("X-Access-Token", accessToken, cookieOptions);
        Response.Cookies.Append("X-Refresh-Token", refreshToken, cookieOptions);
    }

    private void RemoveTokenCookies()
    {
        Response.Cookies.Delete("X-Access-Token");
        Response.Cookies.Delete("X-Refresh-Token");
        Response.Cookies.Delete("X-Remember-Me");
    }
}