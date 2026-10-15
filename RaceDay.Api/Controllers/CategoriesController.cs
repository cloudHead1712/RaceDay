using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Models.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/categories")]
public class CategoriesController : ControllerBase
{
	private readonly AppDbContext _db;

	public CategoriesController(AppDbContext db)
	{
		_db = db;
	}

	/// <summary>Lists the categories of one event.</summary>
	[HttpGet]
	[ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetAll(int eventId)
	{
		if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
			return NotFound(new { message = "Event not found." });

		var list = await _db.Categories.AsNoTracking()
			.Where(c => c.EventId == eventId)
			.OrderBy(c => c.Name)
			.Select(c => new CategoryResponse { CategoryId = c.CategoryId, EventId = c.EventId, Name = c.Name })
			.ToListAsync();

		return Ok(list);
	}

	/// <summary>Adds a category to an event. Only the owning Organiser may do this.</summary>
	[HttpPost]
	[SessionAuthorize(Roles = "Organiser")]
	[ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Create(int eventId, CategoryRequest request)
	{
		var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
		if (ev is null) return NotFound(new { message = "Event not found." });

		if (ev.OrganiserId != HttpContext.Session.GetUserId())
			return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only add categories to your own events." });

		var name = request.Name.Trim();
		if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == name))
			return Conflict(new { message = "This event already has a category with that name." });

		var category = new Category { EventId = eventId, Name = name };
		_db.Categories.Add(category);
		await _db.SaveChangesAsync();

		return StatusCode(StatusCodes.Status201Created,
			new CategoryResponse { CategoryId = category.CategoryId, EventId = category.EventId, Name = category.Name });
	}
}