---
title: HealthTrack
aliases: [HealthTrack, Personal Health & Fitness Management System]
tags: [aspnet-mvc, dotnet8, efcore, coursework]
status: in-progress
version: 0.1.0
created: 2026-09-28
updated: 2026-09-28
repo: HealthTrack (branch Baanu)
author: MNS Baanu
---

> [!summary]
> ASP.NET Core MVC app for CS6004ES Coursework 2. Clients track health metrics, workouts and meals; Trainers manage plans and appointments; Admins oversee the system. This branch covers the Client and Admin sections.

## Quick links
- [[README]]
- [[03_ER_Diagram.drawio]]

## Tech stack
.NET 8, ASP.NET Core MVC, EF Core 8.0.26, SQL Server (`LAPTOP-KK271TP3\LOCALHOST`, database `HealthTrackDB`, script in `Private/Database/HealthTrackDB.sql`), ASP.NET Core Identity, Bootstrap 5.

## Commands
- `dotnet build`
- `dotnet ef migrations add <Name>`
- `dotnet ef database update` (also runs automatically on app start)

## Folder structure
- `Controllers/`, `Views/`, `wwwroot/`: MVC template
- `Models/`: Person (abstract, IdentityUser), Client, Admin, HealthMetric, WorkoutLog, MealLog, Notification
- `Models/Data/HealthTrackDbContext.cs`: IdentityDbContext<Person>
- `Migrations/`: EF migrations
- `Private/`: docs, diagrams, reference material (excluded from build)

## How it works
- Person inherits IdentityUser; Client and Admin inherit Person (TPH in `Users`; Identity tables renamed without the AspNet prefix, discriminator column `Role`).
- Client 1:N HealthMetric, WorkoutLog, MealLog; Person 1:N Notification.
- BMI is derived (not stored) from WeightKg and Client.HeightCm.
- Pending migrations are applied at startup via `Database.Migrate()`.

## Open issues
- [ ] Trainer entity, Appointment, WorkoutPlan, DietPlan, Feedback, Client.TrainerId (partner's side)
- [ ] Register Identity services and roles (backend)
- [ ] Client and Admin controllers and views
- [ ] PDF/CSV export, notifications, analytics

## Log
- 2026-09-28: Client/Admin database schema with Identity; InitialCreate migration; ER diagram updated.
