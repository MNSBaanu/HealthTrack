# HealthTrack

HealthTrack is an ASP.NET Core MVC application for recording fitness activity, meals, and health measurements. It is developed for the Application Development coursework project.

This README describes the implementation on **Baanu**, which covers the **Client and Admin** modules. The Trainer module is developed separately by **Irfa**.

## Current features

### Client

- Register with initial weight, height, date of birth, and an optional fitness goal.
- Sign in, sign out, and update profile information.
- View dashboard summaries for weight, BMI, workouts, and meals.
- Create, view, edit, and delete personal workout logs, meal logs, and health measurements.
- View progress charts and export personal data as PDF or CSV.
- View notifications and reminders for workouts, meals, and weight check-ins.

### Admin

- View dashboard totals and application activity.
- Search and filter users and inspect Client details.
- Create Admin accounts, lock or unlock accounts, and delete users.
- View Client performance, logged activity, and engagement reports.
- Export reports as PDF or CSV.
- Send notifications to individual Clients or all Clients, including scheduled notifications.

Authentication uses ASP.NET Core Identity. Client and Admin routes use role-based authorization, and form requests use antiforgery validation.

## Pending integration

The current branch does not yet implement Trainer accounts, assigned workout/diet plans, consultation appointments, or Admin Client–Trainer assignments. These require shared models and integration with the Trainer module.

Appointment reminders are also pending. Existing reminders are generated when a Client visits the dashboard; there is no background reminder worker. The current popular activity report groups workout log activity names, rather than assigned Trainer routines.

## Technology

| Component | Technology |
| --- | --- |
| Application | ASP.NET Core MVC, C#, .NET 8 |
| Authentication | ASP.NET Core Identity |
| Database | SQL Server / SQL Server LocalDB |
| Data access | Entity Framework Core 8 |
| PDF exports | QuestPDF |
| Frontend | Razor views, CSS, JavaScript, Chart.js |

## Requirements

- .NET 8 SDK, or a compatible SDK capable of building `net8.0` projects.
- SQL Server or SQL Server Express LocalDB.
- Visual Studio 2022 with the ASP.NET and web development workload, or an editor with .NET CLI support.
- Internet access for the initial NuGet package restore and CDN-hosted frontend assets.

## Local setup

### 1. Clone and choose a branch

```powershell
git clone https://github.com/MNSBaanu/HealthTrack.git
cd HealthTrack
```

Use `main` for the merged project. To work on the Client/Admin branch:

```powershell
git switch Baanu
```

### 2. Configure SQL Server

The default connection in `appsettings.json` uses `(localdb)\MSSQLLocalDB` and the database `HealthTrackDB`.

For a different SQL Server instance, create an optional local configuration file:

```powershell
New-Item -Path appsettings.Local.json -ItemType File
```

Update `ConnectionStrings:DefaultConnection` in `appsettings.Local.json`. For example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR-PC-NAME\\SQLEXPRESS;Database=HealthTrackDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

`appsettings.Local.json` is loaded after the standard configuration and is ignored by Git. Keep machine-specific settings and credentials in that file. The example above uses Windows authentication and trusts the server certificate for local development.

The SQL Server instance must be running. The account used by the connection needs permission to create the database and apply schema migrations.

### 3. Restore and run

Run these commands from the repository root:

```powershell
dotnet restore HealthTrack.csproj
dotnet build HealthTrack.csproj
dotnet dev-certs https --trust
dotnet run --project HealthTrack.csproj --launch-profile HealthTrack
```

The launch profile opens **https://localhost:56364**. An HTTP endpoint is also configured at `http://localhost:56365`, with HTTPS redirection enabled.

On startup, the application automatically applies the checked-in EF Core migrations and seeds the Client/Admin roles and initial Admin account. A separate database-update command is not required for normal local startup.

Alternatively, open `HealthTrack.sln` in Visual Studio, select the root HealthTrack project, and run the HealthTrack launch profile.

## First login

The checked-in development configuration seeds this Admin account on a new database:

| Field | Development value |
| --- | --- |
| Email | `admin@healthtrack.com` |
| Password | `CHANGE-ME` |

Override the `AdminAccount` name, email, and password in `appsettings.Local.json` before initializing a shared database. The seeder only creates the account when its configured email does not already exist; changing the configuration does not reset an existing account's password.

For a Client account, use the registration page and provide the initial health measurements. Passwords must meet the Identity password rules, including a minimum of eight characters.

## Project structure

| Path | Purpose |
| --- | --- |
| `Controllers/` | Account, Client, Admin, logging, and notification actions |
| `Models/` | Identity entities, health records, and view models |
| `Models/Data/` | EF Core context and initial role/Admin seeding |
| `Migrations/` | Versioned database schema migrations |
| `Services/` | Notification generation and PDF/CSV exports |
| `Views/` | Razor pages and shared layouts |
| `ViewComponents/` | Reusable view components |
| `wwwroot/` | Static frontend assets |
| `Properties/launchSettings.json` | Local launch profile |
| `Private/` | Reference material excluded from the root application's build |

## Team workflow

- **Baanu:** Client and Admin development.
- **Irfa:** Trainer development.
- **main:** Integrated project.

Agree on Trainer identity, Client–Trainer relationships, plan assignments, appointment statuses, and database migrations before connecting the modules. Use shared entities and the existing Identity/database context so each module reads the same records.

## Troubleshooting

- **Database connection fails:** Check the SQL Server instance name, service status, authentication, and database permissions in the effective local connection string.
- **HTTPS certificate warning:** Run `dotnet dev-certs https --trust` and restart the browser/application.
- **Admin login fails:** Confirm the configured email and whether that account was already created in the database. Seeding does not update existing passwords.
- **Charts do not load:** Check access to the CDN assets and the browser console.
