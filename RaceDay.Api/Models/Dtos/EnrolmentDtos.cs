using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models.Dtos;

public class EnrolmentRequest
{
	[Range(1, int.MaxValue)]
	public int CategoryId { get; set; }
}

public class EnrolmentResponse
{
	public int EnrolmentId { get; set; }
	public int ParticipantId { get; set; }
	public string ParticipantName { get; set; } = string.Empty;
	public int EventId { get; set; }
	public string EventName { get; set; } = string.Empty;
	public DateTime EventDate { get; set; }
	public int CategoryId { get; set; }
	public string CategoryName { get; set; } = string.Empty;
	public string EnrolmentStatus { get; set; } = string.Empty;
	public DateTime EnrolmentDate { get; set; }
}

public class ResultRequest
{
	[Required]
	public TimeSpan? FinishTime { get; set; }

	[Range(1, int.MaxValue)]
	public int FinishingPosition { get; set; }
}

public class ResultResponse
{
	public int ResultId { get; set; }
	public int EnrolmentId { get; set; }
	public string EventName { get; set; } = string.Empty;
	public DateTime EventDate { get; set; }
	public string CategoryName { get; set; } = string.Empty;
	public TimeSpan FinishTime { get; set; }
	public int FinishingPosition { get; set; }
}