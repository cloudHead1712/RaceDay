using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
	private readonly string _dbName = Guid.NewGuid().ToString();

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");
		builder.ConfigureServices(services =>
		{
			services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
		});
	}

	protected override IHost CreateHost(IHostBuilder builder)
	{
		var host = base.CreateHost(builder);
		using var scope = host.Services.CreateScope();
		scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
		return host;
	}
}