using Microsoft.AspNetCore.Identity;

namespace SurveyPortal.API.Models
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Navigasyon: Bir kullanıcının katıldığı anket oturumları
        public ICollection<SurveyResponse>? SurveyResponses { get; set; }
    }
}