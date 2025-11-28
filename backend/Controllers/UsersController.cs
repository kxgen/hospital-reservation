using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Backend.Data;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IActionResult GetUserData()
    {
        // Only return basic info
        var users = InMemoryStore.Users.Select(u => new { u.Email, u.Role }).ToList();
        return Ok(users);
    }
}
