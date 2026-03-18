namespace SurveyPortal.API.DTOs
{
    public class SurveyDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; } //Tanım
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
    }
}