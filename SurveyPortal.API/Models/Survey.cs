namespace SurveyPortal.API.Models
{
    public class Survey : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // Status & Settings
        public bool IsActive { get; set; } = true;
        public bool IsPublished { get; set; } = false;
        public bool IsAnonymous { get; set; } = false;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Foreign Keys
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // Navigation Properties
        public ICollection<Question>? Questions { get; set; }
        public ICollection<SurveyResponse>? SurveyResponses { get; set; }
    }
}