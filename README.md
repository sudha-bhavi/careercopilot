# JobPilot

A small job-application tracker with a resume-match helper. Track every application through
Saved, Applied, Interview, Offer and Rejected, get nudged when a follow-up is due, and paste a
job description next to your resume to see which skills match and which are missing.

Built as a portfolio project and used in my own job search.

## Features

- Applications: company, role, link, status, applied date, follow-up date and notes
- Dashboard with a count per status and the number of follow-ups due
- Follow-up reminders: applications still in play whose follow-up date has arrived are highlighted
- Resume match: matched skills, missing skills and a score from a pasted resume and job description
- Single-user sign-in with a JWT

## Stack

| Layer | Technology |
| --- | --- |
| API | .NET 10 minimal API, EF Core, Npgsql |
| Database | PostgreSQL 17 in Docker Compose |
| Web | React 19, TypeScript, Vite |
| Tests | xUnit with `WebApplicationFactory` (in-memory database) |
| CI | GitHub Actions: build and test the API, type-check and build the web app |

## Run it locally

You need the .NET 10 SDK, Docker and Node 22.

1. Copy `.env.example` to `.env` and set `POSTGRES_PASSWORD` to a password you choose.
2. Start the database:

   ```bash
   docker compose up -d
   ```

3. Give the API its secrets (user-secrets keeps them out of git). Replace every `<...>`:

   ```bash
   dotnet user-secrets set "ConnectionStrings:JobPilot" "Host=localhost;Port=5433;Database=jobpilot;Username=jobpilot;Password=<postgres-password>" --project src/JobPilot.Api
   dotnet user-secrets set "Auth:Username" "<your-username>" --project src/JobPilot.Api
   dotnet user-secrets set "Auth:Password" "<your-password>" --project src/JobPilot.Api
   dotnet user-secrets set "Auth:JwtKey" "<random string, at least 32 characters>" --project src/JobPilot.Api
   ```

4. Run the API (it applies database migrations on startup) at http://localhost:5090:

   ```bash
   dotnet run --project src/JobPilot.Api
   ```

5. In a second terminal, run the web app at http://localhost:5173:

   ```bash
   cd web && npm install && npm run dev
   ```

Run the tests with `dotnet test`. They need no database and no secrets.

## API

| Method and path | Purpose |
| --- | --- |
| `POST /api/auth/login` | Exchange the username and password for a token |
| `GET /api/applications?status=` | List applications, optionally by status |
| `GET /api/applications/follow-ups` | Applications whose follow-up is due |
| `GET /api/applications/{id}` | One application |
| `POST /api/applications` | Create |
| `PUT /api/applications/{id}` | Update |
| `DELETE /api/applications/{id}` | Delete |
| `GET /api/dashboard` | Counts per status and follow-ups due |
| `POST /api/resume/match` | Match a resume against a job description |
| `GET /health`, `GET /health/ready` | Liveness and readiness |

Everything except login and health requires `Authorization: Bearer <token>`.

## Design decisions

- **Resume matching sits behind `IResumeMatcher`.** Today it is a deterministic keyword matcher:
  free, instant and easy to test. An LLM-backed matcher can replace it by changing one line in
  `Program.cs`, with no endpoint or UI changes.
- **Liveness and readiness are separate.** `/health` never touches the database, so a database
  outage does not make an orchestrator restart a healthy app. `/health/ready` checks it.
- **Auth settings are validated on startup.** A missing secret fails the boot with a clear
  message instead of surfacing as a confusing login error later.
- **Login is rate limited and compared in constant time** to make password guessing impractical.
- **Only http(s) links are accepted.** A `javascript:` link stored in the database would become
  an XSS vector once rendered as an `<a href>`.
- **Request and response records are separate from the EF entity**, so clients cannot set
  server-owned fields and the database model can change freely.
- **Dev uses a Vite proxy** for `/api`, so the browser sees one origin and no CORS setup is needed.
- **Trade-off:** the token is kept in `localStorage`, so it survives a refresh but would be
  readable by an XSS bug. For a single-user app this is acceptable; a multi-user product would
  use an HttpOnly cookie.

## Project layout

```
src/JobPilot.Api/      API, EF Core model and migrations
tests/JobPilot.Api.Tests/   integration and unit tests
web/                   React + TypeScript app
compose.yaml           PostgreSQL for local development
```

## License

MIT, see [LICENSE](LICENSE).
