namespace SurveyPortal.API.Models
{
    public class Answer : BaseEntity
    {
        
        public int SurveyResponseId { get; set; }
        public SurveyResponse? SurveyResponse { get; set; } // Hangi oturuma ait?

        public int QuestionId { get; set; }
        public Question? Question { get; set; } 

        public int? OptionId { get; set; } 
        public Option? Option { get; set; }

        public string? TextAnswer { get; set; } 
    }
}