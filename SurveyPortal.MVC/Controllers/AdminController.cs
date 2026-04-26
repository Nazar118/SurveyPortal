using Microsoft.AspNetCore.Mvc;

namespace SurveyPortal.MVC.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SurveyList()
        {
            return View();
        }

        public IActionResult Questions(int id)
        {
            ViewBag.SurveyId = id;
            return View();
        }
        // Anket Sonuçları Sayfası
        public IActionResult Results(int id)
        {
            ViewBag.SurveyId = id;
            return View();
        }
    }
}