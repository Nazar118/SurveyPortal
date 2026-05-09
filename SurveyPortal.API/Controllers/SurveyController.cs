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
                IsParticipated = userId != null && context.Set<SurveyResponse>().Any(r => r.SurveyId == s.Id && r.AppUserId == userId),

                EstimatedTime = Math.Max(1, s.Questions.Count(q => !q.IsDeleted) / 2), // Her 2 soru 1 dk
                CompletionRate = 75 + (s.Id % 20) // 75 ile 95 arası dinamik ama sabit bir sayı üretir
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
        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingSurvey([FromServices] AppDbContext context)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // 1. En çok yanıt alan anketi bul
            var trendingSurveyId = await context.Set<SurveyResponse>()
                .GroupBy(r => r.SurveyId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            if (trendingSurveyId == 0) return NotFound("Popüler anket bulunamadı.");

            // 2. Anket detaylarını getir
            var survey = await context.Surveys
                .Include(s => s.Category)
                .Include(s => s.Questions.Where(q => !q.IsDeleted))
                .FirstOrDefaultAsync(s => s.Id == trendingSurveyId && !s.IsDeleted && s.Status == "Active");

            if (survey == null) return NotFound();

            // 3. İstatistikleri hesapla
            var participantCount = await context.Set<SurveyResponse>().CountAsync(r => r.SurveyId == survey.Id);
            var isParticipated = userId != null && await context.Set<SurveyResponse>().AnyAsync(r => r.SurveyId == survey.Id && r.AppUserId == userId);

            // Tahmini Süre: Ortalama her 2 soru 1 dakika sürer varsayımı
            var estimatedTime = Math.Max(1, survey.Questions.Count / 2);

            return Ok(new
            {
                Id = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                CategoryName = survey.Category != null ? survey.Category.Name : "Genel",
                ParticipantCount = participantCount,
                EstimatedTime = estimatedTime,
                CompletionRate = 89, // Şimdilik modern bir hava katması için yüksek bir "mock" değer gönderiyoruz
                IsParticipated = isParticipated
            });
        }
        [Authorize]
        [HttpGet("recommendation")]
        public async Task<IActionResult> GetRecommendations([FromServices] AppDbContext context)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Ok(new List<object>());

            // 1. Kullanıcının daha önce çözdüğü anketlerin ve kategorilerin ID'lerini al
            var solvedSurveyIds = await context.Set<SurveyResponse>()
                .Where(r => r.AppUserId == userId)
                .Select(r => r.SurveyId)
                .ToListAsync();

            var solvedCategoryIds = await context.Set<SurveyResponse>()
                .Where(r => r.AppUserId == userId)
                .Select(r => r.Survey.CategoryId)
                .Distinct()
                .ToListAsync();

            // 2. Aktif ve ÇÖZÜLMEMİŞ anketleri veritabanından GÜVENLİ bir şekilde çek
            var rawSurveys = await context.Surveys
                .Include(s => s.Category)
                .Include(s => s.Questions)
                .Where(s => !s.IsDeleted && s.Status == "Active" && (s.EndDate == null || s.EndDate > DateTime.Now))
                .Where(s => !solvedSurveyIds.Contains(s.Id))
                .ToListAsync();

            // 3. Algoritma: Çözdüğü kategorilere uyanları öner, yoksa yenileri öner (RAM'de çalışır, hata vermez)
            IEnumerable<Models.Survey> recommendedSurveys;
            if (solvedCategoryIds.Any())
            {
                recommendedSurveys = rawSurveys
                    .Where(s => solvedCategoryIds.Contains(s.CategoryId))
                    .OrderByDescending(s => s.CreatedDate)
                    .Take(4);
            }
            else
            {
                recommendedSurveys = rawSurveys
                    .OrderByDescending(s => s.CreatedDate)
                    .Take(4);
            }

            // 4. Verileri paketle
            var result = new List<object>();
            foreach (var s in recommendedSurveys)
            {
                var pCount = await context.Set<SurveyResponse>().CountAsync(r => r.SurveyId == s.Id);
                var qCount = s.Questions != null ? s.Questions.Count(q => !q.IsDeleted) : 0;
                var estimatedTime = (qCount / 2) < 1 ? 1 : (qCount / 2);

                result.Add(new
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    CreatedDate = s.CreatedDate,
                    EndDate = s.EndDate,
                    CategoryName = s.Category?.Name ?? "Genel",
                    ParticipantCount = pCount,
                    IsParticipated = false,
                    EstimatedTime = estimatedTime,
                    CompletionRate = 75 + (s.Id % 20)
                });
            }

            return Ok(result);
        }
    } 
}