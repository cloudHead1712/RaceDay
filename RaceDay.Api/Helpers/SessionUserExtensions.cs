namespace RaceDay.Api.Helpers;

public static class SessionUserExtensions
{
	public static int? GetUserId(this ISession session) => session.GetInt32("UserId");
	public static string? GetRole(this ISession session) => session.GetString("Role");
}