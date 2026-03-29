using SurveyPortal.API.DTOs;

namespace SurveyPortal.API.Services
{
    public interface IOptionService
    {
        Task<IEnumerable<OptionDto>> GetOptionsByQuestionIdAsync(int questionId);
        Task<OptionDto> CreateOptionAsync(OptionDto optionDto);
        Task DeleteOptionAsync(int id);
    }
}