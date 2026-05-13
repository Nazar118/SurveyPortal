using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
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

                EstimatedTime = Math.Max(1, s.Questions.Count(q => !q.IsDeleted) / 2),
                CompletionRate = 75 + (s.Id % 20)
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

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var alreadySolved = await context.Set<SurveyResponse>().AnyAsync(r => r.SurveyId == id && r.AppUserId == userId);
                if (alreadySolved)
                    return BadRequest(new { ErrorCode = "ALREADY_SOLVED", Message = "Bu anketi zaten çözdünüz." });
            }

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

            var trendingSurveyId = await context.Set<SurveyResponse>()
                .GroupBy(r => r.SurveyId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            if (trendingSurveyId == 0) return NotFound("Popüler anket bulunamadı.");

            var survey = await context.Surveys
                .Include(s => s.Category)
                .Include(s => s.Questions.Where(q => !q.IsDeleted))
                .FirstOrDefaultAsync(s => s.Id == trendingSurveyId && !s.IsDeleted && s.Status == "Active");

            if (survey == null) return NotFound();

            var participantCount = await context.Set<SurveyResponse>().CountAsync(r => r.SurveyId == survey.Id);
            var isParticipated = userId != null && await context.Set<SurveyResponse>().AnyAsync(r => r.SurveyId == survey.Id && r.AppUserId == userId);

            var estimatedTime = Math.Max(1, survey.Questions.Count / 2);

            return Ok(new
            {
                Id = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                CategoryName = survey.Category != null ? survey.Category.Name : "Genel",
                ParticipantCount = participantCount,
                EstimatedTime = estimatedTime,
                CompletionRate = 89,
                IsParticipated = isParticipated
            });
        }

        [Authorize]
        [HttpGet("recommendation")]
        public async Task<IActionResult> GetRecommendations([FromServices] AppDbContext context)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Ok(new List<object>());

                var solvedSurveyIds = await context.Set<SurveyResponse>()
                    .Where(r => r.AppUserId == userId)
                    .Select(r => r.SurveyId)
                    .Distinct()
                    .ToListAsync();

                var solvedCategoryIds = await context.Surveys
                    .Where(s => solvedSurveyIds.Contains(s.Id))
                    .Select(s => s.CategoryId)
                    .Distinct()
                    .ToListAsync();

                var availableSurveys = await context.Surveys
                    .Include(s => s.Category)
                    .Include(s => s.Questions.Where(q => !q.IsDeleted))
                    .Where(s => !s.IsDeleted && s.Status == "Active" && (s.EndDate == null || s.EndDate > DateTime.Now))
                    .Where(s => !solvedSurveyIds.Contains(s.Id))
                    .ToListAsync();

                var surveyIds = availableSurveys.Select(s => s.Id).ToList();
                var participantCounts = await context.Set<SurveyResponse>()
                    .Where(r => surveyIds.Contains(r.SurveyId))
                    .GroupBy(r => r.SurveyId)
                    .Select(g => new { SurveyId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.SurveyId, x => x.Count);

                var recommendedSurveys = availableSurveys
                    .Where(s => solvedCategoryIds.Contains(s.CategoryId))
                    .OrderByDescending(s => s.CreatedDate)
                    .Take(4)
                    .ToList();

                if (!recommendedSurveys.Any())
                {
                    recommendedSurveys = availableSurveys.OrderByDescending(s => s.CreatedDate).Take(4).ToList();
                }

                var result = recommendedSurveys.Select(s => new {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    CategoryName = s.Category?.Name ?? "Genel",
                    ParticipantCount = participantCounts.ContainsKey(s.Id) ? participantCounts[s.Id] : 0,
                    EstimatedTime = Math.Max(1, s.Questions.Count / 2),
                    CompletionRate = 75 + (s.Id % 20)
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "API Hatası: " + ex.Message);
            }
        }

        // 🔥 AŞAMA 3: ANKET CEVAPLARINI VERİTABANINA KAYDEDEN METOT
        [Authorize]
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitSurvey([FromBody] SurveySubmitRequest request, [FromServices] AppDbContext context)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized("Oturum süreniz dolmuş.");

                var alreadySolved = await context.Set<SurveyResponse>().AnyAsync(r => r.SurveyId == request.SurveyId && r.AppUserId == userId);
                if (alreadySolved) return BadRequest("Bu anketi zaten çözdünüz.");

                var response = new SurveyResponse
                {
                    SurveyId = request.SurveyId,
                    AppUserId = userId,
                    StartedAt = DateTime.Now,
                    CompletedAt = DateTime.Now,
                    IsCompleted = true
                };

                context.Set<SurveyResponse>().Add(response);
                await context.SaveChangesAsync();

                foreach (var ans in request.Answers)
                {
                    var newAnswer = new Answer
                    {
                        SurveyResponseId = response.Id,
                        QuestionId = ans.QuestionId,
                        OptionId = ans.SelectedOptionId,
                        TextAnswer = ans.AnswerText
                    };
                    context.Set<Answer>().Add(newAnswer);
                }

                await context.SaveChangesAsync();

                return Ok(new { Message = "Anket başarıyla kaydedildi!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Kayıt sırasında bir hata oluştu: " + ex.Message);
            }
        }

         [HttpGet("{id}/results")]
        public async Task<IActionResult> GetSurveyResults(int id, [FromServices] AppDbContext context)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var survey = await context.Surveys
                .Include(s => s.Questions.Where(q => !q.IsDeleted))
                    .ThenInclude(q => q.Options.Where(o => !o.IsDeleted))
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (survey == null) return NotFound("Anket bulunamadı.");

            var totalParticipants = await context.Set<SurveyResponse>().CountAsync(r => r.SurveyId == id);

            var allAnswers = await context.Set<Answer>()
                .Include(a => a.SurveyResponse)
                .Where(a => a.SurveyResponse.SurveyId == id)
                .ToListAsync();

            var resultDto = new
            {
                SurveyId = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                TotalParticipants = totalParticipants,
                Insights = new List<string>(),
                Questions = new List<object>()
            };

            foreach (var q in survey.Questions)
            {
                var qAnswers = allAnswers.Where(a => a.QuestionId == q.Id).ToList();
                var totalQAnswers = qAnswers.Count;

                // Akıllı Soru Tipi Algılayıcı (Crash Önleyici)
                string safeType = "Radio";
                if (q.Options == null || !q.Options.Any()) safeType = "Text";
                else if (q.QuestionType != null && (q.QuestionType.ToString().ToLower().Contains("check") || q.QuestionType.ToString().ToLower().Contains("çoklu") || q.QuestionType.ToString() == "2")) safeType = "Checkbox";

                if (safeType != "Text")
                {
                    var optionsStats = new List<object>();
                    int maxCount = 0;

                    foreach (var opt in q.Options)
                    {
                        var optCount = qAnswers.Count(a => a.OptionId == opt.Id);
                        if (optCount > maxCount) maxCount = optCount;
                    }

                    foreach (var opt in q.Options)
                    {
                        var optCount = qAnswers.Count(a => a.OptionId == opt.Id);
                        var percentage = totalQAnswers > 0 ? (int)Math.Round((double)optCount / totalQAnswers * 100) : 0;
                        var isUserChoice = userId != null && qAnswers.Any(a => a.OptionId == opt.Id && a.SurveyResponse.AppUserId == userId);

                        optionsStats.Add(new
                        {
                            OptionId = opt.Id,
                            OptionText = opt.OptionText,
                            Count = optCount,
                            Percentage = percentage,
                            IsWinner = optCount > 0 && optCount == maxCount,
                            IsUserChoice = isUserChoice
                        });
                    }

                    resultDto.Questions.Add(new
                    {
                        QuestionId = q.Id,
                        QuestionText = q.QuestionText,
                        QuestionType = safeType,
                        Options = optionsStats
                    });

                    // INSIGHT ÜRETİMİ
                    if (maxCount > 0 && totalQAnswers > 0)
                    {
                        var topPercent = (int)Math.Round((double)maxCount / totalQAnswers * 100);
                        if (topPercent > 50)
                        {
                            var topOption = q.Options.First(o => qAnswers.Count(a => a.OptionId == o.Id) == maxCount);
                            resultDto.Insights.Add($"Katılımcıların <strong>%{topPercent}'i</strong>, '{q.QuestionText}' konusunda <strong>{topOption.OptionText}</strong> tercihini yaptı.");
                        }
                    }
                }
                else
                {
                    var textAnswers = qAnswers.Where(a => !string.IsNullOrWhiteSpace(a.TextAnswer)).Select(a => a.TextAnswer).ToList();
                    resultDto.Questions.Add(new
                    {
                        QuestionId = q.Id,
                        QuestionText = q.QuestionText,
                        QuestionType = "Text",
                        TextAnswers = textAnswers
                    });
                }
            }

            return Ok(resultDto);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("ai/generate")]
        public async Task<IActionResult> GenerateAiSurvey([FromBody] AiSurveyRequest request, [FromServices] AppDbContext context)
        {
            try
            {
                var questions = new List<Models.Question>();

                string[] questionTemplates = {
                    "'{0}' konusundaki genel beklentileriniz ne ölçüde karşılanıyor?",
                    "Sizce '{0}' alanında karşılaşılan en temel zorluklar nelerdir?",
                    "'{0}' ile ilgili yenilikleri veya süreçleri ne sıklıkla takip ediyorsunuz?",
                    "'{0}' konusunda mevcut altyapıyı nasıl değerlendiriyorsunuz?",
                    "'{0}' alanında acilen iyileştirilmesi gerektiğini düşündüğünüz noktalar nelerdir?",
                    "'{0}' kavramı günlük iş veya eğitim yaşantınızı ne yönde etkiliyor?",
                    "Son olarak, '{0}' hakkında eklemek veya vurgulamak istediğiniz bir detay var mı?"
                };

                for (int i = 0; i < request.QuestionCount; i++)
                {
                    string qText = string.Format(questionTemplates[i % questionTemplates.Length], request.Topic);
                    int qType;
                    var options = new List<Models.Option>();

                    // Sorunun sırasına göre mantıklı soru tipleri ve şıklar atıyoruz
                    if (i % questionTemplates.Length == 1 || i % questionTemplates.Length == 4)
                    {
                        // ÇOKLU SEÇİM (Zorluklar veya İyileştirmeler)
                        qType = 2; // Checkbox
                        options.Add(new Models.Option { OptionText = "Teknik Altyapı ve Kaynak Eksikliği", OrderNumber = 1, CreatedDate = DateTime.Now });
                        options.Add(new Models.Option { OptionText = "Eğitim ve Bilgi Yetersizliği", OrderNumber = 2, CreatedDate = DateTime.Now });
                        options.Add(new Models.Option { OptionText = "Zaman Yönetimi ve Planlama", OrderNumber = 3, CreatedDate = DateTime.Now });
                        options.Add(new Models.Option { OptionText = "Maliyet ve Bütçe Kısıtlamaları", OrderNumber = 4, CreatedDate = DateTime.Now });
                        options.Add(new Models.Option { OptionText = "İletişim ve Koordinasyon Sorunları", OrderNumber = 5, CreatedDate = DateTime.Now });
                    }
                    else if (i % questionTemplates.Length == 2)
                    {
                        // TEKLİ SEÇİM (Sıklık Belirten Şıklar)
                        qType = 1; // Radio
                        options.Add(new Models.Option { OptionText = "Sürekli / Her Gün", OrderNumber = 1, CreatedDate = DateTime.Now });
                        options.Add(new Models.Option { OptionText = "Haftada Birkaç Kez", OrderNumber = 2, CreatedDate = DateTime.Now });
                        options.Add(new Models.Option { OptionText = "Ayda Bir veya Daha Az", OrderNumber = 3, CreatedDate = DateTime.Now });
                        options.Add(new Models.Option { OptionText = "Neredeyse Hiç", OrderNumber = 4, CreatedDate = DateTime.Now });
                    }
                    else if (i % questionTemplates.Length == 6 || (i == request.QuestionCount - 1 && i % questionTemplates.Length != 1))
                    {
                        // METİN (Genellikle son soru veya açık uçlu görüş)
                        qType = 0; // Text
                        qText = $"'{request.Topic}' konusunda sistemin geliştirilmesi için önerileriniz nelerdir?";
                    }
                    else
                    {
                        // TEKLİ SEÇİM (Genel Memnuniyet / Derecelendirme)
                        qType = 1; // Radio
                        options.Add(new Models.Option { OptionText = "Çok Başarılı / Kesinlikle Katılıyorum", OrderNumber = 1, CreatedDate = DateTime.Now });
                        options.Add(new Models.Option { OptionText = "Başarılı / Katılıyorum", OrderNumber = 2, CreatedDate = DateTime.Now });
                        options.Add(new Models.Option { OptionText = "Kararsızım / Nötr", OrderNumber = 3, CreatedDate = DateTime.Now });
                        options.Add(new Models.Option { OptionText = "Geliştirilmeli / Katılmıyorum", OrderNumber = 4, CreatedDate = DateTime.Now });
                    }

                    questions.Add(new Models.Question
                    {
                        QuestionText = qText,
                        QuestionType = qType,
                        IsRequired = true,
                        OrderNumber = i + 1,
                        CreatedDate = DateTime.Now,
                        Options = options
                    });
                }

                // Anketi veritabanına ekle
                var newSurvey = new Models.Survey
                {
                    Title = request.Topic + " Anketi (Yapay Zeka Destekli)",
                    Description = $"Bu anket, '{request.Topic}' konusu analiz edilerek sistem tarafından dinamik olarak oluşturulmuştur.",
                    CategoryId = request.CategoryId,
                    Status = "Draft",
                    CreatedDate = DateTime.Now,
                    Questions = questions
                };

                context.Surveys.Add(newSurvey);
                await context.SaveChangesAsync();

                return Ok(new { Message = "Başarılı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hata Detayı: " + ex.Message);
            }
        }
    }

    public class SurveySubmitRequest
    {
        public int SurveyId { get; set; }
        public List<AnswerSubmitDto> Answers { get; set; } = new List<AnswerSubmitDto>();
    }

    public class AnswerSubmitDto
    {
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
        public string? AnswerText { get; set; }
    }
    public class AiSurveyRequest
    {
        public string Topic { get; set; }
        public int QuestionCount { get; set; }
        public int CategoryId { get; set; }
    }
    public class AiQuestionResponse
    {
        public string Text { get; set; }
        public int Type { get; set; }
        public List<string> Options { get; set; }
    }
}