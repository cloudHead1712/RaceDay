using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/profile")]
[SessionAuthorize]
public class ProfileController : ControllerBase
{
	private readonly AppDbContext _db;

	public ProfileController(AppDbContext db)
	{
		_db = db;
	}

	/// <summary>Returns the logged-in user's own profile.</summary>
	[HttpGet]
	[ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Get()
	{
		var id = HttpContext.Session.GetUserId()!.Value;
		var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
		if (user is null) return Unauthorized(new { message = "Account no longer exists." });

		return Ok(ToResponse(user));
	}

	/// <summary>Updates the logged-in user's name and profile picture URL.</summary>
	[HttpPut]
	[ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Update(UpdateProfileRequest request)
	{
		var id = HttpContext.Session.GetUserId()!.Value;
		var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
		if (user is null) return Unauthorized(new { message = "Account no longer exists." });

		user.FullName = request.FullName.Trim();
		user.ProfilePictureUrl = request.ProfilePictureUrl;
		await _db.SaveChangesAsync();

		return Ok(ToResponse(user));
	}

	private static UserResponse ToResponse(Models.User u) => new()
	{
		UserId = u.UserId,
		FullName = u.FullName,
		Email = u.Email,
		Role = u.Role,
		ProfilePictureUrl = u.ProfilePictureUrl
	};
}