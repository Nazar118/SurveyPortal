namespace SurveyPortal.API.Models
{
    public class SurveyResponse : BaseEntity
    {
        public string? IpAddress { get; set; } 
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; } // Bitirme zamanı
        public bool IsCompleted { get; set; } = false; // Tamamlandı mı?

        // İlişkiler
        public int SurveyId { get; set; }
        public Survey? Survey { get; set; } // Hangi anket dolduruluyor?

        public string? UserId { get; set; } 

        public ICollection<Answer>? Answers { get; set; }
        public string? AppUserId { get; set; }
        public AppUser? AppUser { get; set; }
    }
}