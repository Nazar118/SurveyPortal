using Microsoft.AspNetCore.Mvc;

namespace SurveyPortal.MVC.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
