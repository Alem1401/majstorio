# Majstorio

Majstorio is a Software Development II (RSII) coursework project at the Faculty of Information Technologies (FIT). The platform connects customers with tradespeople for home services through a desktop administration app and a mobile application.

## Development status

The application is under development.

- The database schema is defined through EF Core configurations and created by migrations. The API applies pending migrations and the seed data on startup.
- The API covers the core flow: registration and login, service requests, offers, accepting an offer with appointment confirmation, completing and cancelling, and the status history. It also has the administrative data: cities, service categories, users, tradesperson profiles, news posts, reviews, the dashboard and the two reports.
- Not implemented yet: RabbitMQ publishing and the worker consumer, notifications and SignalR, PayPal payment and refund, the recommender, and e-mail delivery of the password reset token.
- The Flutter desktop app is in `UI/majstorio_desktop` (administrators). The Flutter Android app is in `UI/majstorio_mobile` (customers and tradespeople), including ratings and profile editing with a photo.

## Technologies

- C# / .NET 9 and ASP.NET Core Web API.
- Entity Framework Core, Mapster, FluentValidation and SQL Server.
- JWT bearer authentication.
- RabbitMQ and a separate .NET worker (host only for now).
- Docker Compose for the API, worker, broker and database.
- Flutter / Dart for the Windows and Android applications.

## Solution structure

| Project | Purpose |
| --- | --- |
| `Majstorio.WebAPI` | REST API host: controllers, authentication and dependency registration. |
| `Majstorio.Model` | Request DTOs, response DTOs and search objects. |
| `Majstorio.Services` | Database entities, EF Core context, validators and the service layer. |
| `Majstorio.Common.Services` | Shared services (password hashing). |
| `Majstorio.Worker` | Separate worker host for the planned background message processing. |
| `Majstorio.Services.Tests` | xUnit tests. |

## Build and test

Install the .NET 9 SDK. From the repository root, run:

```powershell
dotnet build Majstorio.sln
dotnet test Majstorio.sln
```

Most tests use the EF Core in-memory provider. The tests in `ConcurrentAcceptTests` need a real SQL Server, because they check locking and unique indexes with simultaneous requests. They use SQL Server LocalDB (installed with Visual Studio), create a temporary database and drop it afterwards. To use another server, set `MAJSTORIO_TEST_SQLSERVER` to a connection string without a database name. When no server answers, those tests are reported as skipped.

## Run the API locally

The API reads its settings from a `.env` file in the repository root, found automatically when you start it from Visual Studio, Visual Studio Code or `dotnet run`. The file is ignored by Git. Create it once from the example:

```powershell
Copy-Item .env.example .env
```

Then edit `.env` for a local run. The lines that matter are the connection string and the JWT key. This connection string uses SQL Server LocalDB:

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

The first start creates the `230070` database, applies the migrations and inserts the seed data. The API then listens on `http://localhost:5100`. Open `http://localhost:5100/scalar` to view and test the endpoints. Scalar and the OpenAPI document are available only in the Development environment.

## Run with Docker Compose

Docker Desktop with Linux containers is required. Copy the example configuration:

```powershell
Copy-Item .env.example .env
```

Replace the SQL Server password, the RabbitMQ password and `Jwt__Key` in the local `.env`, then start the four services (api, worker, rabbitmq, sqlserver):

```powershell
docker compose up --build
```

Compose reads `.env` and passes it to the API and worker containers. On startup the API creates the database, applies the migrations and inserts the seed data. The API is then on `http://localhost:5100`. Every image tag is pinned in `.env.example`.

## Environment variables

All of them are listed with example values in `.env.example`.

| Variable | Used by | Purpose |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | API | SQL Server connection string. Required; the API does not start without it. |
| `Jwt__Issuer`, `Jwt__Audience` | API | Written into every token and checked on every request. |
| `Jwt__Key` | API | Secret that signs the tokens. At least 32 characters. Changing it invalidates all tokens. |
| `Jwt__ExpiryMinutes` | API | Lifetime of an access token, 1 to 1440. |
| `ASPNETCORE_ENVIRONMENT` | API | `Development` enables Scalar and returns the password reset token in the response. |
| `ASPNETCORE_URLS` / `ASPNETCORE_HTTP_PORTS` | API | Address the API listens on (local run / container). |
| `MSSQL_SA_PASSWORD`, `DATABASE_*`, `ACCEPT_EULA`, `MSSQL_PID` | Compose | SQL Server container settings; also used to build the connection string. |
| `RabbitMq__*` | Compose, worker | Broker host, port, user, password, virtual host, exchange and queue. The messaging code is not written yet. |
| `*_IMAGE`, `HOST_BIND_ADDRESS`, `*_HOST_PORT`, `API_CONTAINER_PORT` | Compose | Pinned image tags and published ports. |
| `ProfileImages__Path` | API (optional) | Folder for uploaded profile photos. Defaults to `uploads/profile-images` next to the API; in Docker it is the `profile-images` volume. |
| `PayPal__*` | API (planned) | PayPal sandbox settings; payments are not implemented yet. |
| `MAJSTORIO_TEST_SQLSERVER` | Tests (optional) | SQL Server for the concurrency tests instead of LocalDB. |

## Authentication

1. `POST /Auth/login` with a user name and password returns `accessToken`, `expiresAtUtc` and `role`.
2. Send the token on every other request in the header `Authorization: Bearer <accessToken>`.
3. The token carries the user id and the role (`Customer`, `Tradesperson` or `Administrator`). The server takes the user from the token; it never reads a user id from the request body.
4. A token stops working when it expires, after logout, after a password change or reset, when the account is deactivated, and when an administrator approves the user's tradesperson profile (the user logs in again and gets a token with the new role).

### Test accounts

Every seeded account uses the password `test`.

| Role | Username | Notes |
| --- | --- | --- |
| Administrator (desktop) | `desktop` | |
| Customer (mobile) | `mobile` | Owns seeded requests in Mostar. |
| Customer | `customer` | |
| Tradesperson | `tradesperson` | Plumbing and air conditioning in Mostar. |
| Tradesperson | `electrician` | Electrical work in Sarajevo. |
| Customer with an unapproved profile | `applicant` | Becomes a tradesperson when the administrator approves profile 3. |

## Endpoints

Lists are paged with `page` and `pageSize` (at most 100) and return `{ items, totalCount, page, pageSize }`. Errors return `{ "message": "..." }` with 400 (validation), 401 (no valid token), 403 (not allowed), 404 (not found, or not visible to the caller) or 409 (the rule or the current status does not allow it). Statuses are numbers:

- Request: 0 Created, 1 OfferAccepted, 2 AppointmentConfirmed, 3 Completed, 4 Cancelled.
- Offer: 0 Pending, 1 Accepted, 2 Rejected, 3 Withdrawn, 4 Closed.
- Appointment: 0 Pending, 1 Confirmed, 2 Cancelled, 3 Completed.

### Account (`/Auth`)

| Method | Route | Who | Purpose |
| --- | --- | --- | --- |
| `POST` | `/Auth/register` | anyone | Creates an account. The role is always Customer; a role in the body is ignored. |
| `POST` | `/Auth/login` | anyone | Returns the access token. |
| `POST` | `/Auth/logout` | signed in | Invalidates every token of the user. |
| `POST` | `/Auth/change-password` | signed in | Body `{ oldPassword, newPassword }`. The old password must be correct. Log in again afterwards. |
| `POST` | `/Auth/forgot-password` | anyone | Body `{ email }`. Issues a one-time reset token valid for 15 minutes. The answer is the same for unknown e-mails. There is no e-mail delivery, so the token is returned in `resetToken` only in the Development environment. |
| `POST` | `/Auth/reset-password` | anyone | Body `{ email, token, newPassword }`. The token works once. |

### Service requests (`/ServiceRequests`)

All require a token.

| Method | Route | Who | Purpose |
| --- | --- | --- | --- |
| `GET` | `/ServiceRequests?status=&cityId=&serviceCategoryId=&fromUtc=&toUtc=` | signed in | A customer gets their own requests. A tradesperson gets open requests in their city and category, plus the ones they made an offer on. An administrator gets all. |
| `GET` | `/ServiceRequests/{id}` | signed in | Detail with offers, appointment and status history. The customer and administrator see all offers; a tradesperson sees only their own. |
| `POST` | `/ServiceRequests` | signed in | Body `{ serviceCategoryId, cityId, description, address?, desiredStartUtc, maximumBudget, targetTradespersonProfileId? }`. Budget is in EUR. The customer comes from the token and the status starts as Created. |
| `PUT` | `/ServiceRequests/{id}` | owner | Only while the request is open and has no offers. |
| `DELETE` | `/ServiceRequests/{id}` | owner | Only while the request is open and has no offers; otherwise cancel it. |
| `POST` | `/ServiceRequests/{id}/cancel` | owner, assigned tradesperson, administrator | Body `{ reason? }`. Allowed until the request is completed. Also cancels the appointment and closes pending offers. |

### Offers (`/Offers`)

| Method | Route | Who | Purpose |
| --- | --- | --- | --- |
| `GET` | `/Offers/mine?status=&serviceRequestId=` | signed in | Offers the user sent as a tradesperson or received on their own requests. |
| `POST` | `/Offers` | Tradesperson | Body `{ serviceRequestId, price, proposedStartUtc, durationMinutes }`. One offer per tradesperson per request; the request must match the tradesperson's city and category and the price must fit the budget. |
| `PUT` | `/Offers/{id}` | Tradesperson (owner) | Changes the price and time of a pending offer. |
| `POST` | `/Offers/{id}/withdraw` | Tradesperson (owner) | Only while the offer is pending. |
| `POST` | `/Offers/{id}/accept` | owner of the request | In one transaction: accepts the offer, closes the other offers, creates the appointment (Pending, then Confirmed), moves the request to AppointmentConfirmed and writes two history rows. Returns 409 for a repeated acceptance or when the tradesperson already has an overlapping appointment. |
| `GET` | `/Offers`, `/Offers/{id}` | Administrator | Administrative overview for the desktop app. |
| `DELETE` | `/Offers/{id}` | Administrator | Deletes a pending offer without an appointment. |

### Appointments (`/Appointments`)

| Method | Route | Who | Purpose |
| --- | --- | --- | --- |
| `GET` | `/Appointments/mine?status=` | signed in | Appointments of the user's requests or of the jobs assigned to them. |
| `POST` | `/Appointments/{id}/complete` | Tradesperson (assigned) | Marks the job as completed, not before the appointment starts. |
| `GET` | `/Appointments`, `/Appointments/{id}` | Administrator | Administrative overview for the desktop app. |

Appointments are not created, edited or deleted directly: they are created by accepting an offer and cancelled by cancelling the request.

### Tradesperson profiles (`/TradespersonProfiles`)

All require a token.

| Method | Route | Who | Purpose |
| --- | --- | --- | --- |
| `GET` | `/TradespersonProfiles?name=&cityId=&serviceCategoryId=&minimumRating=` | signed in | Approved, active profiles with rating, number of jobs, price range and categories, best rated first. Administrators also see unapproved and inactive profiles and can filter with `isApproved` and `isActive`. |
| `GET` | `/TradespersonProfiles/{id}` | signed in | Detail with services, prices and the latest reviews. |
| `POST` | `/TradespersonProfiles/{id}/approve` | Administrator | Approves the profile and gives its user the Tradesperson role. |
| `PUT` | `/TradespersonProfiles/{id}/active` | Administrator | Body `{ isActive }`. An inactive profile is hidden and cannot send offers. |

### Own account and ratings (mobile)

All require a token. Who the user is always comes from the token.

| Method | Route | Who | Purpose |
| --- | --- | --- | --- |
| `GET` | `/Account` | signed in | The user's own details and photo address. |
| `PUT` | `/Account` | signed in | Body `{ email, phoneNumber?, firstName, lastName }`. The user name and role cannot be changed here. |
| `POST` | `/Account/photo` | signed in | `multipart/form-data` with one `file`: a JPEG or PNG of at most 2 MB, recognised by its content. Replaces the previous photo. |
| `DELETE` | `/Account/photo` | signed in | Removes the photo. |
| `GET` | `/Account/photos/{fileName}` | signed in | The photo file. Not available without a token. |
| `GET` | `/Reviews/mine?appointmentId=&rating=` | Customer | The customer's own ratings. |
| `POST` | `/Reviews/mine` | Customer | Body `{ appointmentId, rating (1-5), comment? }`. Only for a completed job on the customer's own request, once per job. |

### Administrative data (desktop)

`/Cities`, `/ServiceCategories`, `/NewsPost`, `/Users` and `/Reviews` each have a paged list, get by id, update and delete (`/Users` has no create; accounts are registered). `/Dashboard` returns the numbers of the home screen, and `/Reports/requests` and `/Reports/tradespeople` return the data of the two PDF reports.

Every one of them requires a token. `/Users`, `/Reviews`, `/Dashboard` and `/Reports` are for administrators only. `/Cities`, `/ServiceCategories` and `/NewsPost` can be read by any signed-in user and changed only by administrators. Only the `/Auth` endpoints for register, login, forgot and reset password work without a token.

## Desktop app

```powershell
cd UI/majstorio_desktop
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000
```

`API_BASE_URL` defaults to `http://localhost:5000`, the address the API uses when started from Visual Studio. Pass `http://localhost:5100` when the API runs with `dotnet run` (which follows `ASPNETCORE_URLS` in `.env`) or in Docker. The app starts on a login screen and accepts only administrator accounts (`desktop` / `test`). The token is kept in memory, sent with every request, and dropped on "Sign out" or when the API answers 401.

## Mobile app

```powershell
cd UI/majstorio_mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5100
```

`10.0.2.2` is how the Android emulator reaches the computer it runs on, so the API must be running on port 5100 (`dotnet run` or Docker). Sign in with `mobile` / `test` (customer) or `tradesperson` / `test` (tradesperson); an administrator is directed to the desktop app.

Release build for the emulator:

```powershell
cd UI/majstorio_mobile
flutter clean
flutter pub get
flutter build apk --release --dart-define=API_BASE_URL=http://10.0.2.2:5100
adb install -r build/app/outputs/flutter-apk/app-release.apk
```

## How a status changes

Every change of a request or appointment status goes through `StatusTransitions` (`Majstorio.Services/RequestStateMachine`). It allows only these moves and writes one `StatusHistory` row for each (who, when, from, to):

- Request: Created → OfferAccepted → AppointmentConfirmed → Completed; Cancelled from any of the first three.
- Appointment: Pending → Confirmed → Completed; Cancelled from Pending or Confirmed.

Accepting, withdrawing, cancelling and completing first lock the request row inside a transaction, so two simultaneous actions on the same request run one after another. Accepting also locks the tradesperson row, so two overlapping appointments for one tradesperson cannot both be created. Unique indexes are the second line of defence: one offer per tradesperson per request, one accepted offer per request, one appointment per offer.

## Configuration and repository contents

- `.env.example` contains example configuration and is committed.
- Local `.env` files, credentials, generated builds and IDE output are excluded by `.gitignore`.
- Keep real passwords, API keys and tokens out of source code and Git history.

## Coursework submission

The final public repository will contain the complete source code. Android and Windows builds will be attached to an immutable GitHub Release as a single ZIP containing the APK and the complete Windows release folder.

Sensitive configuration must follow the course's encrypted ZIP procedure; an unencrypted `.env` must not be included in the release. The exact release link and the configuration archive password are submitted through DLWMS. Recommendation documentation will be added when that feature is implemented.
