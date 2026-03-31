using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.Services;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] 
    public class ResultController : ControllerBase
    {
        private readonly IResultService _resultService;

        public ResultController(IResultService resultService)
        {
            _resultService = resultService;
        }

        [HttpGet("{surveyId}")]
        public async Task<IActionResult> GetResults(int surveyId)
        {
            var results = await _resultService.GetSurveyResultsAsync(surveyId);

            if (results == null)
                return NotFound("Anket bulunamadı veya sonuç hesaplanamıyor.");

            return Ok(results);
        }
    }
}