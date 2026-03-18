using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Services;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly IQuestionService _questionService;

        public QuestionController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet("survey/{surveyId}")]
        public async Task<IActionResult> GetBySurveyId(int surveyId)
        {
            var questions = await _questionService.GetQuestionsBySurveyIdAsync(surveyId);
            return Ok(questions);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(QuestionDto questionDto)
        {
            var newQuestion = await _questionService.CreateQuestionAsync(questionDto);
            return Ok(newQuestion);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _questionService.DeleteQuestionAsync(id);
            return Ok(new { Message = "Soru başarıyla silindi." });
        }
    }
}