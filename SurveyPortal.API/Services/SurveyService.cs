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

            //  Sadece silinmemiş (Soft Delete yapılmamış) anketleri getir
            var activeSurveys = surveys.Where(s => s.IsDeleted == false);

            return _mapper.Map<IEnumerable<SurveyDto>>(activeSurveys);
        }

        public async Task<SurveyDto?> GetSurveyByIdAsync(int id)
        {
            var survey = await _repository.GetByIdAsync(id);

            //  Eğer anket yoksa veya silinmiş olarak işaretlendiyse null dön
            if (survey == null || survey.IsDeleted) return null;

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

            if (survey != null && !survey.IsDeleted)
            {
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
            if (survey != null && !survey.IsDeleted)
            {
                //  Veriyi kalıcı silmek (Remove) yerine gizliyoruz (Soft Delete)
                survey.IsDeleted = true;
                survey.IsActive = false; 
                survey.UpdatedDate = DateTime.Now;

                _repository.Update(survey); 
                await _unitOfWork.CommitAsync();
            }
        }
    }
}