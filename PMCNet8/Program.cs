using Data.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
namespace PMCNet8
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
         
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<Medihub4rumDbContext>(options =>
               options.UseSqlServer(builder.Configuration.GetConnectionString("MediHub4rumConnectionString"), o => o.UseCompatibilityLevel(120))
           );
            builder.Services.AddDbContext<LogActionDbContext>(options =>
              options.UseSqlServer(builder.Configuration.GetConnectionString("LogActionConnectionString"), o => o.UseCompatibilityLevel(120))
          );
            builder.Services.AddDbContext<MedihubSCAppDbContext>(options =>
             options.UseSqlServer(builder.Configuration.GetConnectionString("MediHubSCAppConnectionString"), o => o.UseCompatibilityLevel(120))
         );

            builder.Host.UseNLog();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseRouting();
            app.UseSession();
            
            app.UseAuthorization();
           
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");
            app.MapControllers();
            app.Run();
        }
    }
}
