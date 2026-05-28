# SimPle — Backend

ASP.NET Core 8 backend for the SimPle multiplayer game platform.

## Stack

- **ASP.NET Core 8** — Web API
- **PostgreSQL** + **EF Core 8** — database and migrations
- **Argon2id** — password hashing
- **JWT** (HS256, 15-min access) + rotating refresh tokens (SHA-256 hashed, 7-day)
- **Google reCAPTCHA v2** — server-side verification
- **Google Identity Services** — OAuth ID token flow
- **MailKit** — SMTP email delivery
- **FluentValidation** — request validation
- **xUnit** + **NSubstitute** — unit and integration tests

## Modules

| Module | Status |
|---|---|
| Authentication & Sessions | Complete |
| User profile | Planned |
| Friends & social graph | Planned |
| Lobby & matchmaking | Planned |

## Getting started

1. Copy `.env` from the example and fill in credentials:
   ```
   cp src/SimPle.Api/.env.example src/SimPle.Api/.env
   ```
2. Or run the setup script (creates a random-key `.env` automatically):
   ```powershell
   .\scripts\Initialize-AuthEnvironment.ps1
   ```
3. Start PostgreSQL:
   ```
   docker compose -f compose.auth.yml up -d
   ```
4. Apply migrations:
   ```
   dotnet ef database update --project src/SimPle.Infrastructure --startup-project src/SimPle.Api
   ```
5. Run the API:
   ```
   dotnet run --project src/SimPle.Api
   ```
   Swagger is available at `https://localhost:5147/swagger`.

## Running tests

```
dotnet test
```

145 tests total: 100 unit tests, 45 integration tests.

## CI

GitHub Actions runs on every push to `main` and `feature/**`:
build → unit tests → integration tests → NuGet vulnerability scan.
