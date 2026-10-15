namespace RaceDay.Api.Models;

public class EventType
{
	public int EventTypeId { get; set; }
	public string Name { get; set; } = string.Empty;

	public ICollection<Event> Events { get; set; } = new List<Event>();
}