using Microsoft.IdentityModel.Tokens;
using SurveyPortal.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SurveyPortal.API.Helpers
{
    public class JwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        // appsettings.json dosyasındaki ayarları okumak için IConfiguration kullanıyoruz
        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(AppUser user, IList<string> roles)
        {
            // 1. Kullanıcı bilgilerini (Claims) hazırlıyoruz.
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id), 
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Token'a özel benzersiz ID
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!)
            };

            // 2. Kullanıcının rollerini (Admin, User vb.) token'a ekliyoruz
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // 3. appsettings.json'dan gizli anahtarımızı (Key) alıp şifreliyoruz
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 4. Token ayarlarını ve süresini belirliyoruz
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:DurationInMinutes"])),
                signingCredentials: creds
            );

            // 5. Oluşturulan token'ı string (metin) formatında geri döndürüyoruz
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}