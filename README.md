# Eagles Nest

Eagles Nest is the member-operations application for the U. S. Military Vets Motorcycle Club. The foundation release provides approved-access accounts, role-and-permission security, append-only auditing, and a responsive Blazor UI. Member, activity, and dues workflows are represented by placeholders for future milestones.

## Technology and architecture

- .NET 10 Blazor Web App with global Interactive Server rendering
- ASP.NET Core Identity with SQL Server-backed users and roles
- Entity Framework Core 10 with versioned migrations
- Modular monolith split into Domain, Application, Infrastructure, and Web projects
- xUnit unit and SQL Server integration tests
- GitHub Actions CI for `Dev`, `Test`, and `Prod`

The projects have one-way dependencies: Domain has no infrastructure dependencies; Application describes use cases and interfaces; Infrastructure implements Identity, persistence, authorization, audit, and email services; Web contains the Blazor experience and composition root.

## Local prerequisites

- Visual Studio Community 2026 with the ASP.NET and web development workload
- .NET 10 SDK (the repository currently pins 10.0.401 with latest-patch roll-forward)
- SQL Server 2022 or later. The initial local target is the default `localhost` instance.
- Git

## First-time setup

From the repository root:

```powershell
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=EaglesNest_Dev;Trusted_Connection=True;TrustServerCertificate=True" --project src/EaglesNest.Web
dotnet user-secrets set "BootstrapAdmin:UserName" "your-admin-name" --project src/EaglesNest.Web
dotnet user-secrets set "BootstrapAdmin:Email" "your-address@example.com" --project src/EaglesNest.Web
dotnet user-secrets set "BootstrapAdmin:Password" "choose-a-long-bootstrap-password" --project src/EaglesNest.Web
dotnet user-secrets set "BootstrapAdmin:MustChangePassword" "true" --project src/EaglesNest.Web
dotnet tool run dotnet-ef database update --project src/EaglesNest.Infrastructure --startup-project src/EaglesNest.Web
dotnet run --project src/EaglesNest.Web
```

The application creates the first Admin only when no active Admin exists. `BootstrapAdmin:MustChangePassword` defaults to `true` when omitted. Set it to `false` only when the bootstrap password is intentionally retained after the first sign-in. After the first successful startup, remove the bootstrap values:

```powershell
dotnet user-secrets remove "BootstrapAdmin:UserName" --project src/EaglesNest.Web
dotnet user-secrets remove "BootstrapAdmin:Email" --project src/EaglesNest.Web
dotnet user-secrets remove "BootstrapAdmin:Password" --project src/EaglesNest.Web
dotnet user-secrets remove "BootstrapAdmin:MustChangePassword" --project src/EaglesNest.Web
```

The connection string and bootstrap credentials are never stored in tracked configuration, command scripts, migrations, logs, or audit details. Use protected Azure App Service settings or another deployment secret provider for Test and Prod, then remove or disable the bootstrap settings after each environment creates its first Admin. Reusing a retained bootstrap password across environments increases the impact of credential exposure. Visual Studio can launch `EaglesNest.Web` directly after the secrets are configured.

## Account workflow

1. A visitor requests access with a username, email, and password.
2. The account remains `Pending` and cannot authenticate.
3. An Admin approves or rejects it from **Administration → User accounts**.
4. Approved users can sign in. Five failed password attempts cause a 15-minute automatic lockout.
5. Admins may lock, unlock, soft-delete, issue reset links, and assign the Admin role. Self-lock/self-delete and disabling the final active Admin are blocked.

Admin-created users bypass approval and receive a set-password link. Password reset links expire after two hours and are invalid after use. In Development, links are captured in the protected Admin outbox and are never written to logs. A production email provider must replace the development sender before Test or Prod deployment.

User accounts and future club-member records are deliberately separate. A future Member may optionally link to one login account.

## Auditing and permissions

The initial permissions are `Dashboard.View`, `Users.Manage`, `Roles.Manage`, and `Audit.View`. The `Admin` role receives all permissions; the `User` role receives dashboard access. Authorization is enforced by server-side policies as well as navigation visibility.

Audit records include an immutable actor-name snapshot, action code, UTC timestamp, structured details, and optional notes. Account/security changes write their audit event within the same SQL transaction when possible. Audit records cannot be updated or deleted through the application context, and secrets or tokenized links are never recorded.

## Database and tests

Create a migration after model changes:

```powershell
dotnet tool run dotnet-ef migrations add DescriptiveName --project src/EaglesNest.Infrastructure --startup-project src/EaglesNest.Web --output-dir Data/Migrations
```

Run the full validation suite:

```powershell
dotnet build EaglesNest.slnx --configuration Release
dotnet test EaglesNest.slnx --configuration Release --no-build
```

Integration tests use `EAGLESNEST_TEST_CONNECTION` when provided. On this Windows development machine they default to a disposable `EaglesNest_IntegrationTests` database on `localhost`. CI supplies an isolated SQL Server container.

## Branch and promotion workflow

- Create every local feature branch from `Dev`: `git switch Dev` then `git switch -c feature/short-description`.
- Merge feature pull requests into `Dev`.
- Promote `Dev` to `Test` through a pull request.
- Promote `Test` to `Prod` through a pull request.
- Never push directly to `Dev`, `Test`, or `Prod` after branch protection is enabled.

All three long-lived branches require a successful CI build and test run before merge. Azure deployment workflows will be added after App Service, Azure SQL, secrets, data-protection key storage, and the production email provider are selected.

## Theme and fonts

The default Army theme is built from CSS design tokens, so another color theme can be added without changing components. The selection is persisted in browser storage. Body text uses Barlow at an 18px base; Rajdhani is used for 20px headings and 24px-plus titles. Source Sans 3 with Oswald and Atkinson Hyperlegible with Teko are documented alternatives.

Barlow and Rajdhani are bundled locally under the SIL Open Font License; their license files are stored beside the font assets in `src/EaglesNest.Web/wwwroot/fonts`.
