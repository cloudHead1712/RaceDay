using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models.Dtos;

public class UpdateProfileRequest
{
	[Required, StringLength(100)]
	public string FullName { get; set; } = string.Empty;

	[StringLength(500)]
	public string? ProfilePictureUrl { get; set; }
}