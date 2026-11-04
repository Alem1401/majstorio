# Majstorio

Majstorio is a Software Development II (RSII) coursework project at the Faculty of Information Technologies (FIT). The planned platform connects customers with tradespeople for home services through desktop administration and a mobile application.

## Development status

The application is under development.

- The database schema is defined through EF Core configurations and created by migrations. The API applies pending migrations and the seed data on startup.
- The service layer covers cities, service categories, users, news posts, offers, appointments, reviews and authentication.
- The only exposed API endpoints so far are the city endpoints listed below.
- The worker host exists; RabbitMQ publishing and message processing are planned.
- The Flutter desktop app is in `UI/majstorio_desktop`; the mobile app is planned under `UI/majstorio_mobile`.

## Planned scope

- Customer and tradesperson accounts and profiles.
- Service categories, service requests and offers.
- Appointments and status tracking.
- Sandbox payments, refunds and reviews.
- Notifications, news and recommendations.
- Desktop administration and PDF reports.

## Technologies

- C# / .NET 9 and ASP.NET Core Web API.
- Entity Framework Core, Mapster and SQL Server.
- RabbitMQ and a separate .NET worker.
- Docker Compose for the API, worker, broker and database.
- Flutter / Dart for the planned Windows and Android applications.

## Solution structure

| Project | Purpose |
| --- | --- |
| `Majstorio.WebAPI` | REST API host: controllers, authentication and dependency registration. |
| `Majstorio.Model` | Request DTOs, response DTOs and search objects. |
| `Majstorio.Services` | Database entities, EF Core context and service layer. |
| `Majstorio.Common.Services` | Project reserved for shared service functionality. |
| `Majstorio.Worker` | Separate worker host for planned background message processing. |

## Build the .NET solution

Install the .NET 9 SDK. From the repository root, run:

```powershell
dotnet restore Majstorio.sln
dotnet build Majstorio.sln
```

Open `Majstorio.sln` in Visual Studio, or open the repository folder in Visual Studio Code.

## Run the API locally

The API reads its settings from a `.env` file in the repository root, found automatically when you start it from Visual Studio, Visual Studio Code or `dotnet run`. The file is ignored by Git. Create it once from the example:

```powershell
Copy-Item .env.example .env
```

Then edit `.env` for a local run. The two lines that matter are the connection string and the JWT key. This connection string uses SQL Server LocalDB, which is installed with Visual Studio:

```
ConnectionStrings__DefaultConnection='Server=(localdb)\MSSQLLocalDB;Database=230070;Trusted_Connection=True;TrustServerCertificate=True'
Jwt__Key=<a random value of at least 32 characters>
ASPNETCORE_URLS=http://localhost:5100
```

A random key can be generated with:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

Start the API:

```powershell
dotnet run --project Majstorio.WebAPI
```

If the connection string is missing, the API stops at startup and says so. Values already set in the environment take priority over `.env`.

The first start creates the `230070` database, applies the migrations and inserts the seed data. The API then listens on `http://localhost:5100`. Open `http://localhost:5100/scalar` to view and test the endpoints. The Visual Studio launch profile opens Scalar automatically. Scalar and the OpenAPI document are available in the Development environment.

### City endpoints

The city endpoints are temporarily open without a token while the desktop login is being built. They must require the administrator role before submission.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/Cities?page=1&pageSize=10&name=mo&isActive=true` | Paged list; `name` and `isActive` are optional filters; `pageSize` is at most 100 |
| `GET` | `/Cities/{id}` | One city |
| `POST` | `/Cities` | Create; body `{ "name": "Travnik" }` |
| `PUT` | `/Cities/{id}` | Update; body `{ "name": "Travnik", "isActive": false }` |
| `DELETE` | `/Cities/{id}` | Delete; a city that is still used is rejected with 409 and must be deactivated instead |

### Test accounts

Every seeded account uses the password `test`. The login endpoint is not exposed yet.

| Context | Username | Password |
| --- | --- | --- |
| Desktop (administrator) | `desktop` | `test` |
| Mobile (customer) | `mobile` | `test` |
| Tradesperson | `tradesperson` | `test` |

## Docker infrastructure scaffold

Docker Desktop with Linux containers is required for this setup. Copy the example configuration from the repository root:

```powershell
Copy-Item .env.example .env
```

Replace the SQL Server and RabbitMQ password placeholders in the local `.env`, then start the defined services:

```powershell
docker compose up --build
```

Compose reads `.env` and passes it to the API and worker containers. Set `Jwt__Key` to a random value of at least 32 characters; without it every API request fails. On startup the API creates the database, applies the migrations and inserts the seed data. Messaging is not implemented yet.

## Configuration and repository contents

- `.env.example` contains example configuration and is committed.
- Local `.env` files, credentials, generated builds and IDE output are excluded by `.gitignore`.
- Keep real passwords, API keys and tokens out of source code and Git history.
- Keep this README aligned with the implemented setup. Add complete application startup steps and working test accounts before submission.

## Coursework submission

The final public repository will contain the complete source code. Android and Windows builds will be attached to an immutable GitHub Release as a single ZIP containing the APK and the complete Windows release folder.

Sensitive configuration must follow the course's encrypted ZIP procedure; an unencrypted `.env` must not be included in the release. The exact release link and the configuration archive password are submitted through DLWMS. Recommendation documentation will be added when that feature is implemented.
