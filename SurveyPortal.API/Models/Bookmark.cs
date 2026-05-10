namespace SurveyPortal.API.Models
{
    public class Bookmark : BaseEntity
    {
        public string AppUserId { get; set; } = string.Empty;
        public AppUser? AppUser { get; set; }

        public int SurveyId { get; set; }
        public Survey? Survey { get; set; }
    }
}