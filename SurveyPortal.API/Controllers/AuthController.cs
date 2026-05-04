using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Helpers;
using SurveyPortal.API.Models;
using SurveyPortal.API.Services;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly JwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;

        public AuthController(UserManager<AppUser> userManager, JwtTokenGenerator jwtTokenGenerator, IEmailService emailService)
        {
            _userManager = userManager;
            _jwtTokenGenerator = jwtTokenGenerator;
            _emailService = emailService;
        }
       

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var userExists = await _userManager.FindByNameAsync(registerDto.UserName);
            if (userExists != null) return BadRequest("Bu kullanıcı adı zaten alınmış.");

            var emailExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (emailExists != null) return BadRequest("Bu email adresi zaten kullanımda.");

            var user = new AppUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                return Ok(new { Message = "Kullanıcı başarıyla kaydedildi." });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            if (user == null) return Unauthorized("Geçersiz kullanıcı adı veya şifre.");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid) return Unauthorized("Geçersiz kullanıcı adı veya şifre.");

            var roles = await _userManager.GetRolesAsync(user);
            string primaryRole = roles.Any(r => r.ToLower() == "admin") ? "Admin" : roles.FirstOrDefault() ?? "User";

            var token = _jwtTokenGenerator.GenerateToken(user, roles);

            return Ok(new { Token = token, Role = primaryRole, Message = "Giriş başarılı." });
        }

        [HttpGet("make-admin/{username}")]
        public async Task<IActionResult> MakeAdmin(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound("Böyle bir kullanıcı yok.");

            await _userManager.AddToRoleAsync(user, "Admin");
            return Ok(new { Message = $"{username} adlı kullanıcı artık bir ADMİN! Şimdi giriş yapabilirsiniz." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest("Bu e-posta adresine ait bir hesap bulunamadı.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = $"https://localhost:7095/Auth/ResetPassword?email={user.Email}&token={Uri.EscapeDataString(token)}";

            var mailBody = $@"
                <h3>Şifre Sıfırlama Talebi</h3>
                <p>SurveyPortal hesabınızın şifresini sıfırlamak için aşağıdaki bağlantıya tıklayın:</p>
                <a href='{resetLink}' style='padding: 10px 20px; background-color: #b66dff; color: white; text-decoration: none; border-radius: 5px;'>Şifremi Sıfırla</a>
                <p>Eğer bu talebi siz yapmadıysanız, bu e-postayı dikkate almayın.</p>";

            await _emailService.SendEmailAsync(user.Email, "SurveyPortal - Şifre Sıfırlama", mailBody);

            return Ok(new { Message = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest("Geçersiz işlem.");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result.Succeeded) return Ok(new { Message = "Şifreniz başarıyla sıfırlandı." });

            var errors = string.Join("\n", result.Errors.Select(e => e.Description));
            return BadRequest(errors);
        }
    }
}