# Social_Network API

> TR/EN friendly documentation for a Clean Architecture based social platform backend.

## 1) Overview / Genel Bakış

**Social_Network** is a **C# .NET 8 ASP.NET Core Web API** solution built with **Clean Architecture** principles.
It is designed as a community/social backend with authentication, forum-like discussions, moderation, clubs, notifications, and auditability.

- Solution: `Social_Network.sln`
- Executable API project: `Presentation`
- Main style: layered architecture + repository/unit of work

---

## 2) Key Features / Temel Özellikler

- Authentication: register, login, refresh token, logout
- Account security: email verification, resend verification, forgot/reset password
- User management and profile image upload
- Categories and discussion threads
- Posts, nested replies, post votes, accepted-solution marking
- Search endpoints (threads, posts, users, combined)
- Notifications
- Reports and moderation workflows
- Moderation actions: user ban/mute, thread lock/unlock, moderation search
- Audit logs and dashboard-oriented queries
- Clubs: club requests, club creation, memberships, role management, application status, club image upload

---

## 3) Architecture / Mimari

The repository follows layered **Clean Architecture**:

- **Domain**: core entities, enums, shared base types
- **Application**: DTOs, validators, service abstractions/implementations, JWT and business orchestration
- **Persistence**: EF Core DbContext, migrations, repositories, Unit of Work
- **Infrastructure**: middleware and cross-cutting concerns (CORS, rate limiting, exception handling)
- **Presentation**: ASP.NET Core API host, controllers, Swagger setup, middleware pipeline

### Request flow (simplified)

`HTTP Request -> Presentation (Controllers) -> Application (Services/Validation) -> Persistence (UoW/Repository/EF Core) -> MySQL`

---

## 4) Project Structure / Proje Yapısı

```text
Social_Network/
├─ Domain/
│  ├─ Common/
│  ├─ Entities/
│  ├─ Enums/
│  └─ Services/
├─ Application/
│  ├─ DTOs/
│  ├─ Models/
│  ├─ Services/
│  │  ├─ Abstractions/
│  │  └─ Concrete/
│  └─ Validations/
├─ Persistence/
│  ├─ Context/
│  ├─ Migrations/
│  ├─ Repositories/
│  └─ UnitOfWork/
├─ Infrastructure/
│  ├─ Extensions/
│  └─ Middleware/
├─ Presentation/
│  ├─ Controllers/
│  └─ Program.cs
└─ Social_Network.sln
```

---

## 5) Technology Stack / Teknoloji Yığını

- **Runtime/Framework:** .NET 8, ASP.NET Core Web API
- **Language:** C#
- **ORM:** Entity Framework Core 8
- **Database Provider:** Pomelo MySQL (`Pomelo.EntityFrameworkCore.MySql`)
- **Authentication:** JWT access/refresh token flow
- **Validation:** FluentValidation (`FluentValidation.DependencyInjectionExtensions`)
- **Password Hashing:** BCrypt (`BCrypt.Net-Next`)
- **API Docs:** Swagger (`Swashbuckle.AspNetCore`)
- **Logging:** Serilog

### Relevant NuGet packages

- `FluentValidation.DependencyInjectionExtensions`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `BCrypt.Net-Next`
- `Microsoft.EntityFrameworkCore`
- `Pomelo.EntityFrameworkCore.MySql`
- `Swashbuckle.AspNetCore`
- `Serilog.AspNetCore`

---

## 6) Domain Model / Alan Modeli

Main entities in the codebase:

- `Users`
- `Categories`
- `Threads`
- `Posts`
- `PostVotes`
- `Notifications`
- `Reports`
- `UserBans`
- `UserMutes`
- `PasswordResetTokens`
- `AuditLogs`
- `Clubs`
- `ClubRequests`
- `ClubMemberships`

All key entities derive from `Domain/Common/BaseEntity.cs` and include audit/soft-delete related metadata such as:
`CreatedAt`, `UpdatedAt`, `DeletedDate`, `CreatedUserId`, `UpdatedUserId`, `DeletedUserId`, `IsDeleted`, `Recstatus`.

---

## 7) Authentication & Authorization / Kimlik Doğrulama ve Yetkilendirme

- JWT authentication is configured in `Application/ServiceRegistration.cs`.
- API uses claims-based authentication with role checks (`User`, `Moderator`, `Admin`).
- Token-version logic is enforced by `Infrastructure/Middleware/TokenVersionValidationMiddleware.cs`.
- Token-version mismatch can invalidate previously issued tokens for security-sensitive flows.

---

## 8) Getting Started / Başlangıç

### Prerequisites

- .NET 8 SDK
- MySQL server
- (Optional) EF CLI: `dotnet tool install --global dotnet-ef`

### Clone + Restore

```bash
git clone https://github.com/MustafaxCmrt/Social_Network.git
cd Social_Network
dotnet restore
```

### Build

```bash
dotnet build Social_Network.sln
```

---

## 9) Configuration / Yapılandırma

### Database connection (required)

`Persistence/ServiceRegistration.cs` reads:

- `ConnectionStrings:socialnetwork`

You **must** configure a MySQL connection string for that exact key.

Example (replace with your own values):

```json
{
  "ConnectionStrings": {
    "socialnetwork": "YOUR_MYSQL_CONNECTION_STRING"
  }
}
```

### JWT settings

`Application/ServiceRegistration.cs` reads `JwtSettings` and supports environment fallback:

- If `JwtSettings:SecretKey` is empty, it falls back to `JWT_SECRET_KEY` environment variable.

Example:

```bash
export JWT_SECRET_KEY="replace-with-a-long-random-secret"
```

> Security note: Replace development JWT secrets before production use. Keep production DB/JWT credentials out of source control (use env vars, secret managers, or secure host settings).

---

## 10) Database Migrations / Veritabanı Migrasyonları

Run from repository root:

```bash
# Add migration
dotnet ef migrations add MigrationName --project Persistence --startup-project Presentation

# Apply migration
dotnet ef database update --project Persistence --startup-project Presentation

# Remove last migration (if needed)
dotnet ef migrations remove --project Persistence --startup-project Presentation
```

---

## 11) Running the API / API'yi Çalıştırma

```bash
dotnet run --project Presentation
```

`Presentation` is the executable host project.

---

## 12) Swagger / OpenAPI

Swagger is enabled in **Development** environment (`Program.cs`).

After startup, use:

- `https://localhost:<port>/swagger`

---

## 13) API Areas & Controllers / API Alanları ve Controller'lar

Current controller set in `Presentation/Controllers`:

- `AuthController`
- `UserController`
- `CategoryController`
- `ThreadController`
- `PostController`
- `SearchController`
- `NotificationController`
- `ReportController`
- `ModerationController`
- `AuditLogController`
- `DashboardController`
- `ClubController`

All inherit from `AppController` and use route base:

- `api/[controller]`

---

## 14) Middleware, CORS, Rate Limiting

From `Presentation/Program.cs` and `Infrastructure`:

- Global exception handling middleware
- Serilog request logging
- HTTPS redirection
- Static files middleware (file serving)
- CORS (`AllowAll` policy configured; `Production` policy example exists)
- ASP.NET Core rate limiting (including per-IP policy)
- Authentication + token-version validation middleware + authorization

---

## 15) Logging & Error Handling / Loglama ve Hata Yönetimi

- Serilog is integrated via `builder.Host.UseSerilog()`.
- Request logging uses `app.UseSerilogRequestLogging()`.
- Unhandled exceptions are handled centrally by `GlobalExceptionHandler` middleware.

---

## 16) Contribution Guidance / Katkı Rehberi

1. Fork the repository
2. Create a feature branch from `main`
3. Make focused, reviewable changes
4. Run restore/build and required DB migration checks
5. Open a pull request with clear summary and rationale

Suggested local checks:

```bash
dotnet restore
dotnet build Social_Network.sln
```

---

## 17) Roadmap / Gelecek Planı

Potential future enhancements:

- Automated tests (unit/integration)
- CI/CD pipelines
- Docker/containerization
- Deployment automation and environment templates
- Optional frontend clients

> Note: The above are roadmap ideas; they are not claimed as currently implemented here.

---

## 18) License Status

No explicit license file is currently present in this repository.
If you plan to distribute or reuse publicly, add a proper `LICENSE` file.

---

## 19) Author

- GitHub: **[@MustafaxCmrt](https://github.com/MustafaxCmrt)**
