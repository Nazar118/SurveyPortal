namespace SurveyPortal.API.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalSurveys { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalResponses { get; set; }

        public string MostPopularSurveyTitle { get; set; } = string.Empty;
        public List<DailyActivityDto> ActivityLast7Days { get; set; } = new();

        public List<LatestSurveyDto>? LatestSurveys { get; set; }
    }

    public class DailyActivityDto
    {
        public string Date { get; set; } = string.Empty;
        public int ResponseCount { get; set; }
    }

    public class LatestSurveyDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}