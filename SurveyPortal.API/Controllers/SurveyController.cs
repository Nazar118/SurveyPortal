using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.DTOs;
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
                return NotFound("Aradığınız anket bulunamadı.");

            return Ok(survey);
        }

        [Authorize] 
        [HttpPost]
        public async Task<IActionResult> Create(SurveyDto surveyDto)
        {
            var newSurvey = await _surveyService.CreateSurveyAsync(surveyDto);
            return Ok(newSurvey);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, SurveyDto surveyDto)
        {
            await _surveyService.UpdateSurveyAsync(id, surveyDto);
            return Ok(new { Message = "Anket başarıyla güncellendi." });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _surveyService.DeleteSurveyAsync(id);
            return Ok(new { Message = "Anket başarıyla silindi." });
        }
    }
}