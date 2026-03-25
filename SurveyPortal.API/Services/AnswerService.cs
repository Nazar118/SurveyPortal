using AutoMapper;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Models;
using SurveyPortal.API.Repositories.Interfaces;

namespace SurveyPortal.API.Services
{
    public class AnswerService : IAnswerService
    {
        private readonly IGenericRepository<Answer> _answerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AnswerService(IGenericRepository<Answer> answerRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _answerRepository = answerRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task SaveAnswersAsync(int surveyResponseId, IEnumerable<AnswerDto> answersDto)
        {
            var answers = _mapper.Map<IEnumerable<Answer>>(answersDto);

            foreach (var answer in answers)
            {
                answer.SurveyResponseId = surveyResponseId;
                answer.CreatedDate = DateTime.Now;
                await _answerRepository.AddAsync(answer);
            }

            await _unitOfWork.CommitAsync();
        }
    }
}