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
public class EnrolmentsController : ControllerBase
{
	private readonly AppDbContext _db;

	public EnrolmentsController(AppDbContext db)
	{
		_db = db;
	}

	private static readonly Expression<Func<Enrolment, EnrolmentResponse>> Map = e => new EnrolmentResponse
	{
		EnrolmentId = e.EnrolmentId,
		ParticipantId = e.ParticipantId,
		ParticipantName = e.Participant!.FullName,
		EventId = e.EventId,
		EventName = e.Event!.Name,
		EventDate = e.Event!.Date,
		CategoryId = e.CategoryId,
		CategoryName = e.Category!.Name,
		EnrolmentStatus = e.EnrolmentStatus,
		EnrolmentDate = e.EnrolmentDate
	};

	/// <summary>Enrols the logged-in Participant in an event under a chosen category.</summary>
	[HttpPost("api/events/{eventId:int}/enrolments")]
	[SessionAuthorize(Roles = "Participant")]
	[ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Enrol(int eventId, EnrolmentRequest request)
	{
		if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
			return NotFound(new { message = "Event not found." });

		var category = await _db.Categories.AsNoTracking()
			.FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == eventId);
		if (category is null)
			return BadRequest(new { message = "Category does not belong to this event." });

		var userId = HttpContext.Session.GetUserId()!.Value;
		if (await _db.Enrolments.AnyAsync(e => e.EventId == eventId && e.ParticipantId == userId))
			return Conflict(new { message = "You are already enrolled in this event." });

		var enrolment = new Enrolment
		{
			ParticipantId = userId,
			EventId = eventId,
			CategoryId = category.CategoryId
		};
		_db.Enrolments.Add(enrolment);
		await _db.SaveChangesAsync();

		var created = await _db.Enrolments.AsNoTracking()
			.Where(e => e.EnrolmentId == enrolment.EnrolmentId).Select(Map).FirstAsync();
		return StatusCode(StatusCodes.Status201Created, created);
	}

	/// <summary>Lists the logged-in Participant's own enrolments.</summary>
	[HttpGet("api/enrolments/mine")]
	[SessionAuthorize(Roles = "Participant")]
	[ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Mine()
	{
		var userId = HttpContext.Session.GetUserId()!.Value;
		var list = await _db.Enrolments.AsNoTracking()
			.Where(e => e.ParticipantId == userId)
			.OrderBy(e => e.Event!.Date)
			.Select(Map)
			.ToListAsync();
		return Ok(list);
	}

	/// <summary>Lists all enrolments for an event. Only the owning Organiser may do this.</summary>
	[HttpGet("api/events/{eventId:int}/enrolments")]
	[SessionAuthorize(Roles = "Organiser")]
	[ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> ForEvent(int eventId)
	{
		var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
		if (ev is null) return NotFound(new { message = "Event not found." });

		if (ev.OrganiserId != HttpContext.Session.GetUserId())
			return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only view enrolments for your own events." });

		var list = await _db.Enrolments.AsNoTracking()
			.Where(e => e.EventId == eventId)
			.OrderBy(e => e.EnrolmentDate)
			.Select(Map)
			.ToListAsync();
		return Ok(list);
	}
}