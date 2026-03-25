using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Models;
using SurveyPortal.API.Repositories.Interfaces;
using SurveyPortal.API.Services;
using System.Security.Claims;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
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

            // 3. Oluşan bu oturumun ID'sini kullanarak, kullanıcının gönderdiği tüm cevapları kaydediyoruz
            await _answerService.SaveAnswersAsync(surveyResponse.Id, answersDto);

            return Ok(new { Message = "Cevaplarınız başarıyla kaydedildi. Katılımınız için teşekkürler!" });
        }
    }
}