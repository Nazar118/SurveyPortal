using SurveyPortal.API.DTOs;

namespace SurveyPortal.API.Services
{
    public interface IResultService
    {
        Task<SurveyResultDto?> GetSurveyResultsAsync(int surveyId);
    }
}