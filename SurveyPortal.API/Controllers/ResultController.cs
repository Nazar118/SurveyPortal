using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.Data;
using SurveyPortal.API.Services;
using Microsoft.EntityFrameworkCore;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] 
    public class ResultController : ControllerBase
    {
        private readonly IResultService _resultService;

        public ResultController(IResultService resultService)
        {
            _resultService = resultService;
        }

        [HttpGet("{surveyId}")]
        public async Task<IActionResult> GetResults(int surveyId)
        {
            var results = await _resultService.GetSurveyResultsAsync(surveyId);

            if (results == null)
                return NotFound("Anket bulunamadı veya sonuç hesaplanamıyor.");

            return Ok(results);
        }
        [HttpGet("users/{surveyId}")]
        public async Task<IActionResult> GetSurveyUsers(int surveyId, [FromServices] AppDbContext context)
        {
            var answers = await context.Answers
                .Include(a => a.SurveyResponse)
                    .ThenInclude(sr => sr.AppUser)
                .Include(a => a.Question)
                .Include(a => a.Option)
                .Where(a => a.Question != null && a.Question.SurveyId == surveyId)
                .ToListAsync();

            var usersData = answers
                .Where(a => a.SurveyResponse != null)
                .GroupBy(a => a.SurveyResponse!.AppUserId)
                .Select(g => new {
                    userId = g.Key ?? Guid.NewGuid().ToString(),

                    userName = g.FirstOrDefault()?.SurveyResponse?.AppUser?.UserName ?? "Anonim Kullanıcı",

                    submissionDate = g.FirstOrDefault()?.SurveyResponse?.StartedAt ?? DateTime.Now,

                    answers = g.Select(x => new {
                        question = x.Question?.QuestionText,
                        answer = x.OptionId != null ? x.Option?.OptionText : x.TextAnswer
                    }).ToList()
                }).ToList();

            return Ok(usersData);
        }
    }
}