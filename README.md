# Majstorio

Majstorio is a Software Development II (RSII) coursework project at the Faculty of Information Technologies (FIT). The planned platform connects customers with tradespeople for home services through desktop administration and a mobile application.

## Development status

The repository currently contains the initial .NET solution, database entity classes, selected request/response DTOs, a generic read service and Docker infrastructure definitions. The application is under development.

The database context is a starting point. Database mappings, migrations, seed data, concrete services, API endpoints, authentication and business workflows still need to be implemented. The worker host exists; RabbitMQ publishing and message processing are planned. The Flutter desktop and mobile projects will be created from scratch under `UI/majstorio_desktop` and `UI/majstorio_mobile`.

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
| `Majstorio.WebAPI` | REST API host; endpoints and dependency registration will be added during development. |
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

The API host can be started with:

```powershell
dotnet run --project Majstorio.WebAPI
```

There are currently no implemented business endpoints or test user accounts. The API and worker are not yet connected to the service layer, database or RabbitMQ. A plain `dotnet run` does not load `.env` automatically.

## Docker infrastructure scaffold

Docker Desktop with Linux containers is required for this setup. Copy the example configuration from the repository root:

```powershell
Copy-Item .env.example .env
```

Replace the SQL Server and RabbitMQ password placeholders in the local `.env`, then start the defined services:

```powershell
docker compose up --build
```

Compose reads `.env` and passes it to the API and worker containers. Starting the containers does not yet create the application database/schema, seed users or implement messaging. Those steps will be documented as the corresponding functionality is completed.

## Configuration and repository contents

- `.env.example` contains example configuration and is committed.
- Local `.env` files, credentials, generated builds and IDE output are excluded by `.gitignore`.
- Keep real passwords, API keys and tokens out of source code and Git history.
- Keep this README aligned with the implemented setup. Add complete application startup steps and working test accounts before submission.

## Coursework submission

The final public repository will contain the complete source code. Android and Windows builds will be attached to an immutable GitHub Release as a single ZIP containing the APK and the complete Windows release folder.

Sensitive configuration must follow the course's encrypted ZIP procedure; an unencrypted `.env` must not be included in the release. The exact release link and the configuration archive password are submitted through DLWMS. Recommendation documentation and test login credentials will be added when those features are implemented.
