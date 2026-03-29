using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
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
    }
}