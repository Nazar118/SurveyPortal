using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Services;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OptionController : ControllerBase
    {
        private readonly IOptionService _optionService;

        public OptionController(IOptionService optionService)
        {
            _optionService = optionService;
        }

        [HttpGet("question/{questionId}")]
        public async Task<IActionResult> GetByQuestionId(int questionId)
        {
            var options = await _optionService.GetOptionsByQuestionIdAsync(questionId);
            return Ok(options);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(OptionDto optionDto)
        {
            var newOption = await _optionService.CreateOptionAsync(optionDto);
            return Ok(newOption);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _optionService.DeleteOptionAsync(id);
            return Ok(new { Message = "Seçenek başarıyla silindi (Soft Delete)." });
        }
    }
}