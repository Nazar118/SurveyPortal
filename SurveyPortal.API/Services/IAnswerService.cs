using SurveyPortal.API.DTOs;

namespace SurveyPortal.API.Services
{
    public interface IAnswerService
    {
        // Kullanıcının bir ankete verdiği tüm cevapları liste halinde kaydet (Toplu kayıt)
        Task SaveAnswersAsync(int surveyResponseId, IEnumerable<AnswerDto> answersDto);
    }
}