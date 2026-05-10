using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.Data;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Models;
using SurveyPortal.API.Repositories.Interfaces;
using SurveyPortal.API.Services;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly IQuestionService _questionService;
        private readonly IGenericRepository<Question> _questionRepository; // Repository'i ekledik

        public QuestionController(IQuestionService questionService, IGenericRepository<Question> questionRepository)
        {
            _questionService = questionService;
            _questionRepository = questionRepository;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllQuestions()
        {
            var allQuestions = await _questionRepository.GetAllAsync();
            return Ok(allQuestions);
        }

        [HttpGet("survey/{surveyId}")]
        public async Task<IActionResult> GetBySurveyId(int surveyId)
        {
            var questions = await _questionService.GetQuestionsBySurveyIdAsync(surveyId);
            return Ok(questions);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(QuestionDto questionDto)
        {
            var newQuestion = await _questionService.CreateQuestionAsync(questionDto);
            return Ok(newQuestion);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestionById(int id, [FromServices] AppDbContext context)
        {
            var question = await context.Questions.FindAsync(id);

            if (question == null)
                return NotFound("Soru bulunamadı.");

            return Ok(new
            {
                id = question.Id,
                questionText = question.QuestionText,
                questionType = question.QuestionType,
                isRequired = question.IsRequired,
                surveyId = question.SurveyId
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _questionService.DeleteQuestionAsync(id);
            return Ok(new { Message = "Soru başarıyla silindi (Soft Delete uygulandı)." });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, QuestionDto questionDto)
        {
            await _questionService.UpdateQuestionAsync(id, questionDto);
            return Ok(new { Message = "Soru başarıyla güncellendi." });
        }
    }
}