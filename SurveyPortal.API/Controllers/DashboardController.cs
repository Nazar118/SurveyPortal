using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.Models;
using SurveyPortal.API.Repositories.Interfaces;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IGenericRepository<Survey> _surveyRepo;
        private readonly IGenericRepository<Question> _questionRepo;
        private readonly IGenericRepository<Answer> _answerRepo;

        public DashboardController(IGenericRepository<Survey> surveyRepo,
                                   IGenericRepository<Question> questionRepo,
                                   IGenericRepository<Answer> answerRepo)
        {
            _surveyRepo = surveyRepo;
            _questionRepo = questionRepo;
            _answerRepo = answerRepo;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var surveys = await _surveyRepo.GetAllAsync();
            var questions = await _questionRepo.GetAllAsync();
            var answers = await _answerRepo.GetAllAsync();

            var latestSurveys = surveys
                .OrderByDescending(s => s.CreatedDate)
                .Take(5)
                .Select(s => new {
                    id = s.Id,
                    title = s.Title,
                    createdDate = s.CreatedDate,
                    isActive = s.IsActive
                }).ToList();

            return Ok(new
            {
                totalSurveys = surveys.Count(),
                totalQuestions = questions.Count(),
                totalResponses = answers.Count(),
                latestSurveys = latestSurveys 
            });
        }
    }
}