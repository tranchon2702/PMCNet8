using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PMCNet8.Controllers
{
    public abstract class BaseController : Controller
    {
       
        protected Guid GetSponsorId()
        {
            var sponsorIdClaim = User.FindFirst("SponsorId")?.Value;
            return sponsorIdClaim != null ? Guid.Parse(sponsorIdClaim) : Guid.Empty;
        }

      
        protected string GetSponsorName()
        {
            return User.FindFirst("SponsorName")?.Value ?? string.Empty;
        }

       
        protected bool HasHub()
        {
            return bool.Parse(User.FindFirst("HasHub")?.Value ?? "false");
        }
    }
}
