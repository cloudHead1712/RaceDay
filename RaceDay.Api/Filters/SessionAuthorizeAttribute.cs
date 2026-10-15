using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
	public string? Roles { get; set; }

	public void OnAuthorization(AuthorizationFilterContext context)
	{
		var session = context.HttpContext.Session;

		if (session.GetUserId() is null)
		{
			context.Result = new UnauthorizedObjectResult(new { message = "You must be logged in." });
			return;
		}

		if (!string.IsNullOrEmpty(Roles))
		{
			var allowed = Roles.Split(',', StringSplitOptions.TrimEntries);
			if (!allowed.Contains(session.GetRole()))
			{
				context.Result = new ObjectResult(new { message = "You do not have permission to do this." })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}
		}
	}
}