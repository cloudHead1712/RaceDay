using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Api.Models.Dtos;

namespace RaceDay.Api.Tests;

public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient NewClient() => _factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });

    private async Task<HttpClient> LoggedInClient(string role)
    {
        var client = NewClient();
        var email = $"{Guid.NewGuid():N}@test.com";

        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { FullName = "Test User", Email = email, Password = "Pass123!", Role = role });
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "Pass123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        return client;
    }

    private static EventRequest SampleEvent() => new()
    {
        Name = "Test Run",
        Description = "Test event",
        Date = DateTime.UtcNow.AddDays(30),
        Location = "Pretoria",
        Distance = 10,
        EventTypeId = 1
    };

    private static async Task<EventResponse> CreateEvent(HttpClient organiser)
    {
        var r = await organiser.PostAsJsonAsync("/api/events", SampleEvent());
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    private static async Task<CategoryResponse> CreateCategory(HttpClient organiser, int eventId)
    {
        var r = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new CategoryRequest { Name = "10km" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    // ---------- Registration and login ----------

    [Fact]
    public async Task Register_ValidUser_Returns201()
    {
        var client = NewClient();
        var r = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { FullName = "New User", Email = $"{Guid.NewGuid():N}@test.com", Password = "Pass123!", Role = "Participant" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = NewClient();
        var req = new RegisterRequest { FullName = "Dup", Email = $"{Guid.NewGuid():N}@test.com", Password = "Pass123!", Role = "Participant" };
        await client.PostAsJsonAsync("/api/auth/register", req);
        var second = await client.PostAsJsonAsync("/api/auth/register", req);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = NewClient();
        var r = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { FullName = "Bad Role", Email = $"{Guid.NewGuid():N}@test.com", Password = "Pass123!", Role = "Admin" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = NewClient();
        var email = $"{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { FullName = "User", Email = email, Password = "Pass123!", Role = "Participant" });

        var r = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "WrongPass1!" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var client = await LoggedInClient("Participant");
        var profile = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    // ---------- Authenticated vs unauthenticated ----------

    [Fact]
    public async Task Profile_Unauthenticated_Returns401()
    {
        var r = await NewClient().GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_Unauthenticated_Returns401()
    {
        var r = await NewClient().PostAsJsonAsync("/api/events", SampleEvent());
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    // ---------- Event management and roles ----------

    [Fact]
    public async Task Organiser_CanCreateUpdateAndDeleteOwnEvent()
    {
        var organiser = await LoggedInClient("Organiser");
        var ev = await CreateEvent(organiser);

        var update = SampleEvent();
        update.Name = "Renamed Run";
        var put = await organiser.PutAsJsonAsync($"/api/events/{ev.EventId}", update);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var del = await organiser.DeleteAsync($"/api/events/{ev.EventId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var get = await organiser.GetAsync($"/api/events/{ev.EventId}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var participant = await LoggedInClient("Participant");
        var r = await participant.PostAsJsonAsync("/api/events", SampleEvent());
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotEditEvent_Returns403()
    {
        var owner = await LoggedInClient("Organiser");
        var other = await LoggedInClient("Organiser");
        var ev = await CreateEvent(owner);

        var r = await other.PutAsJsonAsync($"/api/events/{ev.EventId}", SampleEvent());
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var organiser = await LoggedInClient("Organiser");
        var ev = await CreateEvent(organiser);
        var cat = await CreateCategory(organiser, ev.EventId);

        var r = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments",
            new EnrolmentRequest { CategoryId = cat.CategoryId });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    // ---------- Enrolments ----------

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var organiser = await LoggedInClient("Organiser");
        var participant = await LoggedInClient("Participant");
        var ev = await CreateEvent(organiser);
        var cat = await CreateCategory(organiser, ev.EventId);

        var enrol = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments",
            new EnrolmentRequest { CategoryId = cat.CategoryId });
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/mine");
        Assert.NotNull(mine);
        var single = Assert.Single(mine!);
        Assert.Equal(ev.EventId, single.EventId);
        Assert.Equal(cat.CategoryId, single.CategoryId);
        Assert.Equal("Pending", single.EnrolmentStatus);
    }

    [Fact]
    public async Task Participant_CannotEnrolTwice_Returns409()
    {
        var organiser = await LoggedInClient("Organiser");
        var participant = await LoggedInClient("Participant");
        var ev = await CreateEvent(organiser);
        var cat = await CreateCategory(organiser, ev.EventId);
        var req = new EnrolmentRequest { CategoryId = cat.CategoryId };

        await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", req);
        var second = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", req);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // ---------- Results ----------

    [Fact]
    public async Task Organiser_CapturesResult_ParticipantCanSeeIt()
    {
        var organiser = await LoggedInClient("Organiser");
        var participant = await LoggedInClient("Participant");
        var ev = await CreateEvent(organiser);
        var cat = await CreateCategory(organiser, ev.EventId);

        var enrol = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments",
            new EnrolmentRequest { CategoryId = cat.CategoryId });
        var enrolment = (await enrol.Content.ReadFromJsonAsync<EnrolmentResponse>())!;

        var result = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results",
            new ResultRequest { FinishTime = new TimeSpan(0, 52, 14), FinishingPosition = 47 });
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<ResultResponse>>("/api/results/mine");
        var single = Assert.Single(mine!);
        Assert.Equal(47, single.FinishingPosition);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_Returns403()
    {
        var participant = await LoggedInClient("Participant");
        var r = await participant.PostAsJsonAsync("/api/enrolments/1/results",
            new ResultRequest { FinishTime = new TimeSpan(0, 50, 0), FinishingPosition = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }
}