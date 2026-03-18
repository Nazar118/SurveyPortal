using AutoMapper;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Models;
using SurveyPortal.API.Repositories.Interfaces;

namespace SurveyPortal.API.Services
{
    public class SurveyService : ISurveyService
    {
        private readonly IGenericRepository<Survey> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SurveyService(IGenericRepository<Survey> repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SurveyDto>> GetAllSurveysAsync()
        {
            var surveys = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<SurveyDto>>(surveys);
        }

        public async Task<SurveyDto?> GetSurveyByIdAsync(int id)
        {
            var survey = await _repository.GetByIdAsync(id);
            if (survey == null) return null;
            return _mapper.Map<SurveyDto>(survey);
        }

        public async Task<SurveyDto> CreateSurveyAsync(SurveyDto surveyDto)
        {
            var survey = _mapper.Map<Survey>(surveyDto);

            survey.CreatedDate = DateTime.Now;

            await _repository.AddAsync(survey);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<SurveyDto>(survey);
        }

        public async Task UpdateSurveyAsync(int id, SurveyDto surveyDto)
        {
            var survey = await _repository.GetByIdAsync(id);
            if (survey != null)
            {
                // Mevcut anketin bilgilerini yeni gelen DTO ile değiştiriyoruz
                survey.Title = surveyDto.Title;
                survey.Description = surveyDto.Description;
                survey.IsActive = surveyDto.IsActive;
                survey.CategoryId = surveyDto.CategoryId;
                survey.UpdatedDate = DateTime.Now;

                _repository.Update(survey);
                await _unitOfWork.CommitAsync();
            }
        }

        public async Task DeleteSurveyAsync(int id)
        {
            var survey = await _repository.GetByIdAsync(id);
            if (survey != null)
            {
                _repository.Remove(survey);
                await _unitOfWork.CommitAsync();
            }
        }
    }
}