namespace SurveyPortal.API.DTOs
{
    public class QuestionDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int OrderNumber { get; set; } 
        public bool IsRequired { get; set; } // Zorunlu soru mu?
        public int QuestionType { get; set; } 
        public int SurveyId { get; set; } // Bu soru hangi ankete ait?
    }
}