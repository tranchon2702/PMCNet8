using Data.Authentication.Models;
using Data.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog.Web;
using System.Text;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add JWT Settings
        builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection("JwtSettings"));

        // Add JWT Authentication
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                    ValidAudience = builder.Configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["X-Access-Token"];
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogError(context.Exception, "Token validation failed");
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddControllersWithViews();

        // Add DbContexts
        builder.Services.AddDbContext<Medihub4rumDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("MediHub4rumConnectionString"),o => o.UseCompatibilityLevel(120)));

        builder.Services.AddDbContext<LogActionDbContext>(options =>
           options.UseSqlServer(builder.Configuration.GetConnectionString("LogActionConnectionString"), o => o.UseCompatibilityLevel(120)));

        builder.Services.AddDbContext<MedihubSCAppDbContext>(options =>
          options.UseSqlServer(builder.Configuration.GetConnectionString("MediHubSCAppConnectionString"), o => o.UseCompatibilityLevel(120)));
       

        builder.Host.UseNLog();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();
        app.UseCookiePolicy();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Account}/{action=Login}/{id?}");

        app.Run();
    }
}