using SurveyPortal.API.DTOs;

namespace SurveyPortal.API.Services
{
    public interface ISurveyService
    {
        Task<IEnumerable<SurveyDto>> GetAllSurveysAsync();
        Task<SurveyDto?> GetSurveyByIdAsync(int id);
        Task<SurveyDto> CreateSurveyAsync(SurveyDto surveyDto);
        Task UpdateSurveyAsync(int id, SurveyDto surveyDto);
        Task DeleteSurveyAsync(int id);
    }
}