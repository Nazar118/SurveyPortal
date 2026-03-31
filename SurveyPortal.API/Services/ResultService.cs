using Microsoft.EntityFrameworkCore;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Models;
using SurveyPortal.API.Repositories.Interfaces;

namespace SurveyPortal.API.Services
{
    public class ResultService : IResultService
    {
        private readonly IGenericRepository<Survey> _surveyRepo;
        private readonly IGenericRepository<SurveyResponse> _responseRepo;
        private readonly IGenericRepository<Question> _questionRepo;
        private readonly IGenericRepository<Option> _optionRepo;
        private readonly IGenericRepository<Answer> _answerRepo;

        public ResultService(
            IGenericRepository<Survey> surveyRepo,
            IGenericRepository<SurveyResponse> responseRepo,
            IGenericRepository<Question> questionRepo,
            IGenericRepository<Option> optionRepo,
            IGenericRepository<Answer> answerRepo)
        {
            _surveyRepo = surveyRepo;
            _responseRepo = responseRepo;
            _questionRepo = questionRepo;
            _optionRepo = optionRepo;
            _answerRepo = answerRepo;
        }

        public async Task<SurveyResultDto?> GetSurveyResultsAsync(int surveyId)
        {
            var survey = await _surveyRepo.GetByIdAsync(surveyId);
            if (survey == null || survey.IsDeleted) return null;

            var totalParticipants = await _responseRepo.Where(r => r.SurveyId == surveyId).CountAsync();

            var resultDto = new SurveyResultDto
            {
                SurveyId = survey.Id,
                SurveyTitle = survey.Title,
                TotalParticipants = totalParticipants
            };

            var questions = await _questionRepo.Where(q => q.SurveyId == surveyId && !q.IsDeleted).ToListAsync();

            foreach (var q in questions)
            {
                var qDto = new QuestionResultDto { QuestionId = q.Id, QuestionText = q.QuestionText };

                int totalAnswersForQuestion = await _answerRepo.Where(a => a.QuestionId == q.Id).CountAsync();
                qDto.TotalAnswers = totalAnswersForQuestion;

                var options = await _optionRepo.Where(o => o.QuestionId == q.Id && !o.IsDeleted).ToListAsync();

                foreach (var opt in options)
                {
                    int voteCount = await _answerRepo.Where(a => a.OptionId == opt.Id).CountAsync();

                    double percentage = totalAnswersForQuestion == 0 ? 0 : Math.Round(((double)voteCount / totalAnswersForQuestion) * 100, 2);

                    qDto.Options.Add(new OptionResultDto
                    {
                        OptionId = opt.Id,
                        OptionText = opt.OptionText,
                        VoteCount = voteCount,
                        Percentage = percentage
                    });
                }

                resultDto.Questions.Add(qDto);
            }

            return resultDto;
        }
    }
}