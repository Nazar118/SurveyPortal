namespace SurveyPortal.API.DTOs
{
    public class OptionDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int OrderNumber { get; set; } 
        public int QuestionId { get; set; }  
    }
}