using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyPortal.API.Data;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Models;
using SurveyPortal.API.Services;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyController : ControllerBase
    {
        private readonly ISurveyService _surveyService;

        public SurveyController(ISurveyService surveyService)
        {
            _surveyService = surveyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var surveys = await _surveyService.GetAllSurveysAsync();
            return Ok(surveys);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var survey = await _surveyService.GetSurveyByIdAsync(id);
            if (survey == null)
                return NotFound("Aradığınız anket bulunamadı veya silinmiş.");

            return Ok(survey);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(SurveyDto surveyDto)
        {
            var newSurvey = await _surveyService.CreateSurveyAsync(surveyDto);
            return Ok(newSurvey);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, SurveyDto surveyDto)
        {
            await _surveyService.UpdateSurveyAsync(id, surveyDto);
            return Ok(new { Message = "Anket başarıyla güncellendi." });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _surveyService.DeleteSurveyAsync(id);
            return Ok(new { Message = "Anket başarıyla silindi." });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/clone")]
        public async Task<IActionResult> Clone(int id, [FromServices] AppDbContext context)
        {
            var original = await context.Surveys
                .Include(s => s.Questions.Where(q => !q.IsDeleted))
                    .ThenInclude(q => q.Options.Where(o => !o.IsDeleted))
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (original == null) return NotFound("Anket bulunamadı.");

            var clone = new Models.Survey
            {
                Title = original.Title + " (Kopya)",
                Description = original.Description,
                CategoryId = original.CategoryId,
                Status = "Draft",
                CreatedDate = DateTime.Now,
                IsAnonymous = original.IsAnonymous,
                Questions = original.Questions.Select(q => new Models.Question
                {
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    IsRequired = q.IsRequired,
                    OrderNumber = q.OrderNumber,
                    CreatedDate = DateTime.Now,
                    Options = q.Options.Select(o => new Models.Option
                    {
                        OptionText = o.OptionText,
                        OrderNumber = o.OrderNumber,
                        CreatedDate = DateTime.Now
                    }).ToList()
                }).ToList()
            };

            context.Surveys.Add(clone);
            await context.SaveChangesAsync();

            return Ok(new { Message = "Anket başarıyla kopyalandı!" });
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveSurveys(
            [FromServices] AppDbContext context,
            [FromQuery] int? categoryId,
            [FromQuery] string? search,
            [FromQuery] string? sortBy)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var query = context.Surveys
                .Include(s => s.Category)
                .Where(s => !s.IsDeleted && s.Status == "Active" && (s.EndDate == null || s.EndDate > DateTime.Now))
                .AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(s => s.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(s => s.Title.ToLower().Contains(lowerSearch) || s.Description.ToLower().Contains(lowerSearch));
            }

            var projectedQuery = query.Select(s => new {
                s.Id,
                s.Title,
                s.Description,
                s.CreatedDate,
                s.EndDate,
                CategoryName = s.Category != null ? s.Category.Name : "Genel",
                ParticipantCount = context.Set<SurveyResponse>().Count(r => r.SurveyId == s.Id),

                IsParticipated = userId != null && context.Set<SurveyResponse>().Any(r => r.SurveyId == s.Id && r.AppUserId == userId)
            });

            if (sortBy == "popular")
                projectedQuery = projectedQuery.OrderByDescending(s => s.ParticipantCount);
            else if (sortBy == "ending")
                projectedQuery = projectedQuery.OrderBy(s => s.EndDate ?? DateTime.MaxValue);
            else
                projectedQuery = projectedQuery.OrderByDescending(s => s.CreatedDate);

            var result = await projectedQuery.ToListAsync();
            return Ok(result);
        }
        [HttpGet("{id}/detail")]
        public async Task<IActionResult> GetSurveyDetailForSolve(int id, [FromServices] AppDbContext context)
        {
            var survey = await context.Surveys
                .Include(s => s.Questions.Where(q => !q.IsDeleted).OrderBy(q => q.OrderNumber))
                    .ThenInclude(q => q.Options.Where(o => !o.IsDeleted).OrderBy(o => o.OrderNumber))
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.Status == "Active");

            if (survey == null)
                return NotFound("Anket bulunamadı veya artık aktif değil.");

            var result = new
            {
                survey.Id,
                survey.Title,
                survey.Description,
                Questions = survey.Questions.Select(q => new
                {
                    q.Id,
                    q.QuestionText,
                    q.QuestionType, 
                    q.IsRequired,
                    Options = q.Options.Select(o => new
                    {
                        o.Id,
                        o.OptionText
                    }).ToList()
                }).ToList()
            };

            return Ok(result);
        }
    } 
}