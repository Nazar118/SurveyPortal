using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Models;
using SurveyPortal.API.Repositories.Interfaces;

namespace SurveyPortal.API.Services
{
    public class OptionService : IOptionService
    {
        private readonly IGenericRepository<Option> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OptionService(IGenericRepository<Option> repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<OptionDto>> GetOptionsByQuestionIdAsync(int questionId)
        {
            // Sadece silinmemiş seçenekleri getir ve OrderNumber'a göre sırala
            var options = await _repository.Where(o => o.QuestionId == questionId && o.IsDeleted == false)
                                           .OrderBy(o => o.OrderNumber)
                                           .ToListAsync();

            return _mapper.Map<IEnumerable<OptionDto>>(options);
        }

        public async Task<OptionDto> CreateOptionAsync(OptionDto optionDto)
        {
            var option = _mapper.Map<Option>(optionDto);
            option.CreatedDate = DateTime.Now;

            await _repository.AddAsync(option);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<OptionDto>(option);
        }

        public async Task DeleteOptionAsync(int id)
        {
            var option = await _repository.GetByIdAsync(id);
            if (option != null && !option.IsDeleted)
            {
                option.IsDeleted = true; 
                option.UpdatedDate = DateTime.Now;

                _repository.Update(option);
                await _unitOfWork.CommitAsync();
            }
        }
        public async Task UpdateOptionAsync(int id, OptionDto optionDto)
        {
            var option = await _repository.GetByIdAsync(id);
            if (option != null && !option.IsDeleted)
            {
                option.OptionText = optionDto.OptionText;
                option.OrderNumber = optionDto.OrderNumber;
                option.UpdatedDate = DateTime.Now;

                _repository.Update(option);
                await _unitOfWork.CommitAsync();
            }
        }
    }
}