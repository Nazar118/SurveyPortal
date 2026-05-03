namespace SurveyPortal.API.Models
{
    public class Survey : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string Status { get; set; } = "Draft";

        public bool IsAnonymous { get; set; } = false;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public ICollection<Question>? Questions { get; set; }
        public ICollection<SurveyResponse>? SurveyResponses { get; set; }
    }
}