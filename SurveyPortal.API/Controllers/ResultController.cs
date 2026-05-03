using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.Data;
using SurveyPortal.API.Services;
using Microsoft.EntityFrameworkCore;
using SurveyPortal.API.DTOs;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ResultController : ControllerBase
    {
        private readonly IResultService _resultService;
        private readonly AppDbContext _context;

        public ResultController(IResultService resultService, AppDbContext context)
        {
            _resultService = resultService;
            _context = context;
        }

        [HttpGet("{surveyId}")]
        public async Task<IActionResult> GetResults(int surveyId)
        {
            var results = await _resultService.GetSurveyResultsAsync(surveyId);

            if (results == null)
                return NotFound("Anket bulunamadı veya sonuç hesaplanamıyor.");

            var questionsInfo = await _context.Questions
                .Where(q => q.SurveyId == surveyId)
                .ToDictionaryAsync(q => q.Id, q => q.QuestionType);

            foreach (var q in results.Questions)
            {
                if (questionsInfo.ContainsKey(q.QuestionId))
                {
                }
            }

            var richResults = new
            {
                results.SurveyId,
                results.SurveyTitle,
                results.TotalParticipants,
                Questions = results.Questions.Select(q => new
                {
                    q.QuestionId,
                    q.QuestionText,
                    q.TotalAnswers,
                    QuestionType = questionsInfo.ContainsKey(q.QuestionId) ? questionsInfo[q.QuestionId] : 0, // 0: Text, 1: Single, 2: Multi
                    Options = q.Options
                })
            };

            return Ok(richResults);
        }

        [HttpGet("users/{surveyId}")]
        public async Task<IActionResult> GetSurveyUsers(int surveyId)
        {
            var answers = await _context.Answers
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