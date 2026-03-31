using SurveyPortal.API.DTOs;

namespace SurveyPortal.API.Services
{
    public interface IQuestionService
    {
        // Bir ankete ait tüm soruları getir
        Task<IEnumerable<QuestionDto>> GetQuestionsBySurveyIdAsync(int surveyId);
        Task<QuestionDto> CreateQuestionAsync(QuestionDto questionDto);
        Task DeleteQuestionAsync(int id);
        Task UpdateQuestionAsync(int id, QuestionDto questionDto);
    }
}