namespace SurveyPortal.API.DTOs
{
    public class AnswerDto
    {
        public int Id { get; set; }

        // Bu cevap hangi ankete katılımın (oturumun) parçası?
        public int SurveyResponseId { get; set; }

        // Hangi soruya cevap veriliyor?
        public int QuestionId { get; set; }

        public int? OptionId { get; set; }

        public string? TextAnswer { get; set; }
    }
}