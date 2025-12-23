# Social Network API - AI Agent Instructions

## Architecture Overview

This is a .NET 8 Web API following **Clean Architecture** with 4 layers:

- **Domain**: Core entities, enums, and base classes. No dependencies.
- **Application**: Business logic, DTOs, validations, service abstractions. Depends on Domain.
- **Persistence**: EF Core, repositories, DbContext, migrations. Depends on Domain.
- **Infrastructure**: Cross-cutting concerns (logging, rate limiting, CORS, middleware). No domain dependencies.
- **Presentation**: Controllers, API endpoints. Depends on all layers.

Each layer has `ServiceRegistration.cs` for dependency injection registration.

## Key Patterns & Conventions

### 1. Entity Design
- All entities inherit from [Domain/Common/BaseEntity.cs](Domain/Common/BaseEntity.cs)
- BaseEntity provides: `Id`, `CreatedAt`, `UpdatedAt`, `DeletedDate`, `IsDeleted`, `Recstatus`
- **Soft delete** is enforced via query filters in [ApplicationDbContext](Persistence/Context/ApplicationDbContext.cs)
- Timestamps auto-updated in `SaveChangesAsync` override

### 2. Repository Pattern & UnitOfWork
- Generic repository: [Persistence/Repositories/IRepository.cs](Persistence/Repositories/IRepository.cs)
- UnitOfWork pattern: [Persistence/UnitOfWork/IUnitOfWork.cs](Persistence/UnitOfWork/IUnitOfWork.cs) exposes entity-specific repositories
- Repositories accessed via `IUnitOfWork.Users`, `IUnitOfWork.Posts`, etc.
- Always call `await _unitOfWork.SaveChangesAsync()` to persist changes

### 3. Service Layer Structure
- Services defined in [Application/Services/Abstractions/](Application/Services/Abstractions/) (interfaces)
- Implementations in [Application/Services/Concrete/](Application/Services/Concrete/)
- Services injected via DI from `Application.ServiceRegistration.AddApplicationServices()`

### 4. DTOs & Validation
- DTOs in [Application/DTOs/](Application/DTOs/) organized by feature (Auth, User, Category)
- Use `record` types for DTOs (e.g., `LoginRequestDto`)
- **FluentValidation** for all validations in [Application/Validations/](Application/Validations/)
- Validators auto-registered in `ServiceRegistration` via `AddValidatorsFromAssembly()`
- Inject validators into controllers and validate manually before service calls

### 5. Controllers
- All controllers inherit from [AppController](Presentation/Controllers/Abstraction/AppController.cs)
- Route pattern: `[Route("api/[controller]")]`
- Use XML comments (`///`) for Swagger documentation
- Controllers should be thin—delegate business logic to services

### 6. Authentication & Authorization
- **JWT-based** auth configured in [Application/ServiceRegistration.cs](Application/ServiceRegistration.cs)
- JWT settings in `appsettings.json` under `JwtSettings`
- Refresh token versioning: `Users.RefreshTokenVersion` increments on login/logout/refresh
- Token validation checks version claim against DB value
- Use `[Authorize]` attribute; roles from [Domain/Enums/Roles.cs](Domain/Enums/Roles.cs) (`User`, `Admin`)

### 7. Error Handling & Logging
- Global exception handler: [Infrastructure/Middleware/GlobalExceptionHandler.cs](Infrastructure/Middleware/GlobalExceptionHandler.cs)
- Registered via `app.UseGlobalExceptionHandler()` in [Program.cs](Presentation/Program.cs)
- **Serilog** configured for structured logging (console + file)
- Use `ILogger<T>` for logging in services/controllers

### 8. Rate Limiting
- Configured in [Infrastructure/ServiceRegistration.cs](Infrastructure/ServiceRegistration.cs)
- IP-based rate limiting: 60 requests/minute per IP
- Applied via `.RequireRateLimiting("PerIpPolicy")` on endpoint mappings
- Policies: `Fixed`, `Sliding`, `PerIpPolicy`

### 9. Database Configuration
- **MySQL 9.4** via Entity Framework Core
- Connection string in `appsettings.json`: `"ConnectionStrings:socialnetwork"`
- Entity configurations in [Persistence/Context/Configurations/](Persistence/Context/Configurations/)
- Use `IEntityTypeConfiguration<T>` pattern for fluent API setup

## Development Workflows

### Running the Application
```bash
cd Presentation
dotnet run
# App runs on https://localhost:7xxx (see console output)
# Swagger UI available at /swagger
```

### Database Migrations
```bash
# Add migration (from solution root)
dotnet ef migrations add MigrationName --project Persistence --startup-project Presentation

# Apply migrations
dotnet ef database update --project Persistence --startup-project Presentation

# Remove last migration
dotnet ef migrations remove --project Persistence --startup-project Presentation
```

### Building the Solution
```bash
dotnet build Social_Network.sln
dotnet test  # If tests exist
```

## Code Generation Guidelines

### Adding a New Entity
1. Create entity class in [Domain/Entities/](Domain/Entities/) inheriting `BaseEntity`
2. Add `DbSet<YourEntity>` in [ApplicationDbContext](Persistence/Context/ApplicationDbContext.cs)
3. Create configuration class in [Persistence/Context/Configurations/](Persistence/Context/Configurations/) implementing `IEntityTypeConfiguration<YourEntity>`
4. Add query filter for soft delete in `ApplicationDbContext.OnModelCreating`
5. Add repository property to [IUnitOfWork](Persistence/UnitOfWork/IUnitOfWork.cs) and implementation
6. Create migration

### Adding a New Service
1. Define interface in [Application/Services/Abstractions/](Application/Services/Abstractions/)
2. Implement in [Application/Services/Concrete/](Application/Services/Concrete/)
3. Register in `Application.ServiceRegistration` using appropriate lifetime (Scoped/Singleton/Transient)

### Adding a New Controller Endpoint
1. Create DTOs in [Application/DTOs/](Application/DTOs/) using `record` types
2. Create FluentValidation validators in [Application/Validations/](Application/Validations/)
3. Add controller method with XML comments for Swagger
4. Inject validators and call `validator.ValidateAsync()` before service calls
5. Return appropriate HTTP status codes (200, 400, 401, 404, etc.)

## Critical Notes

- **Never bypass UnitOfWork**: Always use `IUnitOfWork` for data access, not `DbContext` directly
- **Soft delete is automatic**: Query filters hide deleted entities unless explicitly disabled
- **Timestamps are automatic**: `CreatedAt`/`UpdatedAt` managed by `SaveChangesAsync` override
- **Environment variables**: JWT SecretKey can be set via `JWT_SECRET_KEY` env var (overrides appsettings)
- **CORS**: Currently set to `AllowAll` policy; production should use `Production` policy with specific origins
- **Password hashing**: Use BCrypt (see `test_bcrypt.csx` for reference)

## Example: Creating a New Feature

**Task**: Add endpoint to get user profile

1. Create DTO: `Application/DTOs/User/UserProfileResponseDto.cs`
2. Add method to `IUserService`: `Task<UserProfileResponseDto> GetProfileAsync(int userId)`
3. Implement in `UserService` using `_unitOfWork.Users.GetByIdAsync()`
4. Add controller action in `UserController`:
   ```csharp
   [HttpGet("{id}")]
   [Authorize]
   public async Task<IActionResult> GetProfile(int id)
   {
       var profile = await _userService.GetProfileAsync(id);
       return Ok(profile);
   }
   ```

## Questions to Address

- Are there specific performance optimizations applied (e.g., caching, query optimization strategies)?
- Is there a testing strategy in place (unit tests, integration tests)?
- Are there any background jobs or scheduled tasks?
- Is there a specific error response format standard across all endpoints?
