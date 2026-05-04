using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyPortal.API.Data; // 🔥 İŞTE EKSİK OLAN SATIR BURADA!
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Models;
using SurveyPortal.API.Repositories.Interfaces;
using SurveyPortal.API.Services;
using System.Security.Claims;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Sadece giriş yapanlar anket çözebilir
    public class AnswerController : ControllerBase
    {
        private readonly IAnswerService _answerService;
        private readonly IGenericRepository<SurveyResponse> _surveyResponseRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AnswerController(IAnswerService answerService, IGenericRepository<SurveyResponse> surveyResponseRepository, IUnitOfWork unitOfWork)
        {
            _answerService = answerService;
            _surveyResponseRepository = surveyResponseRepository;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllResponses()
        {
            var allResponses = await _surveyResponseRepository.GetAllAsync();
            return Ok(allResponses);
        }

        [HttpPost("submit/{surveyId}")]
        public async Task<IActionResult> SubmitAnswers(int surveyId, [FromBody] IEnumerable<AnswerDto> answersDto)
        {
            // 1. JWT Token içerisinden, giriş yapan kullanıcının ID'sini yakalıyoruz
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Geçersiz kullanıcı kimliği.");

            var hasAnswered = await _surveyResponseRepository
                .Where(r => r.SurveyId == surveyId && r.AppUserId == userId)
                .AnyAsync();

            if (hasAnswered)
            {
                return BadRequest(new { Message = "Bu anketi daha önce çözdünüz! Bir ankete sadece bir kez katılabilirsiniz." });
            }

            var surveyResponse = new SurveyResponse
            {
                SurveyId = surveyId,
                AppUserId = userId,
                StartedAt = DateTime.Now,
                IsCompleted = true,
                CreatedDate = DateTime.Now
            };

            await _surveyResponseRepository.AddAsync(surveyResponse);
            await _unitOfWork.CommitAsync();

            await _answerService.SaveAnswersAsync(surveyResponse.Id, answersDto);

            return Ok(new { Message = "Cevaplarınız başarıyla kaydedildi. Katılımınız için teşekkürler!" });
        }

        // 🔥 5. ADIM: KULLANICIYA ÖZEL GELİŞMİŞ SONUÇ EKRANI (YÜZDELİKLER VE KİŞİSEL SEÇİMLER)
        [HttpGet("result/{surveyId}")]
        public async Task<IActionResult> GetSurveyResultsForUser(int surveyId, [FromServices] AppDbContext context)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Anketi ve sorularını çek
            var survey = await context.Surveys
                .Include(s => s.Questions.Where(q => !q.IsDeleted))
                    .ThenInclude(q => q.Options.Where(o => !o.IsDeleted))
                .FirstOrDefaultAsync(s => s.Id == surveyId && !s.IsDeleted);

            if (survey == null) return NotFound("Anket bulunamadı.");

            // Toplam katılımcı sayısı
            var totalParticipants = await context.Set<SurveyResponse>().CountAsync(r => r.SurveyId == surveyId);

            // Giriş yapan kullanıcının bu anketteki KENDİ cevapları
            var userResponses = userId != null
                ? await context.Set<Answer>().Include(a => a.SurveyResponse)
                    .Where(a => a.SurveyResponse.SurveyId == surveyId && a.SurveyResponse.AppUserId == userId)
                    .ToListAsync()
                : new List<Answer>();

            // Ankete verilen TÜM cevaplar (Yüzde hesabı için)
            var allAnswers = await context.Set<Answer>().Include(a => a.SurveyResponse)
                .Where(a => a.SurveyResponse.SurveyId == surveyId).ToListAsync();

            var result = new
            {
                SurveyTitle = survey.Title,
                TotalParticipants = totalParticipants,
                Questions = survey.Questions.OrderBy(q => q.OrderNumber).Select(q => new
                {
                    q.Id,
                    q.QuestionText,
                    q.QuestionType,
                    // Metin soruları için: Tüm cevapları ve kullanıcının kendi cevabını gönder
                    TextAnswers = q.QuestionType == 0
                        ? allAnswers.Where(a => a.QuestionId == q.Id && !string.IsNullOrWhiteSpace(a.TextAnswer)).Select(a => a.TextAnswer).ToList()
                        : null,
                    UserTextAnswer = q.QuestionType == 0
                        ? userResponses.FirstOrDefault(a => a.QuestionId == q.Id)?.TextAnswer
                        : null,

                    // Seçmeli sorular için: Oy sayıları, Yüzdelik dilimler ve kullanıcının seçimi
                    Options = q.QuestionType != 0 ? q.Options.Select(o => {
                        var voteCount = allAnswers.Count(a => a.QuestionId == q.Id && a.OptionId == o.Id);
                        var percentage = totalParticipants == 0 ? 0 : Math.Round((double)voteCount / totalParticipants * 100);
                        return new
                        {
                            o.Id,
                            o.OptionText,
                            VoteCount = voteCount,
                            Percentage = percentage,
                            IsUserChoice = userResponses.Any(a => a.QuestionId == q.Id && a.OptionId == o.Id)
                        };
                    }).OrderByDescending(o => o.VoteCount).ToList() : null // En çok oy alan en üstte gelsin
                })
            };

            return Ok(result);
        }
    }
}