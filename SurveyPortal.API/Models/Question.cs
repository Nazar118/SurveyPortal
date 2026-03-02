namespace SurveyPortal.API.Models
{
    public class Question : BaseEntity
    {
        public string QuestionText { get; set; } = string.Empty; 
        public int OrderNumber { get; set; } // Sorunun görüntülenme sırası
        public bool IsRequired { get; set; } = true; // Zorunlu soru mu?
        
       
        public int QuestionType { get; set; } 

        // İlişkiler
        public int SurveyId { get; set; }
        public Survey? Survey { get; set; } // Hangi ankete ait?

        public ICollection<Option>? Options { get; set; } 
        public ICollection<Answer>? Answers { get; set; } 
    }
}