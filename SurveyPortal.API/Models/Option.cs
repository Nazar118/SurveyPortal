namespace SurveyPortal.API.Models
{
    public class Option : BaseEntity
    {
        public string OptionText { get; set; } = string.Empty; // Seçenek metni
        public int OrderNumber { get; set; } // Seçenek sırası

        // İlişkiler
        public int QuestionId { get; set; }
        public Question? Question { get; set; } 
    }
}