using Microsoft.AspNetCore.Mvc;
using SurveyPortal.API.Models;
using SurveyPortal.API.Repositories.Interfaces;

namespace SurveyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IGenericRepository<AppUser> _userRepo;

        public UserController(IGenericRepository<AppUser> userRepo)
        {
            _userRepo = userRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userRepo.GetAllAsync();

            var result = users.Select(u => new
            {
                id = u.Id,
                userName = u.UserName, 
                email = u.Email,
                role = "Kullanıcı" 
            });

            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var users = await _userRepo.GetAllAsync();
                var user = users.FirstOrDefault(u => u.Id == id);

                if (user == null)
                {
                    return NotFound("Kullanıcı bulunamadı.");
                }

                _userRepo.Remove(user);
                await _userRepo.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                // Veritabanı tokat atarsa, çökmesini engelliyor ve hatayı biz yakalıyoruz.
                return BadRequest("Bu kullanıcı silinemez! Muhtemelen sisteme eklediği anketler veya çözdüğü sorular var. Lütfen önce kullanıcının hareketlerini silin.");
            }
        }
    }
}