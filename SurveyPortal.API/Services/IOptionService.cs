using SurveyPortal.API.DTOs;

namespace SurveyPortal.API.Services
{
    public interface IOptionService
    {
        Task<IEnumerable<OptionDto>> GetOptionsByQuestionIdAsync(int questionId);
        Task<OptionDto> CreateOptionAsync(OptionDto optionDto);
        Task DeleteOptionAsync(int id);
        Task UpdateOptionAsync(int id, OptionDto optionDto);
    }
}