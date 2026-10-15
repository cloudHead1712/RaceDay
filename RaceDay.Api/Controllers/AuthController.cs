using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Models.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly PasswordHasher<User> _hasher = new();

	public AuthController(AppDbContext db)
	{
		_db = db;
	}

	/// <summary>Registers a new Organiser or Participant account.</summary>
	[HttpPost("register")]
	[ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Register(RegisterRequest request)
	{
		var email = request.Email.Trim().ToLowerInvariant();

		if (await _db.Users.AnyAsync(u => u.Email == email))
			return Conflict(new { message = "Email already registered." });

		var user = new User
		{
			FullName = request.FullName.Trim(),
			Email = email,
			Role = request.Role
		};
		user.PasswordHash = _hasher.HashPassword(user, request.Password);

		_db.Users.Add(user);
		await _db.SaveChangesAsync();

		return StatusCode(StatusCodes.Status201Created, ToResponse(user));
	}

	/// <summary>Logs in with email and password and starts a session.</summary>
	[HttpPost("login")]
	[ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Login(LoginRequest request)
	{
		var email = request.Email.Trim().ToLowerInvariant();
		var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

		if (user is null ||
			_hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
		{
			return Unauthorized(new { message = "Invalid email or password." });
		}

		HttpContext.Session.SetInt32("UserId", user.UserId);
		HttpContext.Session.SetString("Role", user.Role);

		return Ok(ToResponse(user));
	}

	/// <summary>Ends the current session.</summary>
	[HttpPost("logout")]
	[SessionAuthorize]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public IActionResult Logout()
	{
		HttpContext.Session.Clear();
		return Ok(new { message = "Logged out." });
	}

	private static UserResponse ToResponse(User u) => new()
	{
		UserId = u.UserId,
		FullName = u.FullName,
		Email = u.Email,
		Role = u.Role,
		ProfilePictureUrl = u.ProfilePictureUrl
	};
}