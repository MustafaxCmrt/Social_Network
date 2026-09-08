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

## GitHub Copilot IDE Comparison for .NET Development

### GitHub Copilot in VS Code vs JetBrains Rider

Both IDEs offer excellent GitHub Copilot integration for .NET development, but they have different strengths:

#### **Visual Studio Code + GitHub Copilot**

**Strengths:**
- **Faster startup and lighter resource usage** - ideal for quick edits and smaller projects
- **More frequent Copilot updates** - Microsoft's tight integration means latest features arrive first
- **Better inline suggestions** - smoother, more responsive code completions
- **Copilot Chat integration** - native chat experience with context awareness
- **Cross-platform consistency** - identical experience on Windows, macOS, and Linux
- **Extensive extension ecosystem** - complementary tools for .NET development

**Best for:**
- Rapid prototyping and quick iterations
- Projects where Copilot suggestions are primary workflow
- Teams using mixed tech stacks beyond .NET
- Developers who prefer lightweight, customizable environments
- Working with multiple programming languages

**Recommended Extensions for .NET:**
```
- C# Dev Kit
- GitHub Copilot
- GitHub Copilot Chat
- .NET Install Tool
```

#### **JetBrains Rider + GitHub Copilot**

**Strengths:**
- **Superior .NET-specific refactoring** - best-in-class code analysis and transformations
- **Better code navigation** - find usages, type hierarchy, call chains more efficiently
- **Integrated debugging experience** - advanced debugging features for complex scenarios
- **Built-in database tools** - MySQL/SQL Server integration without extra extensions
- **Better for large solutions** - handles multi-project .NET solutions more efficiently
- **ReSharper integration** - combines Copilot with ReSharper's powerful inspections

**Best for:**
- Large, complex .NET solutions with multiple projects
- Enterprise applications requiring advanced debugging
- Developers who rely heavily on refactoring tools
- Teams already using JetBrains tools
- Projects with heavy database interaction

**Configuration Tips:**
- Enable "GitHub Copilot" plugin from JetBrains Marketplace
- Configure Copilot to work with ReSharper's code style
- Use Copilot for boilerplate, ReSharper for refactoring

### **Recommendation for This Project**

For the **Social Network API** project, both IDEs work excellently. Choose based on your workflow:

**Choose VS Code if:**
- You want faster, more responsive Copilot suggestions
- You prefer lightweight development environment
- You frequently use Copilot Chat for architectural questions
- You're working on smaller feature additions

**Choose Rider if:**
- You're working with complex multi-layer refactoring
- You need advanced debugging for EF Core queries
- You prefer integrated database management tools
- You're navigating large codebases frequently

### **Best Practices for GitHub Copilot with .NET**

Regardless of IDE choice:

1. **Use descriptive comments** before code blocks to guide Copilot
   ```csharp
   // Create a service method to get user profile with posts and followers count
   public async Task<UserProfileDto> GetUserProfileAsync(int userId)
   ```

2. **Leverage XML documentation** - Copilot uses these for context
   ```csharp
   /// <summary>
   /// Retrieves user profile with aggregated social metrics
   /// </summary>
   /// <param name="userId">The unique identifier of the user</param>
   /// <returns>Complete user profile with posts and follower statistics</returns>
   ```

3. **Follow project patterns** - Copilot learns from your codebase structure
   - Maintain consistent DTO naming (`*RequestDto`, `*ResponseDto`)
   - Follow service/repository patterns already established
   - Use FluentValidation patterns consistently

4. **Context is key** - Keep related files open to improve suggestions
   - Open interface and implementation side-by-side
   - Have DTOs visible when working on controllers
   - Keep entity classes open when writing EF configurations

5. **Verify Copilot suggestions** against project standards:
   - Check that soft delete patterns are followed
   - Ensure UnitOfWork is used, not DbContext directly
   - Validate that proper error handling is included
   - Confirm authentication/authorization is applied

### **Performance Tips**

- **VS Code**: Disable unused extensions to keep Copilot responsive
- **Rider**: Allocate sufficient heap memory (Settings → Memory Settings)
- **Both**: Exclude `bin/`, `obj/`, `node_modules/` from indexing for better performance

## Questions to Address

- Are there specific performance optimizations applied (e.g., caching, query optimization strategies)?
- Is there a testing strategy in place (unit tests, integration tests)?
- Are there any background jobs or scheduled tasks?
- Is there a specific error response format standard across all endpoints?
