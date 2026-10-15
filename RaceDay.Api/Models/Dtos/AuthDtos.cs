using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models.Dtos;

public class RegisterRequest
{
	[Required, StringLength(100)]
	public string FullName { get; set; } = string.Empty;

	[Required, EmailAddress, StringLength(150)]
	public string Email { get; set; } = string.Empty;

	[Required, MinLength(6)]
	public string Password { get; set; } = string.Empty;

	[Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be Organiser or Participant.")]
	public string Role { get; set; } = string.Empty;
}

public class LoginRequest
{
	[Required, EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string Password { get; set; } = string.Empty;
}

public class UserResponse
{
	public int UserId { get; set; }
	public string FullName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;
	public string? ProfilePictureUrl { get; set; }
}