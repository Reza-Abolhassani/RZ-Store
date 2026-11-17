# BaseProject .NET Template

BaseProject is a layered ASP.NET Core template for building API and admin applications with Identity, EF Core, JWT authentication, Swagger, Serilog, ELMAH, and SQL Server.

## Requirements

- .NET 10 SDK
- SQL Server or SQL Server-compatible development container
- Optional: Docker for containerized builds

## Project Structure

```text
Api/           Public HTTP API and authentication endpoints
Admin/         MVC admin panel
Common/        Shared settings, utilities, exceptions, and provider abstractions
Data/          EF Core DbContext, repositories, and entity configurations
Entities/      Domain entities and Identity user/role types
Services/      Application services, JWT issuing, and data initialization
WebFramework/  ASP.NET Core setup, filters, middleware, Swagger, and DI helpers
```

## Configuration and Secrets

`appsettings.json` contains safe local defaults and placeholders only. Production deployments must override secrets through environment variables, Kubernetes secrets, a vault, or another secret provider.

Important keys:

- `ConnectionStrings__SqlServer`
- `ConnectionStrings__Elmah`
- `SiteSettings__JwtSettings__SecretKey`
- `SiteSettings__JwtSettings__EncryptKey` (must be exactly 16 UTF-8 bytes)
- `SiteSettings__CorsSettings__AllowedOrigins__0`
- `SiteSettings__SmsSettings__ApiToken`

The API validates JWT settings at startup and rejects `CHANGE_ME` JWT values when production HTTPS metadata is enabled.

## Database

Development can auto-apply migrations via:

```json
"SiteSettings": {
  "DatabaseSettings": {
    "AutoMigrate": true
  }
}
```

For production, keep `AutoMigrate` disabled and run migrations as a deployment step:

```bash
dotnet ef database update -p Data -s Api
```

## Running Locally

```bash
dotnet restore BaseProject.sln
dotnet build BaseProject.sln
dotnet run --project Api/API.csproj
```

Swagger is available at `/swagger` when the API is running.

## Security Defaults

- API controllers require authorization by default.
- Public actions must explicitly use `[AllowAnonymous]`.
- CORS denies origins unless configured.
- Admin MVC uses anti-forgery validation.
- ELMAH is restricted to authenticated Admin users.
- Demo users are not seeded unless explicitly enabled.
- OTP values are stored as hashes and are not returned from API responses.

## Health Checks

The API exposes:

- `/health/live`
- `/health/ready`

## Docker

```bash
docker build -t baseproject-api -f dockerfile .
docker run --rm -p 8080:8080 baseproject-api
```

## Recommended Production Checklist

- Configure real JWT signing/encryption keys.
- Configure allowed CORS origins.
- Configure SMS provider token.
- Disable automatic database migration.
- Run migrations before deployment.
- Set up centralized logging, metrics, and tracing.
- Add project-specific tests before building business features.
