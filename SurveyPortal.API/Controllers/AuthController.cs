using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Helpers;
using SurveyPortal.API.Models;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly JwtTokenGenerator _jwtTokenGenerator;

        // Identity'nin kullanıcı yöneticisini ve kendi yazdığımız Token makinesini çağırıyoruz
        public AuthController(UserManager<AppUser> userManager, JwtTokenGenerator jwtTokenGenerator)
        {
            _userManager = userManager;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // Kullanıcı adı daha önce alınmış mı kontrolü
            var userExists = await _userManager.FindByNameAsync(registerDto.UserName);
            if (userExists != null)
                return BadRequest("Bu kullanıcı adı zaten alınmış.");

            var emailExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (emailExists != null)
                return BadRequest("Bu email adresi zaten kullanımda.");

            // DTO'dan gelen bilgilerle yeni bir kullanıcı (AppUser) oluşturuyoruz
            var user = new AppUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName
            };

            // Kullanıcıyı veri tabanına şifresiyle beraber (şifreleyerek) kaydetme
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                // Şifre kurallarına uyulmadıysa 
                return BadRequest(result.Errors);
            }

            return Ok(new { Message = "Kullanıcı başarıyla kaydedildi." });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // 1. Kullanıcıyı veri tabanında bul
            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            if (user == null)
                return Unauthorized("Geçersiz kullanıcı adı veya şifre.");

            // 2. Şifreyi kontrol et
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
                return Unauthorized("Geçersiz kullanıcı adı veya şifre.");

            // 3. Kullanıcının rollerini al 
            var roles = await _userManager.GetRolesAsync(user);

            // 4. Token makinemizi çalıştır ve şifreli anahtarı (Token) üret
            var token = _jwtTokenGenerator.GenerateToken(user, roles);

            // 5. Üretilen Token'ı kullanıcıya ver
            return Ok(new { Token = token, Message = "Giriş başarılı." });
        }
    }
}