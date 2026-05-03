namespace SurveyPortal.API.DTOs
{
    public class SurveyDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string Status { get; set; } = "Draft";

        public int CategoryId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsAnonymous { get; set; }
    }
}