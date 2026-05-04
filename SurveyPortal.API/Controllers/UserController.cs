using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyPortal.API.Data; 
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
            // JWT Token'dan kimliği al
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var roles = await _userManager.GetRolesAsync(user);
            string mainRole = roles.Contains("Admin") ? "Admin" : "Kullanıcı";

            var mySurveys = await context.Set<SurveyResponse>()
                .Include(r => r.Survey)
                .Where(r => r.AppUserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .Select(r => new {
                    SurveyId = r.SurveyId,
                    SurveyTitle = r.Survey!.Title,
                    CompletedDate = r.CreatedDate
                })
                .ToListAsync();

            return Ok(new
            {
                Username = user.UserName,
                Email = user.Email,
                Role = mainRole,
                ParticipatedSurveys = mySurveys
            });
        }


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

        [Authorize(Roles = "Admin")] // Admin kilidini buraya taşıdık
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