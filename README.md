# Eagles Nest

Eagles Nest is planned as a Microsoft-native web application for managing U.S. Military Vets Motorcycle Club members, chapters, dues, fines, rides, and officer-reviewed member updates.

## Application Baseline

- .NET 9 Blazor Web App
- ASP.NET Core Identity with SQL Server LocalDB for development
- Entity Framework Core migrations
- `EaglesNest.Core` for domain types
- `EaglesNest.Data` for EF Core model configuration
- `EaglesNest.Web` for the Blazor UI and Identity host
- `EaglesNest.Tests` for automated tests

## Development Seed Data

When the web app starts in Development, it applies EF Core migrations and seeds the organization hierarchy:

- `National` with abbreviation `NAT`
- All 50 states using club abbreviations, including `FLA` for Florida and `WVA` for West Virginia
- Current local chapter structure parsed from the April 2026 roster, without committing officer names, email addresses, phone numbers, or mailing addresses

Confidential roster files should stay outside the repository and be imported through dedicated local import tooling.

## Development Super Admin

Development can seed a local super-admin account from ASP.NET Core user-secrets. The secret values are not committed.

```powershell
dotnet user-secrets set "DevelopmentSuperAdmin:UserName" "<username>" --project src/EaglesNest.Web/EaglesNest.Web.csproj
dotnet user-secrets set "DevelopmentSuperAdmin:Email" "<email>" --project src/EaglesNest.Web/EaglesNest.Web.csproj
dotnet user-secrets set "DevelopmentSuperAdmin:Password" "<password>" --project src/EaglesNest.Web/EaglesNest.Web.csproj
```

The development seeder confirms the account email and assigns a `SystemAdmin` role assignment when the app starts.

## Source Control

This repository is configured for Visual Studio 2026 as the primary Git experience.

- Use `prod` for production releases.
- Use `dev` for active integrated development.
- Create feature branches from `dev`.
- Merge completed feature work back into `dev`.
- Promote `dev` to `prod` only for true releases.

The local shell does not currently expose `git` on `PATH`, so scripted commands should use the Visual Studio 2026 bundled Git executable:

```powershell
C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe
```

For convenience, use `tools\git-vs2026.cmd` from this repository root. A PowerShell helper is also included, but this machine currently blocks local `.ps1` execution by policy.
