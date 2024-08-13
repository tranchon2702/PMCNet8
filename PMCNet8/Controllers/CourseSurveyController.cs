using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PMCNet8.Controllers
{
   
    public class CourseSurveyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

