CREATE DATABASE [HealthTrackDB];
GO

USE [HealthTrackDB];
GO

Build started...
Build succeeded.
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [DateOfBirth] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastLoginAt] datetime2 NULL,
        [Role] nvarchar(8) NOT NULL,
        [AccessLevel] int NULL,
        [HeightCm] float NULL,
        [FitnessGoal] nvarchar(200) NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE TABLE [RoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE TABLE [HealthMetrics] (
        [MetricId] int NOT NULL IDENTITY,
        [ClientId] nvarchar(450) NOT NULL,
        [WeightKg] float NOT NULL,
        [RecordedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_HealthMetrics] PRIMARY KEY ([MetricId]),
        CONSTRAINT [FK_HealthMetrics_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE TABLE [MealLogs] (
        [MealLogId] int NOT NULL IDENTITY,
        [ClientId] nvarchar(450) NOT NULL,
        [MealType] nvarchar(20) NOT NULL,
        [Calories] int NOT NULL,
        [FoodItems] nvarchar(500) NULL,
        [LoggedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MealLogs] PRIMARY KEY ([MealLogId]),
        CONSTRAINT [FK_MealLogs_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [NotificationId] int NOT NULL IDENTITY,
        [PersonId] nvarchar(450) NOT NULL,
        [Message] nvarchar(300) NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ScheduledFor] datetime2 NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId]),
        CONSTRAINT [FK_Notifications_Users_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE TABLE [UserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE TABLE [UserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE TABLE [UserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE TABLE [WorkoutLogs] (
        [WorkoutLogId] int NOT NULL IDENTITY,
        [ClientId] nvarchar(450) NOT NULL,
        [Activity] nvarchar(100) NOT NULL,
        [DurationMinutes] int NOT NULL,
        [CaloriesBurned] int NOT NULL,
        [LoggedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WorkoutLogs] PRIMARY KEY ([WorkoutLogId]),
        CONSTRAINT [FK_WorkoutLogs_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HealthMetrics_ClientId] ON [HealthMetrics] ([ClientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MealLogs_ClientId] ON [MealLogs] ([ClientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_PersonId] ON [Notifications] ([PersonId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WorkoutLogs_ClientId] ON [WorkoutLogs] ([ClientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928153602_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260928153602_InitialCreate', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011054717_AddConsultationBooking'
)
BEGIN
    CREATE TABLE [ConsultationProviders] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Specialty] nvarchar(200) NULL,
        [Kind] int NOT NULL,
        [IsActive] bit NOT NULL,
        [PersonId] nvarchar(450) NULL,
        CONSTRAINT [PK_ConsultationProviders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConsultationProviders_Users_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011054717_AddConsultationBooking'
)
BEGIN
    CREATE TABLE [AvailabilitySlots] (
        [Id] int NOT NULL IDENTITY,
        [ProviderId] int NOT NULL,
        [StartUtc] datetime2 NOT NULL,
        [EndUtc] datetime2 NOT NULL,
        [IsAvailable] bit NOT NULL,
        CONSTRAINT [PK_AvailabilitySlots] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AvailabilitySlots_TimeRange] CHECK ([EndUtc] > [StartUtc]),
        CONSTRAINT [FK_AvailabilitySlots_ConsultationProviders_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [ConsultationProviders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011054717_AddConsultationBooking'
)
BEGIN
    CREATE TABLE [Appointments] (
        [Id] int NOT NULL IDENTITY,
        [ClientId] nvarchar(450) NOT NULL,
        [SlotId] int NOT NULL,
        [Status] int NOT NULL,
        [RequestedAtUtc] datetime2 NOT NULL,
        [Notes] nvarchar(2000) NULL,
        CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Appointments_Status] CHECK ([Status] IN (0, 1, 2, 3)),
        CONSTRAINT [FK_Appointments_AvailabilitySlots_SlotId] FOREIGN KEY ([SlotId]) REFERENCES [AvailabilitySlots] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Appointments_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011054717_AddConsultationBooking'
)
BEGIN
    CREATE INDEX [IX_Appointments_ClientId_Status] ON [Appointments] ([ClientId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011054717_AddConsultationBooking'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Appointments_SlotId] ON [Appointments] ([SlotId]) WHERE [Status] <> 2');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011054717_AddConsultationBooking'
)
BEGIN
    CREATE INDEX [IX_AvailabilitySlots_ProviderId_StartUtc_EndUtc] ON [AvailabilitySlots] ([ProviderId], [StartUtc], [EndUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011054717_AddConsultationBooking'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ConsultationProviders_PersonId] ON [ConsultationProviders] ([PersonId]) WHERE [PersonId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011054717_AddConsultationBooking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011054717_AddConsultationBooking', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011061522_AddClientTrainerAssignments'
)
BEGIN
    CREATE TABLE [ClientTrainerAssignments] (
        [ClientId] nvarchar(450) NOT NULL,
        [TrainerId] nvarchar(450) NOT NULL,
        [AssignedAtUtc] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ClientTrainerAssignments] PRIMARY KEY ([ClientId]),
        CONSTRAINT [FK_ClientTrainerAssignments_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ClientTrainerAssignments_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011061522_AddClientTrainerAssignments'
)
BEGIN
    CREATE INDEX [IX_ClientTrainerAssignments_TrainerId] ON [ClientTrainerAssignments] ([TrainerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011061522_AddClientTrainerAssignments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011061522_AddClientTrainerAssignments', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011065303_AddAppointmentReminders'
)
BEGIN
    ALTER TABLE [Notifications] ADD [AppointmentId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011065303_AddAppointmentReminders'
)
BEGIN
    ALTER TABLE [Notifications] ADD [AppointmentStartUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011065303_AddAppointmentReminders'
)
BEGIN
    ALTER TABLE [Notifications] ADD [IsDismissed] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011065303_AddAppointmentReminders'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Notifications_AppointmentId] ON [Notifications] ([AppointmentId]) WHERE [AppointmentId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011065303_AddAppointmentReminders'
)
BEGIN
    ALTER TABLE [Notifications] ADD CONSTRAINT [FK_Notifications_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011065303_AddAppointmentReminders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011065303_AddAppointmentReminders', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011071615_AddClientProfilePhoto'
)
BEGIN
    ALTER TABLE [Users] ADD [ProfilePhoto] varbinary(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011071615_AddClientProfilePhoto'
)
BEGIN
    ALTER TABLE [Users] ADD [ProfilePhotoContentType] nvarchar(32) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011071615_AddClientProfilePhoto'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011071615_AddClientProfilePhoto', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE TABLE [DietPlans] (
        [Id] int NOT NULL IDENTITY,
        [TrainerId] nvarchar(450) NOT NULL,
        [ClientId] nvarchar(450) NOT NULL,
        [Title] nvarchar(120) NOT NULL,
        [DailyCalorieTarget] int NOT NULL,
        [TargetProteinGrams] int NULL,
        [TargetCarbsGrams] int NULL,
        [TargetFatGrams] int NULL,
        [Description] nvarchar(1000) NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_DietPlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DietPlans_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DietPlans_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE TABLE [TrainerFeedbacks] (
        [Id] int NOT NULL IDENTITY,
        [TrainerId] nvarchar(450) NOT NULL,
        [ClientId] nvarchar(450) NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Comments] nvarchar(2000) NOT NULL,
        [Category] nvarchar(20) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [IsReadByClient] bit NOT NULL,
        CONSTRAINT [PK_TrainerFeedbacks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrainerFeedbacks_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TrainerFeedbacks_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE TABLE [WorkoutPlans] (
        [Id] int NOT NULL IDENTITY,
        [TrainerId] nvarchar(450) NOT NULL,
        [ClientId] nvarchar(450) NOT NULL,
        [Title] nvarchar(120) NOT NULL,
        [Goal] nvarchar(100) NULL,
        [Description] nvarchar(1000) NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_WorkoutPlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkoutPlans_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkoutPlans_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE TABLE [DietPlanMeals] (
        [Id] int NOT NULL IDENTITY,
        [DietPlanId] int NOT NULL,
        [MealType] nvarchar(20) NOT NULL,
        [FoodRecommendations] nvarchar(300) NOT NULL,
        [ApproximateCalories] int NULL,
        [Notes] nvarchar(300) NULL,
        CONSTRAINT [PK_DietPlanMeals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DietPlanMeals_DietPlans_DietPlanId] FOREIGN KEY ([DietPlanId]) REFERENCES [DietPlans] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE TABLE [WorkoutPlanItems] (
        [Id] int NOT NULL IDENTITY,
        [WorkoutPlanId] int NOT NULL,
        [DayOfWeek] int NULL,
        [ExerciseName] nvarchar(100) NOT NULL,
        [Sets] int NULL,
        [Reps] int NULL,
        [DurationMinutes] int NULL,
        [RestSeconds] int NULL,
        [Instructions] nvarchar(500) NULL,
        CONSTRAINT [PK_WorkoutPlanItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkoutPlanItems_WorkoutPlans_WorkoutPlanId] FOREIGN KEY ([WorkoutPlanId]) REFERENCES [WorkoutPlans] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE INDEX [IX_DietPlanMeals_DietPlanId] ON [DietPlanMeals] ([DietPlanId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE INDEX [IX_DietPlans_ClientId] ON [DietPlans] ([ClientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE INDEX [IX_DietPlans_TrainerId] ON [DietPlans] ([TrainerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE INDEX [IX_TrainerFeedbacks_ClientId] ON [TrainerFeedbacks] ([ClientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE INDEX [IX_TrainerFeedbacks_TrainerId] ON [TrainerFeedbacks] ([TrainerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE INDEX [IX_WorkoutPlanItems_WorkoutPlanId] ON [WorkoutPlanItems] ([WorkoutPlanId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE INDEX [IX_WorkoutPlans_ClientId] ON [WorkoutPlans] ([ClientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    CREATE INDEX [IX_WorkoutPlans_TrainerId] ON [WorkoutPlans] ([TrainerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012135644_AddTrainerPlansAndFeedback'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012135644_AddTrainerPlansAndFeedback', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022135132_AddHealthStats'
)
BEGIN
    ALTER TABLE [HealthMetrics] ADD [BodyFatPercent] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022135132_AddHealthStats'
)
BEGIN
    ALTER TABLE [HealthMetrics] ADD [DiastolicBp] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022135132_AddHealthStats'
)
BEGIN
    ALTER TABLE [HealthMetrics] ADD [RestingHeartRate] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022135132_AddHealthStats'
)
BEGIN
    ALTER TABLE [HealthMetrics] ADD [SleepHours] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022135132_AddHealthStats'
)
BEGIN
    ALTER TABLE [HealthMetrics] ADD [Steps] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022135132_AddHealthStats'
)
BEGIN
    ALTER TABLE [HealthMetrics] ADD [SystolicBp] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022135132_AddHealthStats'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022135132_AddHealthStats', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022135430_AddFeedbackReply'
)
BEGIN
    ALTER TABLE [TrainerFeedbacks] ADD [ClientRepliedAtUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022135430_AddFeedbackReply'
)
BEGIN
    ALTER TABLE [TrainerFeedbacks] ADD [ClientReply] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022135430_AddFeedbackReply'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022135430_AddFeedbackReply', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142518_AddDietSchedule'
)
BEGIN
    ALTER TABLE [DietPlanMeals] ADD [Day] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142518_AddDietSchedule'
)
BEGIN
    ALTER TABLE [DietPlanMeals] ADD [Time] time NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142518_AddDietSchedule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022142518_AddDietSchedule', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022150530_AddLoginHistory'
)
BEGIN
    CREATE TABLE [LoginHistories] (
        [Id] int NOT NULL IDENTITY,
        [PersonId] nvarchar(450) NOT NULL,
        [LoggedInAt] datetime2 NOT NULL,
        [Succeeded] bit NOT NULL,
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(300) NULL,
        CONSTRAINT [PK_LoginHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LoginHistories_Users_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022150530_AddLoginHistory'
)
BEGIN
    CREATE INDEX [IX_LoginHistories_PersonId] ON [LoginHistories] ([PersonId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022150530_AddLoginHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022150530_AddLoginHistory', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022152330_AddAssignmentHistory'
)
BEGIN
    CREATE TABLE [TrainerAssignmentHistories] (
        [Id] int NOT NULL IDENTITY,
        [ClientId] nvarchar(450) NOT NULL,
        [TrainerId] nvarchar(450) NOT NULL,
        [AssignedAtUtc] datetime2 NOT NULL,
        [EndedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_TrainerAssignmentHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrainerAssignmentHistories_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TrainerAssignmentHistories_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022152330_AddAssignmentHistory'
)
BEGIN
    CREATE INDEX [IX_TrainerAssignmentHistories_ClientId] ON [TrainerAssignmentHistories] ([ClientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022152330_AddAssignmentHistory'
)
BEGIN
    CREATE INDEX [IX_TrainerAssignmentHistories_TrainerId] ON [TrainerAssignmentHistories] ([TrainerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022152330_AddAssignmentHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022152330_AddAssignmentHistory', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153059_AddWorkoutLogPlanItem'
)
BEGIN
    ALTER TABLE [WorkoutLogs] ADD [WorkoutPlanItemId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153059_AddWorkoutLogPlanItem'
)
BEGIN
    CREATE INDEX [IX_WorkoutLogs_WorkoutPlanItemId] ON [WorkoutLogs] ([WorkoutPlanItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153059_AddWorkoutLogPlanItem'
)
BEGIN
    ALTER TABLE [WorkoutLogs] ADD CONSTRAINT [FK_WorkoutLogs_WorkoutPlanItems_WorkoutPlanItemId] FOREIGN KEY ([WorkoutPlanItemId]) REFERENCES [WorkoutPlanItems] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153059_AddWorkoutLogPlanItem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022153059_AddWorkoutLogPlanItem', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022161926_AddReminderPreferences'
)
BEGIN
    ALTER TABLE [Users] ADD [MealReminderEnabled] bit NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022161926_AddReminderPreferences'
)
BEGIN
    ALTER TABLE [Users] ADD [MealReminderHour] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022161926_AddReminderPreferences'
)
BEGIN
    ALTER TABLE [Users] ADD [WeightReminderEnabled] bit NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022161926_AddReminderPreferences'
)
BEGIN
    ALTER TABLE [Users] ADD [WeightReminderHour] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022161926_AddReminderPreferences'
)
BEGIN
    ALTER TABLE [Users] ADD [WorkoutReminderEnabled] bit NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022161926_AddReminderPreferences'
)
BEGIN
    ALTER TABLE [Users] ADD [WorkoutReminderHour] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022161926_AddReminderPreferences'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022161926_AddReminderPreferences', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022165230_AddChatQuestions'
)
BEGIN
    CREATE TABLE [ChatQuestions] (
        [Id] int NOT NULL IDENTITY,
        [Question] nvarchar(300) NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [AiAnswered] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ChatQuestions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022165230_AddChatQuestions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022165230_AddChatQuestions', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022165459_AddAuditLog'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorId] nvarchar(450) NOT NULL,
        [ActorName] nvarchar(256) NOT NULL,
        [ActorRole] nvarchar(20) NOT NULL,
        [Action] nvarchar(20) NOT NULL,
        [Entity] nvarchar(60) NOT NULL,
        [SubjectClientId] nvarchar(450) NULL,
        [Detail] nvarchar(300) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022165459_AddAuditLog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022165459_AddAuditLog', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023143647_AddMealLogPlanLink'
)
BEGIN
    ALTER TABLE [MealLogs] ADD [DietPlanMealId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023143647_AddMealLogPlanLink'
)
BEGIN
    CREATE INDEX [IX_MealLogs_DietPlanMealId] ON [MealLogs] ([DietPlanMealId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023143647_AddMealLogPlanLink'
)
BEGIN
    ALTER TABLE [MealLogs] ADD CONSTRAINT [FK_MealLogs_DietPlanMeals_DietPlanMealId] FOREIGN KEY ([DietPlanMealId]) REFERENCES [DietPlanMeals] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023143647_AddMealLogPlanLink'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023143647_AddMealLogPlanLink', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023151711_AddEmailNotificationsEnabled'
)
BEGIN
    ALTER TABLE [Users] ADD [EmailNotificationsEnabled] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023151711_AddEmailNotificationsEnabled'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023151711_AddEmailNotificationsEnabled', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023154813_AddWeeklyCheckIns'
)
BEGIN
    CREATE TABLE [WeeklyCheckIns] (
        [Id] int NOT NULL IDENTITY,
        [TrainerId] nvarchar(450) NOT NULL,
        [ClientId] nvarchar(450) NOT NULL,
        [RequestedAtUtc] datetime2 NOT NULL,
        [Note] nvarchar(300) NULL,
        [RespondedAtUtc] datetime2 NULL,
        [Energy] int NULL,
        [Mood] int NULL,
        [Pain] int NULL,
        [Comment] nvarchar(1000) NULL,
        CONSTRAINT [PK_WeeklyCheckIns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WeeklyCheckIns_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WeeklyCheckIns_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023154813_AddWeeklyCheckIns'
)
BEGIN
    CREATE INDEX [IX_WeeklyCheckIns_ClientId_RequestedAtUtc] ON [WeeklyCheckIns] ([ClientId], [RequestedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023154813_AddWeeklyCheckIns'
)
BEGIN
    CREATE INDEX [IX_WeeklyCheckIns_TrainerId] ON [WeeklyCheckIns] ([TrainerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023154813_AddWeeklyCheckIns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023154813_AddWeeklyCheckIns', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023161615_AddGoalsAndRatings'
)
BEGIN
    ALTER TABLE [Users] ADD [TargetDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023161615_AddGoalsAndRatings'
)
BEGIN
    ALTER TABLE [Users] ADD [TargetWeightKg] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023161615_AddGoalsAndRatings'
)
BEGIN
    ALTER TABLE [Appointments] ADD [RatedAtUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023161615_AddGoalsAndRatings'
)
BEGIN
    ALTER TABLE [Appointments] ADD [Rating] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023161615_AddGoalsAndRatings'
)
BEGIN
    ALTER TABLE [Appointments] ADD [RatingComment] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023161615_AddGoalsAndRatings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023161615_AddGoalsAndRatings', N'8.0.26');
END;
GO

COMMIT;
GO


