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
            var activeSurveys = surveys.Where(s => s.IsDeleted == false).ToList();

            bool isUpdated = false;
            foreach (var survey in activeSurveys)
            {
                if (survey.EndDate.HasValue && survey.EndDate < DateTime.Now && survey.Status != "Closed")
                {
                    survey.Status = "Closed";
                    _repository.Update(survey);
                    isUpdated = true;
                }
            }
            if (isUpdated) await _unitOfWork.CommitAsync();

            return _mapper.Map<IEnumerable<SurveyDto>>(activeSurveys);
        }

        public async Task<SurveyDto?> GetSurveyByIdAsync(int id)
        {
            var survey = await _repository.GetByIdAsync(id);
            if (survey == null || survey.IsDeleted) return null;

            if (survey.EndDate.HasValue && survey.EndDate < DateTime.Now && survey.Status != "Closed")
            {
                survey.Status = "Closed";
                _repository.Update(survey);
                await _unitOfWork.CommitAsync();
            }

            return _mapper.Map<SurveyDto>(survey);
        }

        public async Task<SurveyDto> CreateSurveyAsync(SurveyDto surveyDto)
        {
            var survey = _mapper.Map<Survey>(surveyDto);
            survey.CreatedDate = DateTime.Now;

            // Yönerge 1.2: Yeni anket her zaman "Draft" (Taslak) başlar
            survey.Status = "Draft";

            await _repository.AddAsync(survey);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<SurveyDto>(survey);
        }

        public async Task UpdateSurveyAsync(int id, SurveyDto surveyDto)
        {
            var survey = await _repository.GetByIdAsync(id);

            if (survey != null && !survey.IsDeleted)
            {
                survey.Title = surveyDto.Title;
                survey.Description = surveyDto.Description;
                survey.CategoryId = surveyDto.CategoryId;
                survey.EndDate = surveyDto.EndDate;
                survey.IsAnonymous = surveyDto.IsAnonymous;

                survey.Status = surveyDto.Status;

                if (survey.EndDate.HasValue && survey.EndDate < DateTime.Now)
                    survey.Status = "Closed";

                survey.UpdatedDate = DateTime.Now;

                _repository.Update(survey);
                await _unitOfWork.CommitAsync();
            }
        }

        public async Task DeleteSurveyAsync(int id)
        {
            var survey = await _repository.GetByIdAsync(id);
            if (survey != null && !survey.IsDeleted)
            {
                survey.IsDeleted = true;
                survey.Status = "Closed"; // Silinen anket kapanır
                survey.UpdatedDate = DateTime.Now;

                _repository.Update(survey);
                await _unitOfWork.CommitAsync();
            }
        }
    }
}