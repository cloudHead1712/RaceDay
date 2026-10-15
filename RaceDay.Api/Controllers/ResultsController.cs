using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Models.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
public class ResultsController : ControllerBase
{
	private readonly AppDbContext _db;

	public ResultsController(AppDbContext db)
	{
		_db = db;
	}

	/// <summary>Captures a finish time and position for an enrolment. Only the owning Organiser may do this.</summary>
	[HttpPost("api/enrolments/{enrolmentId:int}/results")]
	[SessionAuthorize(Roles = "Organiser")]
	[ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Create(int enrolmentId, ResultRequest request)
	{
		var enrolment = await _db.Enrolments
			.Include(e => e.Event)
			.Include(e => e.Category)
			.FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
		if (enrolment is null) return NotFound(new { message = "Enrolment not found." });

		if (enrolment.Event!.OrganiserId != HttpContext.Session.GetUserId())
			return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only capture results for your own events." });

		if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
			return Conflict(new { message = "A result has already been captured for this enrolment." });

		var result = new Result
		{
			EnrolmentId = enrolmentId,
			FinishTime = request.FinishTime!.Value,
			FinishingPosition = request.FinishingPosition
		};
		_db.Results.Add(result);
		await _db.SaveChangesAsync();

		return StatusCode(StatusCodes.Status201Created, new ResultResponse
		{
			ResultId = result.ResultId,
			EnrolmentId = result.EnrolmentId,
			EventName = enrolment.Event.Name,
			EventDate = enrolment.Event.Date,
			CategoryName = enrolment.Category!.Name,
			FinishTime = result.FinishTime,
			FinishingPosition = result.FinishingPosition
		});
	}

	/// <summary>Returns the logged-in Participant's own race results.</summary>
	[HttpGet("api/results/mine")]
	[SessionAuthorize(Roles = "Participant")]
	[ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Mine()
	{
		var userId = HttpContext.Session.GetUserId()!.Value;
		var list = await _db.Results.AsNoTracking()
			.Where(r => r.Enrolment!.ParticipantId == userId)
			.OrderByDescending(r => r.Enrolment!.Event!.Date)
			.Select(r => new ResultResponse
			{
				ResultId = r.ResultId,
				EnrolmentId = r.EnrolmentId,
				EventName = r.Enrolment!.Event!.Name,
				EventDate = r.Enrolment!.Event!.Date,
				CategoryName = r.Enrolment!.Category!.Name,
				FinishTime = r.FinishTime,
				FinishingPosition = r.FinishingPosition
			})
			.ToListAsync();
		return Ok(list);
	}
}