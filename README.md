# RaceDay
This is Part 2. of the PROG6121 POE. It will consist of the Commits and Continuous Development/Continuous Integration inclusion.
# RaceDay

RaceDay is a web-based event management system for South African road running, walking and cycling events. Organisers create and manage events, categories and results; Participants browse events, enrol, and track their results.

## Part 2: RESTful API

Built with ASP.NET Core Web API (.NET 10), Entity Framework Core (Code-First), SQL Server and session-based authentication.

### Roles

- **Organiser**: creates, edits and deletes their own events, manages event categories, views all enrolments for their events, and captures results.
- **Participant**: creates an account, browses events and categories, enrols in an event by selecting a category, views their own enrolments, and views their own results.

Role-based access is enforced at API level: no session returns 401, and the wrong role or a non-owner returns 403.

### Setup and run

1. Install the .NET 10 SDK and Visual Studio 2026 (SQL Server LocalDB is included).
2. Clone the repository.
3. In a terminal, run:

```
cd RaceDay.Api
dotnet run --launch-profile https
```

4. The database `RaceDayApi` is created automatically on startup through EF Core migrations (connection string in `RaceDay.Api/appsettings.json`).
5. Open `https://localhost:7186/swagger` to view and test all endpoints.

### Run the unit tests

```
cd RaceDay.Api.Tests
dotnet test
```

### CI/CD

GitHub Actions workflow: `.github/workflows/ci.yml` (checks the `docs` folder, builds the project and runs all unit tests on every push).

![Green CI build](docs/ci-green-build.png)

### Deviations from the Part 1 plan

- SQL table names are plural (Users, Events, and so on) because USER is a reserved word in T-SQL.
- The Part 2 database is named `RaceDayApi` (Code-First migrations); the Part 1 script database is `RaceDay`. The schema is identical.
- `DELETE /api/events/{id}` returns 409 Conflict when the event already has enrolments.
- `GET /api/events` supports optional filters (`eventTypeId`, `search`, `upcomingOnly`).

### Video

YouTube (unlisted): PASTE_YOUR_LINK_HERE
