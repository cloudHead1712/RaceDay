using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Models.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
	private readonly AppDbContext _db;

	public EventsController(AppDbContext db)
	{
		_db = db;
	}

	private static readonly Expression<Func<Event, EventResponse>> Map = e => new EventResponse
	{
		EventId = e.EventId,
		Name = e.Name,
		Description = e.Description,
		Date = e.Date,
		Location = e.Location,
		Distance = e.Distance,
		EventTypeId = e.EventTypeId,
		EventTypeName = e.EventType!.Name,
		OrganiserId = e.OrganiserId,
		OrganiserName = e.Organiser!.FullName,
		BannerImageUrl = e.BannerImageUrl
	};

	/// <summary>Lists events. Optional filters: eventTypeId, search text, upcomingOnly (default true).</summary>
	[HttpGet]
	[ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll([FromQuery] int? eventTypeId, [FromQuery] string? search, [FromQuery] bool upcomingOnly = true)
	{
		var query = _db.Events.AsNoTracking().AsQueryable();

		if (upcomingOnly)
			query = query.Where(e => e.Date >= DateTime.UtcNow);

		if (eventTypeId.HasValue)
			query = query.Where(e => e.EventTypeId == eventTypeId.Value);

		if (!string.IsNullOrWhiteSpace(search))
		{
			var s = search.Trim();
			query = query.Where(e => e.Name.Contains(s) || e.Location.Contains(s));
		}

		var list = await query.OrderBy(e => e.Date).Select(Map).ToListAsync();
		return Ok(list);
	}

	/// <summary>Returns one event with its categories.</summary>
	[HttpGet("{id:int}")]
	[ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetById(int id)
	{
		var ev = await _db.Events.AsNoTracking().Where(e => e.EventId == id).Select(Map).FirstOrDefaultAsync();
		if (ev is null) return NotFound(new { message = "Event not found." });

		ev.Categories = await _db.Categories.AsNoTracking()
			.Where(c => c.EventId == id)
			.OrderBy(c => c.Name)
			.Select(c => new CategoryResponse { CategoryId = c.CategoryId, EventId = c.EventId, Name = c.Name })
			.ToListAsync();

		return Ok(ev);
	}

	/// <summary>Creates an event owned by the logged-in Organiser.</summary>
	[HttpPost]
	[SessionAuthorize(Roles = "Organiser")]
	[ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Create(EventRequest request)
	{
		if (!await _db.EventTypes.AnyAsync(t => t.EventTypeId == request.EventTypeId))
			return BadRequest(new { message = "Invalid event type." });

		var ev = new Event
		{
			Name = request.Name.Trim(),
			Description = request.Description,
			Date = request.Date!.Value,
			Location = request.Location.Trim(),
			Distance = request.Distance,
			EventTypeId = request.EventTypeId,
			OrganiserId = HttpContext.Session.GetUserId()!.Value,
			BannerImageUrl = request.BannerImageUrl
		};

		_db.Events.Add(ev);
		await _db.SaveChangesAsync();

		var created = await _db.Events.AsNoTracking().Where(e => e.EventId == ev.EventId).Select(Map).FirstAsync();
		return StatusCode(StatusCodes.Status201Created, created);
	}

	/// <summary>Updates an event. Only the Organiser who owns it may do this.</summary>
	[HttpPut("{id:int}")]
	[SessionAuthorize(Roles = "Organiser")]
	[ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Update(int id, EventRequest request)
	{
		var ev = await _db.Events.FindAsync(id);
		if (ev is null) return NotFound(new { message = "Event not found." });

		if (ev.OrganiserId != HttpContext.Session.GetUserId())
			return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only edit your own events." });

		if (!await _db.EventTypes.AnyAsync(t => t.EventTypeId == request.EventTypeId))
			return BadRequest(new { message = "Invalid event type." });

		ev.Name = request.Name.Trim();
		ev.Description = request.Description;
		ev.Date = request.Date!.Value;
		ev.Location = request.Location.Trim();
		ev.Distance = request.Distance;
		ev.EventTypeId = request.EventTypeId;
		ev.BannerImageUrl = request.BannerImageUrl;
		await _db.SaveChangesAsync();

		var updated = await _db.Events.AsNoTracking().Where(e => e.EventId == id).Select(Map).FirstAsync();
		return Ok(updated);
	}

	/// <summary>Deletes an event. Only the owning Organiser may do this.</summary>
	[HttpDelete("{id:int}")]
	[SessionAuthorize(Roles = "Organiser")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Delete(int id)
	{
		var ev = await _db.Events.FindAsync(id);
		if (ev is null) return NotFound(new { message = "Event not found." });

		if (ev.OrganiserId != HttpContext.Session.GetUserId())
			return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete your own events." });

		if (await _db.Enrolments.AnyAsync(e => e.EventId == id))
			return Conflict(new { message = "Cannot delete an event that already has enrolments." });

		_db.Events.Remove(ev);
		await _db.SaveChangesAsync();
		return NoContent();
	}
}