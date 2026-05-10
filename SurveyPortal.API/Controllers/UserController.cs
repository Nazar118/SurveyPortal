using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyPortal.API.Data;
using SurveyPortal.API.DTOs;
using SurveyPortal.API.Models;
using System.Security.Claims;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;

        public UserController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile([FromServices] AppDbContext context)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var roles = await _userManager.GetRolesAsync(user);
            string mainRole = roles.Contains("Admin") ? "Admin" : "Kullanıcı";

            // 1. Katılım Geçmişini Çek
            var myResponses = await context.Set<SurveyResponse>()
                .Include(r => r.Survey)
                .Where(r => r.AppUserId == userId)
                .OrderByDescending(r => r.CompletedAt ?? r.CreatedDate)
                .ToListAsync();

            var participatedSurveys = myResponses.Select(r => new {
                SurveyId = r.SurveyId,
                SurveyTitle = r.Survey!.Title,
                CompletedDate = r.CompletedAt ?? r.CreatedDate
            }).ToList();

            // 2. İstatistikleri Hesapla (Stats)
            int totalSurveysSolved = myResponses.Count;
            int totalCategoriesExplored = myResponses.Select(r => r.Survey!.CategoryId).Distinct().Count();

            // 3. Rozetleri (Achievements) Belirle
            var achievements = new List<object>();

            if (totalSurveysSolved >= 1)
                achievements.Add(new { Icon = "🏆", Title = "İlk Adım", Desc = "İlk anketini başarıyla tamamladın." });

            if (totalSurveysSolved >= 5)
                achievements.Add(new { Icon = "🔥", Title = "Anket Canavarı", Desc = "5'ten fazla anketi çözerek rekor kırdın!" });

            if (totalCategoriesExplored >= 3)
                achievements.Add(new { Icon = "🧠", Title = "Kaşif", Desc = "3 farklı kategoride bilgi paylaştın." });

            // 4. Favorileri (Bookmarks) Çek
            var bookmarks = await context.Bookmarks
                .Include(b => b.Survey)
                .Where(b => b.AppUserId == userId && !b.Survey!.IsDeleted)
                .OrderByDescending(b => b.CreatedDate)
                .Select(b => new {
                    SurveyId = b.SurveyId,
                    SurveyTitle = b.Survey!.Title
                }).ToListAsync();

            return Ok(new
            {
                Username = user.UserName,
                Email = user.Email,
                Role = mainRole,
                Stats = new { TotalSolved = totalSurveysSolved, CategoriesExplored = totalCategoriesExplored },
                Achievements = achievements,
                ParticipatedSurveys = participatedSurveys,
                Bookmarks = bookmarks
            });
        }

        [HttpPost("bookmark/{surveyId}")]
        public async Task<IActionResult> ToggleBookmark(int surveyId, [FromServices] AppDbContext context)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingBookmark = await context.Bookmarks.FirstOrDefaultAsync(b => b.SurveyId == surveyId && b.AppUserId == userId);

            if (existingBookmark != null)
            {
                context.Bookmarks.Remove(existingBookmark);
                await context.SaveChangesAsync();
                return Ok(new { IsBookmarked = false, Message = "Anket favorilerden çıkarıldı." });
            }
            else
            {
                var newBookmark = new Bookmark { SurveyId = surveyId, AppUserId = userId, CreatedDate = DateTime.Now };
                context.Bookmarks.Add(newBookmark);
                await context.SaveChangesAsync();
                return Ok(new { IsBookmarked = true, Message = "Anket favorilere eklendi." });
            }
        }

        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            if (user.Email != model.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(model.Email);
                if (emailExists != null) return BadRequest("Bu e-posta adresi zaten kullanımda.");
            }

            user.UserName = model.UserName;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) return Ok(new { Message = "Profil bilgileriniz başarıyla güncellendi." });

            return BadRequest("Güncelleme başarısız oldu.");
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded) return Ok(new { Message = "Şifreniz başarıyla değiştirildi." });

            return BadRequest("Mevcut şifreniz yanlış veya yeni şifre kurallara uymuyor.");
        }

        // --- ADMİN METODLARI ---

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = _userManager.Users.ToList();
            var result = new List<object>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                string mainRole = roles.Contains("Admin") ? "Admin" : "Kullanıcı";

                result.Add(new
                {
                    id = u.Id,
                    userName = u.UserName,
                    email = u.Email,
                    role = mainRole,
                    isActive = !await _userManager.IsLockedOutAsync(u)
                });
            }

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                return Ok(new { Message = "Kullanıcı engeli kaldırıldı." });
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                return Ok(new { Message = "Kullanıcı başarıyla engellendi." });
            }
        }
    }
}