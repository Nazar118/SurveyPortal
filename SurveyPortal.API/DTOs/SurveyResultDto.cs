namespace SurveyPortal.API.DTOs
{
    //  Anketin Genel Sonucu
    public class SurveyResultDto
    {
        public int SurveyId { get; set; }
        public string SurveyTitle { get; set; } = string.Empty;
        public int TotalParticipants { get; set; } // Toplam kaç kişi çözdü?
        public List<QuestionResultDto> Questions { get; set; } = new List<QuestionResultDto>();
    }

    //  Soruların Sonucu
    public class QuestionResultDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int TotalAnswers { get; set; } // Bu soruya toplam kaç cevap verilmiş?
        public List<OptionResultDto> Options { get; set; } = new List<OptionResultDto>();
    }

    //  Şıkların (Seçeneklerin) Sonucu ve YÜZDELİK DİLİM
    public class OptionResultDto
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int VoteCount { get; set; } // Bu şıkkı kaç kişi seçmiş?
        public double Percentage { get; set; } // % Kaç oy almış?
    }
}