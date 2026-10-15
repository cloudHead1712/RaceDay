using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models.Dtos;

public class EventRequest
{
	[Required, StringLength(150)]
	public string Name { get; set; } = string.Empty;

	[StringLength(1000)]
	public string? Description { get; set; }

	[Required]
	public DateTime? Date { get; set; }

	[Required, StringLength(150)]
	public string Location { get; set; } = string.Empty;

	[Range(0.01, 9999.99)]
	public decimal Distance { get; set; }

	[Range(1, int.MaxValue)]
	public int EventTypeId { get; set; }

	[StringLength(500)]
	public string? BannerImageUrl { get; set; }
}

public class EventResponse
{
	public int EventId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public DateTime Date { get; set; }
	public string Location { get; set; } = string.Empty;
	public decimal Distance { get; set; }
	public int EventTypeId { get; set; }
	public string EventTypeName { get; set; } = string.Empty;
	public int OrganiserId { get; set; }
	public string OrganiserName { get; set; } = string.Empty;
	public string? BannerImageUrl { get; set; }
	public List<CategoryResponse>? Categories { get; set; }
}

public class CategoryRequest
{
	[Required, StringLength(50)]
	public string Name { get; set; } = string.Empty;
}

public class CategoryResponse
{
	public int CategoryId { get; set; }
	public int EventId { get; set; }
	public string Name { get; set; } = string.Empty;
}