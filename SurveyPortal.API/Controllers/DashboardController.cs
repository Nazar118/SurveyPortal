using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.Data;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Models;
using SurveyPortal.API.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IGenericRepository<Survey> _surveyRepo;
        private readonly IGenericRepository<Question> _questionRepo;
        private readonly IGenericRepository<Answer> _answerRepo;
        private readonly IGenericRepository<SurveyResponse> _responseRepo;

        public DashboardController(IGenericRepository<Survey> surveyRepo,
                                   IGenericRepository<Question> questionRepo,
                                   IGenericRepository<Answer> answerRepo,
                                   IGenericRepository<SurveyResponse> responseRepo)
        {
            _surveyRepo = surveyRepo;
            _questionRepo = questionRepo;
            _answerRepo = answerRepo;
            _responseRepo = responseRepo;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var surveys = await _surveyRepo.GetAllAsync();
            var questions = await _questionRepo.GetAllAsync();
            var answers = await _answerRepo.GetAllAsync();
            var responses = await _responseRepo.GetAllAsync(); 

            var latestSurveys = surveys
                .Where(s => s.IsDeleted == false)
                .OrderByDescending(s => s.CreatedDate)
                .Take(5)
                .Select(s => new LatestSurveyDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    CreatedDate = s.CreatedDate,
                    EndDate = s.EndDate,
                    Status = s.Status
                }).ToList();

            var popularSurveyId = responses
                .GroupBy(r => r.SurveyId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            var popularSurveyTitle = surveys.FirstOrDefault(s => s.Id == popularSurveyId)?.Title ?? "Henüz katılım yok";

            var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-i)).Reverse().ToList();
            var activityList = new List<DailyActivityDto>();

            foreach (var day in last7Days)
            {
                var count = responses.Count(r => r.StartedAt.Date == day.Date);
                activityList.Add(new DailyActivityDto
                {
                    Date = day.ToString("dd MMM"), 
                    ResponseCount = count
                });
            }

            // 4. Paketi Hazırla
            var stats = new DashboardStatsDto
            {
                TotalSurveys = surveys.Count(s => s.IsDeleted == false),
                TotalQuestions = questions.Count(),
                TotalResponses = answers.Count(),
                MostPopularSurveyTitle = popularSurveyTitle,
                ActivityLast7Days = activityList,
                LatestSurveys = latestSurveys
            };

            return Ok(stats);
        }
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromServices] AppDbContext context)
        {
            // 1. Temel İstatistikler
            var totalSurvey = await context.Surveys.CountAsync(s => !s.IsDeleted && s.Status == "Active");
            var totalCategory = await context.Categories.CountAsync();
            var todayParticipants = await context.Set<SurveyResponse>().CountAsync(r => r.StartedAt.Date == DateTime.Today);

            // 2. Haftanın/Günün En Popüler (Trending) Anketini Bul
            var trendingSurveyId = await context.Set<SurveyResponse>()
                .GroupBy(r => r.SurveyId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            var trendingSurvey = await context.Surveys
                .Where(s => s.Id == trendingSurveyId && !s.IsDeleted)
                .Select(s => new {
                    s.Id,
                    s.Title,
                    ParticipantCount = context.Set<SurveyResponse>().Count(r => r.SurveyId == s.Id)
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                TotalSurvey = totalSurvey,
                TotalCategory = totalCategory,
                TodayParticipants = todayParticipants,
                TrendingSurvey = trendingSurvey
            });
        }
    }
}