using AutoMapper;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Models;

namespace SurveyPortal.API.Mappings
{
    public class MapProfile : Profile
    {
        public MapProfile()
        {
            CreateMap<Category, CategoryDto>().ReverseMap();

            CreateMap<Survey, SurveyDto>().ReverseMap();
            CreateMap<Question, QuestionDto>().ReverseMap();
        }
    }
}