using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Models;
using SurveyPortal.API.Repositories.Interfaces;

namespace SurveyPortal.API.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IGenericRepository<Question> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public QuestionService(IGenericRepository<Question> repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<QuestionDto>> GetQuestionsBySurveyIdAsync(int surveyId)
        {
            // Belirli bir ankete ait soruları filtrele ve sırala
            var questions = await _repository.Where(q => q.SurveyId == surveyId)
                                             .OrderBy(q => q.OrderNumber)
                                             .ToListAsync();

            return _mapper.Map<IEnumerable<QuestionDto>>(questions);
        }

        public async Task<QuestionDto> CreateQuestionAsync(QuestionDto questionDto)
        {
            var question = _mapper.Map<Question>(questionDto);
            question.CreatedDate = DateTime.Now;

            await _repository.AddAsync(question);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<QuestionDto>(question);
        }

        public async Task DeleteQuestionAsync(int id)
        {
            var question = await _repository.GetByIdAsync(id);
            if (question != null)
            {
                _repository.Remove(question);
                await _unitOfWork.CommitAsync();
            }
        }
    }
}