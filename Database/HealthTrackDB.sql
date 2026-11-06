CREATE DATABASE [HealthTrackDB];
GO

USE [HealthTrackDB];
GO

SET QUOTED_IDENTIFIER ON;
GO

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

CREATE TABLE [Roles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO

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
GO

CREATE TABLE [RoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [HealthMetrics] (
    [MetricId] int NOT NULL IDENTITY,
    [ClientId] nvarchar(450) NOT NULL,
    [WeightKg] float NOT NULL,
    [RecordedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_HealthMetrics] PRIMARY KEY ([MetricId]),
    CONSTRAINT [FK_HealthMetrics_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

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
GO

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
GO

CREATE TABLE [UserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

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
GO

CREATE INDEX [IX_HealthMetrics_ClientId] ON [HealthMetrics] ([ClientId]);
GO

CREATE INDEX [IX_MealLogs_ClientId] ON [MealLogs] ([ClientId]);
GO

CREATE INDEX [IX_Notifications_PersonId] ON [Notifications] ([PersonId]);
GO

CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);
GO

CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);
GO

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE INDEX [IX_WorkoutLogs_ClientId] ON [WorkoutLogs] ([ClientId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260928153602_InitialCreate', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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
GO

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
GO

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
GO

CREATE INDEX [IX_Appointments_ClientId_Status] ON [Appointments] ([ClientId], [Status]);
GO

CREATE UNIQUE INDEX [IX_Appointments_SlotId] ON [Appointments] ([SlotId]) WHERE [Status] <> 2;
GO

CREATE INDEX [IX_AvailabilitySlots_ProviderId_StartUtc_EndUtc] ON [AvailabilitySlots] ([ProviderId], [StartUtc], [EndUtc]);
GO

CREATE UNIQUE INDEX [IX_ConsultationProviders_PersonId] ON [ConsultationProviders] ([PersonId]) WHERE [PersonId] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261011054717_AddConsultationBooking', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ClientTrainerAssignments] (
    [ClientId] nvarchar(450) NOT NULL,
    [TrainerId] nvarchar(450) NOT NULL,
    [AssignedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_ClientTrainerAssignments] PRIMARY KEY ([ClientId]),
    CONSTRAINT [FK_ClientTrainerAssignments_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ClientTrainerAssignments_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_ClientTrainerAssignments_TrainerId] ON [ClientTrainerAssignments] ([TrainerId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261011061522_AddClientTrainerAssignments', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Notifications] ADD [AppointmentId] int NULL;
GO

ALTER TABLE [Notifications] ADD [AppointmentStartUtc] datetime2 NULL;
GO

ALTER TABLE [Notifications] ADD [IsDismissed] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

CREATE UNIQUE INDEX [IX_Notifications_AppointmentId] ON [Notifications] ([AppointmentId]) WHERE [AppointmentId] IS NOT NULL;
GO

ALTER TABLE [Notifications] ADD CONSTRAINT [FK_Notifications_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261011065303_AddAppointmentReminders', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [ProfilePhoto] varbinary(max) NULL;
GO

ALTER TABLE [Users] ADD [ProfilePhotoContentType] nvarchar(32) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261011071615_AddClientProfilePhoto', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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
GO

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
GO

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
GO

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
GO

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
GO

CREATE INDEX [IX_DietPlanMeals_DietPlanId] ON [DietPlanMeals] ([DietPlanId]);
GO

CREATE INDEX [IX_DietPlans_ClientId] ON [DietPlans] ([ClientId]);
GO

CREATE INDEX [IX_DietPlans_TrainerId] ON [DietPlans] ([TrainerId]);
GO

CREATE INDEX [IX_TrainerFeedbacks_ClientId] ON [TrainerFeedbacks] ([ClientId]);
GO

CREATE INDEX [IX_TrainerFeedbacks_TrainerId] ON [TrainerFeedbacks] ([TrainerId]);
GO

CREATE INDEX [IX_WorkoutPlanItems_WorkoutPlanId] ON [WorkoutPlanItems] ([WorkoutPlanId]);
GO

CREATE INDEX [IX_WorkoutPlans_ClientId] ON [WorkoutPlans] ([ClientId]);
GO

CREATE INDEX [IX_WorkoutPlans_TrainerId] ON [WorkoutPlans] ([TrainerId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261012135644_AddTrainerPlansAndFeedback', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [HealthMetrics] ADD [BodyFatPercent] float NULL;
GO

ALTER TABLE [HealthMetrics] ADD [DiastolicBp] int NULL;
GO

ALTER TABLE [HealthMetrics] ADD [RestingHeartRate] int NULL;
GO

ALTER TABLE [HealthMetrics] ADD [SleepHours] float NULL;
GO

ALTER TABLE [HealthMetrics] ADD [Steps] int NULL;
GO

ALTER TABLE [HealthMetrics] ADD [SystolicBp] int NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261022135132_AddHealthStats', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [TrainerFeedbacks] ADD [ClientRepliedAtUtc] datetime2 NULL;
GO

ALTER TABLE [TrainerFeedbacks] ADD [ClientReply] nvarchar(1000) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261022135430_AddFeedbackReply', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [DietPlanMeals] ADD [Day] int NULL;
GO

ALTER TABLE [DietPlanMeals] ADD [Time] time NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261022142518_AddDietSchedule', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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
GO

CREATE INDEX [IX_LoginHistories_PersonId] ON [LoginHistories] ([PersonId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261022150530_AddLoginHistory', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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
GO

CREATE INDEX [IX_TrainerAssignmentHistories_ClientId] ON [TrainerAssignmentHistories] ([ClientId]);
GO

CREATE INDEX [IX_TrainerAssignmentHistories_TrainerId] ON [TrainerAssignmentHistories] ([TrainerId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261022152330_AddAssignmentHistory', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [WorkoutLogs] ADD [WorkoutPlanItemId] int NULL;
GO

CREATE INDEX [IX_WorkoutLogs_WorkoutPlanItemId] ON [WorkoutLogs] ([WorkoutPlanItemId]);
GO

ALTER TABLE [WorkoutLogs] ADD CONSTRAINT [FK_WorkoutLogs_WorkoutPlanItems_WorkoutPlanItemId] FOREIGN KEY ([WorkoutPlanItemId]) REFERENCES [WorkoutPlanItems] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261022153059_AddWorkoutLogPlanItem', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [MealReminderEnabled] bit NULL;
GO

ALTER TABLE [Users] ADD [MealReminderHour] int NULL;
GO

ALTER TABLE [Users] ADD [WeightReminderEnabled] bit NULL;
GO

ALTER TABLE [Users] ADD [WeightReminderHour] int NULL;
GO

ALTER TABLE [Users] ADD [WorkoutReminderEnabled] bit NULL;
GO

ALTER TABLE [Users] ADD [WorkoutReminderHour] int NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261022161926_AddReminderPreferences', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ChatQuestions] (
    [Id] int NOT NULL IDENTITY,
    [Question] nvarchar(300) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [AiAnswered] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ChatQuestions] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261022165230_AddChatQuestions', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261022165459_AddAuditLog', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [MealLogs] ADD [DietPlanMealId] int NULL;
GO

CREATE INDEX [IX_MealLogs_DietPlanMealId] ON [MealLogs] ([DietPlanMealId]);
GO

ALTER TABLE [MealLogs] ADD CONSTRAINT [FK_MealLogs_DietPlanMeals_DietPlanMealId] FOREIGN KEY ([DietPlanMealId]) REFERENCES [DietPlanMeals] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261023143647_AddMealLogPlanLink', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [EmailNotificationsEnabled] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261023151711_AddEmailNotificationsEnabled', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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
GO

CREATE INDEX [IX_WeeklyCheckIns_ClientId_RequestedAtUtc] ON [WeeklyCheckIns] ([ClientId], [RequestedAtUtc]);
GO

CREATE INDEX [IX_WeeklyCheckIns_TrainerId] ON [WeeklyCheckIns] ([TrainerId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261023154813_AddWeeklyCheckIns', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [RescheduleRequests] (
    [Id] int NOT NULL IDENTITY,
    [AppointmentId] int NOT NULL,
    [Reason] nvarchar(500) NOT NULL,
    [Status] int NOT NULL,
    [TrainerNote] nvarchar(500) NULL,
    [RequestedAtUtc] datetime2 NOT NULL,
    [DecidedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_RescheduleRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_RescheduleRequests_Status] CHECK ([Status] IN (0, 1, 2, 3, 4)),
    CONSTRAINT [FK_RescheduleRequests_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_RescheduleRequests_AppointmentId] ON [RescheduleRequests] ([AppointmentId]) WHERE [Status] IN (0, 1);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261023160602_AddRescheduleRequests', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [TargetDate] datetime2 NULL;
GO

ALTER TABLE [Users] ADD [TargetWeightKg] float NULL;
GO

ALTER TABLE [Appointments] ADD [RatedAtUtc] datetime2 NULL;
GO

ALTER TABLE [Appointments] ADD [Rating] int NULL;
GO

ALTER TABLE [Appointments] ADD [RatingComment] nvarchar(500) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261023161615_AddGoalsAndRatings', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [WorkoutPlanCompletions] (
    [WorkoutPlanItemId] int NOT NULL,
    [ClientId] nvarchar(450) NOT NULL,
    [CompletedOn] date NOT NULL,
    CONSTRAINT [PK_WorkoutPlanCompletions] PRIMARY KEY ([WorkoutPlanItemId], [ClientId], [CompletedOn]),
    CONSTRAINT [FK_WorkoutPlanCompletions_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_WorkoutPlanCompletions_WorkoutPlanItems_WorkoutPlanItemId] FOREIGN KEY ([WorkoutPlanItemId]) REFERENCES [WorkoutPlanItems] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_WorkoutPlanCompletions_ClientId] ON [WorkoutPlanCompletions] ([ClientId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261029153053_AddWorkoutPlanCompletions', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [MustChangePassword] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Users] ADD [TemporaryPasswordExpiresAtUtc] datetime2 NULL;
GO

ALTER TABLE [Notifications] ADD [AdminBroadcastId] int NULL;
GO

CREATE TABLE [AdminBroadcasts] (
    [Id] int NOT NULL IDENTITY,
    [SenderId] nvarchar(450) NOT NULL,
    [SenderName] nvarchar(100) NOT NULL,
    [Message] nvarchar(300) NOT NULL,
    [Type] int NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ScheduledFor] datetime2 NULL,
    [RecipientCount] int NOT NULL,
    [RecipientDescription] nvarchar(400) NOT NULL,
    CONSTRAINT [PK_AdminBroadcasts] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [NotificationEmails] (
    [NotificationId] int NOT NULL,
    [DueAtUtc] datetime2 NOT NULL,
    [Attempts] int NOT NULL,
    [LeaseUntilUtc] datetime2 NULL,
    [SentAtUtc] datetime2 NULL,
    [CancelledAtUtc] datetime2 NULL,
    [LastError] nvarchar(300) NULL,
    CONSTRAINT [PK_NotificationEmails] PRIMARY KEY ([NotificationId]),
    CONSTRAINT [FK_NotificationEmails_Notifications_NotificationId] FOREIGN KEY ([NotificationId]) REFERENCES [Notifications] ([NotificationId]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Notifications_AdminBroadcastId] ON [Notifications] ([AdminBroadcastId]);
GO

CREATE INDEX [IX_AdminBroadcasts_CreatedAtUtc] ON [AdminBroadcasts] ([CreatedAtUtc]);
GO

CREATE INDEX [IX_NotificationEmails_SentAtUtc_CancelledAtUtc_DueAtUtc] ON [NotificationEmails] ([SentAtUtc], [CancelledAtUtc], [DueAtUtc]);
GO

ALTER TABLE [Notifications] ADD CONSTRAINT [FK_Notifications_AdminBroadcasts_AdminBroadcastId] FOREIGN KEY ([AdminBroadcastId]) REFERENCES [AdminBroadcasts] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261029173533_HardenAdminOperations', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Appointments] ADD [RatingHidden] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

CREATE TABLE [ChatFaqs] (
    [Id] int NOT NULL IDENTITY,
    [Question] nvarchar(300) NOT NULL,
    [Answer] nvarchar(1000) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ChatFaqs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SystemSettings] (
    [Id] int NOT NULL,
    [BannerEnabled] bit NOT NULL,
    [BannerMessage] nvarchar(300) NULL,
    [TemporaryPasswordHours] int NOT NULL,
    [MaxFailedAccessAttempts] int NOT NULL,
    [LockoutMinutes] int NOT NULL,
    [ImportMaxRows] int NOT NULL,
    [WeeklyReportEnabled] bit NOT NULL,
    [WeeklyReportSentAtUtc] datetime2 NULL,
    CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261103161054_AdminOversightAndSettings', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [SystemSettings] ADD [RequireAdminTwoFactor] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261103174706_AdminTwoFactorSetting', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WeeklyCheckIns]') AND [c].[name] = N'Note');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [WeeklyCheckIns] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [WeeklyCheckIns] ALTER COLUMN [Note] nvarchar(max) NULL;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WeeklyCheckIns]') AND [c].[name] = N'Comment');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [WeeklyCheckIns] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [WeeklyCheckIns] ALTER COLUMN [Comment] nvarchar(max) NULL;
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'FitnessGoal');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Users] ALTER COLUMN [FitnessGoal] nvarchar(max) NULL;
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrainerFeedbacks]') AND [c].[name] = N'Comments');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [TrainerFeedbacks] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [TrainerFeedbacks] ALTER COLUMN [Comments] nvarchar(max) NOT NULL;
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrainerFeedbacks]') AND [c].[name] = N'ClientReply');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [TrainerFeedbacks] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [TrainerFeedbacks] ALTER COLUMN [ClientReply] nvarchar(max) NULL;
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Appointments]') AND [c].[name] = N'Notes');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Appointments] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Appointments] ALTER COLUMN [Notes] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261105131638_EncryptSensitiveHealthNotes', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO


WITH ranked AS (
    SELECT Id, ROW_NUMBER() OVER (PARTITION BY ClientId, TrainerId ORDER BY RequestedAtUtc DESC, Id DESC) AS rn
    FROM WeeklyCheckIns WHERE RespondedAtUtc IS NULL
)
DELETE FROM WeeklyCheckIns WHERE Id IN (SELECT Id FROM ranked WHERE rn > 1);
GO

CREATE UNIQUE INDEX [IX_WeeklyCheckIns_ClientId_TrainerId] ON [WeeklyCheckIns] ([ClientId], [TrainerId]) WHERE [RespondedAtUtc] IS NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261105143152_UniqueOpenWeeklyCheckIn', N'8.0.26');
GO

COMMIT;
GO


GO

-- ===== Data export =====
-- Exported 2026-11-06 20:42 from (localdb)\MSSQLLocalDB / HealthTrackDB
SET NOCOUNT ON;
GO
ALTER TABLE [dbo].[AdminBroadcasts] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[Appointments] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[AuditLogs] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[AvailabilitySlots] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[ChatFaqs] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[ChatQuestions] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[ClientTrainerAssignments] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[ConsultationProviders] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[DietPlanMeals] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[DietPlans] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[HealthMetrics] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[LoginHistories] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[MealLogs] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[NotificationEmails] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[Notifications] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[RescheduleRequests] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[RoleClaims] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[Roles] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[SystemSettings] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[TrainerAssignmentHistories] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[TrainerFeedbacks] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[UserClaims] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[UserLogins] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[UserRoles] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[Users] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[UserTokens] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[WeeklyCheckIns] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[WorkoutLogs] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[WorkoutPlanCompletions] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[WorkoutPlanItems] NOCHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[WorkoutPlans] NOCHECK CONSTRAINT ALL;
GO
-- [dbo].[Appointments] (2 rows)
SET IDENTITY_INSERT [dbo].[Appointments] ON;
INSERT INTO [dbo].[Appointments] ([Id], [ClientId], [SlotId], [Status], [RequestedAtUtc], [Notes], [RatedAtUtc], [Rating], [RatingComment], [RatingHidden]) VALUES (1, N'1539dea9-3d60-454d-bf56-622240e8c807', 32, 1, '2026-10-24T06:46:28.6952801', NULL, NULL, NULL, NULL, 0);
INSERT INTO [dbo].[Appointments] ([Id], [ClientId], [SlotId], [Status], [RequestedAtUtc], [Notes], [RatedAtUtc], [Rating], [RatingComment], [RatingHidden]) VALUES (2, N'1539dea9-3d60-454d-bf56-622240e8c807', 33, 2, '2026-10-24T17:08:43.5052579', N'I would like advice on increasing my weekly running distance.', NULL, NULL, NULL, 0);
SET IDENTITY_INSERT [dbo].[Appointments] OFF;
GO
-- [dbo].[AuditLogs] (81 rows)
SET IDENTITY_INSERT [dbo].[AuditLogs] ON;
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (1, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T13:28:03.0154858');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (2, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T13:30:49.3205735');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (3, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T13:33:45.5734626');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (4, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T13:37:13.5926213');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (5, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T13:55:48.6875513');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (6, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T13:56:00.0909237');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (7, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T13:57:05.7399423');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (8, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T13:57:49.9060444');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (9, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T13:59:27.1621304');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (10, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T13:59:31.4635244');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (11, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'HealthMetric', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T14:02:31.3221306');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (12, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'HealthMetric', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T14:02:50.4950624');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (13, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Deleted', N'HealthMetric', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T14:03:18.8434207');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (14, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'HealthMetric', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T14:03:28.1894012');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (15, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'WorkoutLog', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T14:29:46.3433519');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (16, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'WorkoutLog', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T14:30:33.0692324');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (17, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'MealLog', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T14:32:34.6046890');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (18, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'WorkoutLog', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T14:38:50.4827793');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (19, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T14:46:00.0381449');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (20, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T14:46:32.6704509');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (21, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Exported', N'TrainerClientReport', NULL, N'pdf', '2026-10-23T14:47:52.0291648');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (22, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Exported', N'TrainerClientReport', NULL, N'csv', '2026-10-23T14:48:02.8076972');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (23, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-23T14:55:01.7662098');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (24, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Exported', N'ClientPerformanceReport', NULL, N'pdf', '2026-10-23T15:01:23.3204851');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (25, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Exported', N'ClientPerformanceReport', NULL, N'pdf', '2026-10-23T15:02:53.4734200');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (26, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Exported', N'ClientPerformanceReport', NULL, N'pdf', '2026-10-23T15:03:02.1069098');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (27, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Exported', N'ClientPerformanceReport', NULL, N'pdf', '2026-10-23T15:07:43.7341150');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (28, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Exported', N'ClientPerformanceReport', NULL, N'csv', '2026-10-23T15:07:54.1552912');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (29, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'ClientDetails', N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', NULL, '2026-10-24T06:25:57.3207633');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (30, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'ClientDetails', N'0335c890-3be6-4566-a5b0-7213527bea12', NULL, '2026-10-24T06:26:02.4793317');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (31, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Exported', N'ClientPerformanceReport', NULL, N'pdf', '2026-10-24T06:32:46.0565171');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (32, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Exported', N'ClientPerformanceReport', NULL, N'csv', '2026-10-24T06:33:11.2927388');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (33, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'ClientDetails', N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', NULL, '2026-10-24T06:48:58.6438422');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (34, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T06:50:22.3278942');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (35, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'MealLog', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T06:52:38.6082481');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (36, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T06:53:04.1464770');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (37, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Exported', N'ProgressReport', N'1539dea9-3d60-454d-bf56-622240e8c807', N'csv', '2026-10-24T17:03:29.5639790');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (38, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T17:05:44.2469631');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (39, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T17:06:01.0071380');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (40, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'TrainerDetails', N'6c675f46-eaa2-4f3d-baae-f993d6520ca7', NULL, '2026-10-24T17:06:02.6182490');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (41, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T17:08:02.4835248');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (42, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T17:16:28.2061575');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (43, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T17:16:52.6993276');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (44, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T17:22:01.6589624');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (45, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T17:22:02.9659148');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (46, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T17:22:15.7776767');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (10037, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T18:06:30.1862674');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (10038, N'a8995675-309a-4237-adcb-2b86a331a936', N'trainer@healthtrack.com', N'Trainer', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T18:06:31.6761640');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (10039, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'ClientDetails', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T18:06:44.2769470');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (20037, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'HealthMetric', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T18:38:36.8225717');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (20038, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Imported', N'Users', NULL, N'0 imported, 1 rejected', '2026-10-24T18:40:24.3418038');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (20039, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'HealthMetric', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T18:40:47.3090473');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (20040, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Imported', N'Users', NULL, N'0 imported, 1 rejected', '2026-10-24T18:42:02.2992153');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (20041, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Imported', N'Users', NULL, N'0 imported, 1 rejected', '2026-10-24T18:42:41.9677025');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (20042, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Deleted', N'HealthMetric', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-24T18:44:04.8030035');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (30037, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'TrainerDetails', N'6c675f46-eaa2-4f3d-baae-f993d6520ca7', NULL, '2026-10-24T20:28:16.6061140');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (30038, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'ClientDetails', N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', NULL, '2026-10-24T20:28:18.6498294');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (30039, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'TrainerDetails', N'6c675f46-eaa2-4f3d-baae-f993d6520ca7', NULL, '2026-10-24T20:28:47.2613160');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (30040, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'TrainerDetails', N'6c675f46-eaa2-4f3d-baae-f993d6520ca7', NULL, '2026-10-24T20:28:51.1710080');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (30041, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'TrainerDetails', N'6c675f46-eaa2-4f3d-baae-f993d6520ca7', NULL, '2026-10-24T20:28:57.2703353');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (30042, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'MealLog', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-29T15:49:58.7230962');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (30043, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'HealthMetric', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-29T15:50:38.3949505');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (30044, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Updated', N'HealthMetric', N'1539dea9-3d60-454d-bf56-622240e8c807', N'Changed: BodyFatPercent', '2026-10-29T15:50:50.5727720');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (30045, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Deleted', N'HealthMetric', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-10-29T15:54:57.2674185');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (30046, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Updated', N'Admin', NULL, N'Account 84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-03T14:30:57.3464744');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (40046, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'ClientDetails', N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', NULL, '2026-11-03T15:05:37.9261444');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (50046, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Viewed', N'ClientDetails', N'0335c890-3be6-4566-a5b0-7213527bea12', NULL, '2026-11-03T15:13:10.6523194');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (60046, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Updated', N'Client', N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Account b3745867-bbf7-4d08-a4d6-3840c7fa21e7', '2026-11-03T15:42:02.1822440');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (60047, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Locked', N'Client', N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Account b3745867-bbf7-4d08-a4d6-3840c7fa21e7', '2026-11-03T15:42:02.2779664');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (60048, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Updated', N'Client', N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Account b3745867-bbf7-4d08-a4d6-3840c7fa21e7', '2026-11-03T15:42:02.2830876');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (60049, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Updated', N'Client', N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Account b3745867-bbf7-4d08-a4d6-3840c7fa21e7', '2026-11-03T15:42:04.5788571');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (60050, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Unlocked', N'Client', N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Account b3745867-bbf7-4d08-a4d6-3840c7fa21e7', '2026-11-03T15:42:04.5808379');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (60051, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Updated', N'Client', N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Account b3745867-bbf7-4d08-a4d6-3840c7fa21e7', '2026-11-03T15:42:04.5823382');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (60052, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Exported', N'ClientPerformanceReport', NULL, N'pdf', '2026-11-03T15:46:41.2842916');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (70046, N'1539dea9-3d60-454d-bf56-622240e8c807', N'client@healthtrack.com', N'Client', N'Created', N'HealthMetric', N'1539dea9-3d60-454d-bf56-622240e8c807', NULL, '2026-11-03T17:40:01.1029250');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (70047, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Updated', N'Admin', NULL, N'Account 84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-03T17:40:18.3678396');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (70048, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Updated', N'Admin', NULL, N'Account 84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-03T17:40:50.8526216');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (70049, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Updated', N'Admin', NULL, N'Account 84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-03T17:41:26.7704561');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (70050, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Updated', N'Admin', NULL, N'Account 84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-03T17:42:10.0846081');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (70051, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Updated', N'Admin', NULL, N'Account 84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-05T13:18:29.1653721');
INSERT INTO [dbo].[AuditLogs] ([Id], [ActorId], [ActorName], [ActorRole], [Action], [Entity], [SubjectClientId], [Detail], [CreatedAtUtc]) VALUES (80051, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'admin@healthtrack.com', N'Admin', N'Updated', N'Admin', NULL, N'Account 84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-06T15:06:16.8741296');
SET IDENTITY_INSERT [dbo].[AuditLogs] OFF;
GO
-- [dbo].[AvailabilitySlots] (33 rows)
SET IDENTITY_INSERT [dbo].[AvailabilitySlots] ON;
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (2, 2, '2026-10-24T04:00:00.0000000', '2026-10-24T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (3, 2, '2026-10-25T04:00:00.0000000', '2026-10-25T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (4, 2, '2026-10-26T04:00:00.0000000', '2026-10-26T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (5, 2, '2026-10-27T04:00:00.0000000', '2026-10-27T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (6, 2, '2026-10-28T04:00:00.0000000', '2026-10-28T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (7, 3, '2026-10-24T04:00:00.0000000', '2026-10-24T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (8, 3, '2026-10-25T04:00:00.0000000', '2026-10-25T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (9, 3, '2026-10-26T04:00:00.0000000', '2026-10-26T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (10, 3, '2026-10-27T04:00:00.0000000', '2026-10-27T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (11, 3, '2026-10-28T04:00:00.0000000', '2026-10-28T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (12, 4, '2026-10-24T04:00:00.0000000', '2026-10-24T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (13, 4, '2026-10-25T04:00:00.0000000', '2026-10-25T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (14, 4, '2026-10-26T04:00:00.0000000', '2026-10-26T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (15, 4, '2026-10-27T04:00:00.0000000', '2026-10-27T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (16, 4, '2026-10-28T04:00:00.0000000', '2026-10-28T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (17, 10, '2026-10-24T06:00:00.0000000', '2026-10-24T07:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (18, 10, '2026-10-25T06:00:00.0000000', '2026-10-25T07:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (19, 10, '2026-10-26T06:00:00.0000000', '2026-10-26T07:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (20, 10, '2026-10-27T06:00:00.0000000', '2026-10-27T07:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (21, 10, '2026-10-28T06:00:00.0000000', '2026-10-28T07:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (22, 9, '2026-10-24T05:00:00.0000000', '2026-10-24T06:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (23, 9, '2026-10-25T05:00:00.0000000', '2026-10-25T06:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (24, 9, '2026-10-26T05:00:00.0000000', '2026-10-26T06:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (25, 9, '2026-10-27T05:00:00.0000000', '2026-10-27T06:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (26, 9, '2026-10-28T05:00:00.0000000', '2026-10-28T06:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (27, 8, '2026-10-24T04:00:00.0000000', '2026-10-24T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (28, 8, '2026-10-25T04:00:00.0000000', '2026-10-25T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (29, 8, '2026-10-26T04:00:00.0000000', '2026-10-26T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (30, 8, '2026-10-27T04:00:00.0000000', '2026-10-27T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (31, 8, '2026-10-28T04:00:00.0000000', '2026-10-28T05:00:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (32, 1, '2026-10-25T03:30:00.0000000', '2026-10-26T04:30:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (33, 1, '2026-10-30T04:30:00.0000000', '2026-10-30T05:30:00.0000000', 1);
INSERT INTO [dbo].[AvailabilitySlots] ([Id], [ProviderId], [StartUtc], [EndUtc], [IsAvailable]) VALUES (34, 1, '2026-11-02T03:30:00.0000000', '2026-11-02T04:30:00.0000000', 1);
SET IDENTITY_INSERT [dbo].[AvailabilitySlots] OFF;
GO
-- [dbo].[ChatQuestions] (2 rows)
SET IDENTITY_INSERT [dbo].[ChatQuestions] ON;
INSERT INTO [dbo].[ChatQuestions] ([Id], [Question], [Role], [AiAnswered], [CreatedAtUtc]) VALUES (1, N'Can I connect my smartwatch to HealthTrack?', N'Client', 0, '2026-10-24T17:08:44.3915674');
INSERT INTO [dbo].[ChatQuestions] ([Id], [Question], [Role], [AiAnswered], [CreatedAtUtc]) VALUES (2, N'what are the functions', N'Admin', 0, '2026-11-03T17:05:23.2464737');
SET IDENTITY_INSERT [dbo].[ChatQuestions] OFF;
GO
-- [dbo].[ClientTrainerAssignments] (6 rows)
INSERT INTO [dbo].[ClientTrainerAssignments] ([ClientId], [TrainerId], [AssignedAtUtc]) VALUES (N'0335c890-3be6-4566-a5b0-7213527bea12', N'db83af30-11fa-4541-8750-304a72e21ca8', '2026-10-23T15:34:52.4208464');
INSERT INTO [dbo].[ClientTrainerAssignments] ([ClientId], [TrainerId], [AssignedAtUtc]) VALUES (N'1539dea9-3d60-454d-bf56-622240e8c807', N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-22T17:41:50.7960582');
INSERT INTO [dbo].[ClientTrainerAssignments] ([ClientId], [TrainerId], [AssignedAtUtc]) VALUES (N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'26301733-5473-43dc-b2e6-6c64d569bf94', '2026-10-24T06:31:55.9333064');
INSERT INTO [dbo].[ClientTrainerAssignments] ([ClientId], [TrainerId], [AssignedAtUtc]) VALUES (N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'26301733-5473-43dc-b2e6-6c64d569bf94', '2026-10-23T15:34:52.2845279');
INSERT INTO [dbo].[ClientTrainerAssignments] ([ClientId], [TrainerId], [AssignedAtUtc]) VALUES (N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'8f12a9fa-4f0c-4419-918e-50f3c0053c5d', '2026-10-23T15:34:52.5597631');
INSERT INTO [dbo].[ClientTrainerAssignments] ([ClientId], [TrainerId], [AssignedAtUtc]) VALUES (N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'db83af30-11fa-4541-8750-304a72e21ca8', '2026-10-24T20:28:29.3978074');
GO
-- [dbo].[ConsultationProviders] (11 rows)
SET IDENTITY_INSERT [dbo].[ConsultationProviders] ON;
INSERT INTO [dbo].[ConsultationProviders] ([Id], [Name], [Specialty], [Kind], [IsActive], [PersonId]) VALUES (1, N'Coach Alex Rivera', N'Strength, Conditioning & Hypertrophy', 0, 1, N'a8995675-309a-4237-adcb-2b86a331a936');
INSERT INTO [dbo].[ConsultationProviders] ([Id], [Name], [Specialty], [Kind], [IsActive], [PersonId]) VALUES (2, N'Dr. Maya Fernando', N'Sports Medicine', 1, 1, NULL);
INSERT INTO [dbo].[ConsultationProviders] ([Id], [Name], [Specialty], [Kind], [IsActive], [PersonId]) VALUES (3, N'Nadia Perera', N'Registered Dietitian', 1, 1, NULL);
INSERT INTO [dbo].[ConsultationProviders] ([Id], [Name], [Specialty], [Kind], [IsActive], [PersonId]) VALUES (4, N'Dr. Sam Wijesinghe', N'Physiotherapy', 1, 1, NULL);
INSERT INTO [dbo].[ConsultationProviders] ([Id], [Name], [Specialty], [Kind], [IsActive], [PersonId]) VALUES (5, N'Priya Nair', N'Yoga, Mobility & Flexibility', 0, 1, N'8f12a9fa-4f0c-4419-918e-50f3c0053c5d');
INSERT INTO [dbo].[ConsultationProviders] ([Id], [Name], [Specialty], [Kind], [IsActive], [PersonId]) VALUES (6, N'Marcus Chen', N'Weight Loss & Cardio Conditioning', 0, 1, N'db83af30-11fa-4541-8750-304a72e21ca8');
INSERT INTO [dbo].[ConsultationProviders] ([Id], [Name], [Specialty], [Kind], [IsActive], [PersonId]) VALUES (7, N'Sofia Alvarez', N'Nutrition Coaching & Functional Training', 0, 1, N'26301733-5473-43dc-b2e6-6c64d569bf94');
INSERT INTO [dbo].[ConsultationProviders] ([Id], [Name], [Specialty], [Kind], [IsActive], [PersonId]) VALUES (8, N'Dr. Arjun Menon', N'Practitioner', 1, 1, NULL);
INSERT INTO [dbo].[ConsultationProviders] ([Id], [Name], [Specialty], [Kind], [IsActive], [PersonId]) VALUES (9, N'Dr. Hana Yusuf', N'Cardiology', 1, 1, NULL);
INSERT INTO [dbo].[ConsultationProviders] ([Id], [Name], [Specialty], [Kind], [IsActive], [PersonId]) VALUES (10, N'Ruwan Jayasuriya', N'Sports Psychologist', 1, 1, NULL);
INSERT INTO [dbo].[ConsultationProviders] ([Id], [Name], [Specialty], [Kind], [IsActive], [PersonId]) VALUES (12, N'Kamel', N'Fitness & Strength Conditioning', 0, 1, N'343bc79d-db62-4be4-b01f-1f5c9bd3ec0a');
SET IDENTITY_INSERT [dbo].[ConsultationProviders] OFF;
GO
-- [dbo].[DietPlanMeals] (4 rows)
SET IDENTITY_INSERT [dbo].[DietPlanMeals] ON;
INSERT INTO [dbo].[DietPlanMeals] ([Id], [DietPlanId], [MealType], [FoodRecommendations], [ApproximateCalories], [Notes], [Day], [Time]) VALUES (1, 1, N'Breakfast', N'Oatmeal with sliced banana, protein shake', 500, NULL, NULL, NULL);
INSERT INTO [dbo].[DietPlanMeals] ([Id], [DietPlanId], [MealType], [FoodRecommendations], [ApproximateCalories], [Notes], [Day], [Time]) VALUES (2, 1, N'Lunch', N'Grilled chicken breast, brown rice, mixed greens', 750, NULL, NULL, NULL);
INSERT INTO [dbo].[DietPlanMeals] ([Id], [DietPlanId], [MealType], [FoodRecommendations], [ApproximateCalories], [Notes], [Day], [Time]) VALUES (3, 1, N'Dinner', N'Baked salmon, sweet potato, steamed broccoli', 650, NULL, NULL, NULL);
INSERT INTO [dbo].[DietPlanMeals] ([Id], [DietPlanId], [MealType], [FoodRecommendations], [ApproximateCalories], [Notes], [Day], [Time]) VALUES (4, 1, N'Snack', N'Greek yogurt with a handful of almonds', 300, NULL, NULL, NULL);
SET IDENTITY_INSERT [dbo].[DietPlanMeals] OFF;
GO
-- [dbo].[DietPlans] (1 rows)
SET IDENTITY_INSERT [dbo].[DietPlans] ON;
INSERT INTO [dbo].[DietPlans] ([Id], [TrainerId], [ClientId], [Title], [DailyCalorieTarget], [TargetProteinGrams], [TargetCarbsGrams], [TargetFatGrams], [Description], [StartDate], [EndDate], [IsActive], [CreatedAtUtc]) VALUES (1, N'a8995675-309a-4237-adcb-2b86a331a936', N'1539dea9-3d60-454d-bf56-622240e8c807', N'Callory Burn', 2200, 150, 220, 65, NULL, '2026-10-23T00:00:00.0000000', NULL, 0, '2026-10-22T18:33:08.8002522');
SET IDENTITY_INSERT [dbo].[DietPlans] OFF;
GO
-- [dbo].[HealthMetrics] (15 rows)
SET IDENTITY_INSERT [dbo].[HealthMetrics] ON;
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (1, N'1539dea9-3d60-454d-bf56-622240e8c807', 70, '2026-10-22T22:51:16.0939643', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (3, N'1539dea9-3d60-454d-bf56-622240e8c807', 80, '2026-10-23T19:32:00.0000000', 20, NULL, NULL, NULL, NULL, NULL);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (5, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', 68.5, '2026-10-02T21:04:51.8918788', 26, 80, 74, NULL, NULL, 122);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (6, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', 65.2, '2026-10-23T19:04:51.9346422', 24.5, 78, 70, 7, 8200, 118);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (7, N'16f3121b-cf0d-40f5-88e4-85928b270b90', 70, '2026-10-02T21:04:52.1441078', 26, 80, 74, NULL, NULL, 122);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (8, N'16f3121b-cf0d-40f5-88e4-85928b270b90', 72.4, '2026-10-23T19:04:52.1442542', 24.5, 78, 70, 7, 8200, 118);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (9, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', 55, '2026-10-02T21:04:52.2823607', 26, 80, 74, NULL, NULL, 122);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (10, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', 54.1, '2026-10-23T19:04:52.2825680', 24.5, 78, 70, 7, 8200, 118);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (11, N'0335c890-3be6-4566-a5b0-7213527bea12', 92, '2026-10-02T21:04:52.4192548', 26, 80, 74, NULL, NULL, 122);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (12, N'0335c890-3be6-4566-a5b0-7213527bea12', 87.6, '2026-10-23T19:04:52.4194228', 24.5, 78, 70, 7, 8200, 118);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (13, N'69903e05-c9ee-47b5-9d58-972aaea51e78', 74, '2026-10-02T21:04:52.5584984', 26, 80, 74, NULL, NULL, 122);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (14, N'69903e05-c9ee-47b5-9d58-972aaea51e78', 71.3, '2026-10-23T19:04:52.5586566', 24.5, 78, 70, 7, 8200, 118);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (15, N'1539dea9-3d60-454d-bf56-622240e8c807', 80, '2026-10-25T00:08:00.0000000', NULL, NULL, NULL, NULL, NULL, NULL);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (1015, N'1539dea9-3d60-454d-bf56-622240e8c807', 82, '2026-10-29T21:20:00.0000000', 10, NULL, NULL, NULL, NULL, NULL);
INSERT INTO [dbo].[HealthMetrics] ([MetricId], [ClientId], [WeightKg], [RecordedAt], [BodyFatPercent], [DiastolicBp], [RestingHeartRate], [SleepHours], [Steps], [SystolicBp]) VALUES (1018, N'1539dea9-3d60-454d-bf56-622240e8c807', 82, '2026-11-03T23:09:00.0000000', 10, NULL, NULL, NULL, NULL, NULL);
SET IDENTITY_INSERT [dbo].[HealthMetrics] OFF;
GO
-- [dbo].[LoginHistories] (134 rows)
SET IDENTITY_INSERT [dbo].[LoginHistories] ON;
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-22T22:45:14.6969113', 0, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (2, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-22T22:57:58.1118241', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-22T22:59:24.9551114', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (4, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-22T23:11:50.2292636', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.26300; en-US) PowerShell/7.6.5');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (5, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-22T23:11:50.4081467', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.26300; en-US) PowerShell/7.6.5');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (6, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-22T23:11:50.5117580', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.26300; en-US) PowerShell/7.6.5');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (7, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-22T23:13:58.4690819', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (8, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-22T23:18:25.0899281', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (9, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-22T23:46:05.9724967', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (10, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T00:04:55.1397604', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (11, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T00:05:10.2696616', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (12, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T00:05:28.8197326', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (13, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T00:07:39.2008411', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (14, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T00:20:34.4473385', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.26300; en-US) PowerShell/7.6.5');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (15, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T00:21:30.1836860', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.26300; en-US) PowerShell/7.6.5');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (16, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T00:23:35.5013680', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (17, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T00:24:50.0274942', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (18, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T00:25:06.0229107', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (19, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T00:27:45.3464712', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.26300; en-US) PowerShell/7.6.5');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (20, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T00:31:06.6071701', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.26300; en-US) PowerShell/7.6.5');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (21, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T00:31:24.1608734', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (22, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T00:55:55.6522465', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (23, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T18:54:49.1712437', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (24, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T18:56:31.4368161', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (25, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T19:04:31.4640817', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (26, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T19:04:38.0161039', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (27, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T19:06:22.3224545', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (28, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T19:07:02.7481549', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (29, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T19:07:09.6758346', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (30, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T19:25:24.5459911', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (31, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T19:27:14.1121407', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (32, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T19:27:40.8234782', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (33, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T19:30:39.7537805', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (34, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T19:32:11.4257360', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (35, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T19:34:50.1291375', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (36, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T19:51:56.9550580', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (37, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T20:15:38.9042424', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (38, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-23T20:24:37.5983255', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (39, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-23T20:29:54.7772528', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (40, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-23T20:30:18.4180635', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (41, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-23T20:36:26.4336204', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (42, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-23T20:39:16.8638884', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (43, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-23T20:45:13.9512976', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (44, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-23T20:55:52.4408857', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (45, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T11:48:50.8787986', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36 Edg/155.0.0.0');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (46, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T11:49:02.6589325', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36 Edg/155.0.0.0');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (47, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T11:51:16.9190723', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (49, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T11:59:33.3990848', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (50, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T12:04:10.3358371', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (52, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T12:06:39.1359478', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (53, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T12:07:42.6090014', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36 Edg/155.0.0.0');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (54, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T12:15:33.9422319', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36 Edg/155.0.0.0');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (55, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T12:16:35.6066152', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36 Edg/155.0.0.0');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (56, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T12:16:51.2730596', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36 Edg/155.0.0.0');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (57, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T12:17:12.8685748', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36 Edg/155.0.0.0');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (58, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T12:17:36.8189920', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36 Edg/155.0.0.0');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (59, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T12:17:54.3456439', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36 Edg/155.0.0.0');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (60, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T12:20:13.5181315', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (61, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T12:21:53.8682886', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36 Edg/155.0.0.0');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (62, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T13:33:22.3530200', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1062, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T22:33:00.5077765', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1063, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T22:33:15.8592070', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1064, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T22:33:31.3350437', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1065, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T22:33:40.0859848', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1066, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T22:34:00.5457609', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1067, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T22:35:21.4622032', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1068, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T22:35:37.5355413', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1069, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T22:35:47.8373154', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1070, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T22:37:15.0450372', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1071, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T22:37:53.4355188', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1072, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T22:38:00.9440928', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1073, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T22:38:06.1795620', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1074, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T22:38:37.0156317', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1075, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T22:38:41.2323417', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1076, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T22:44:37.6494098', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1077, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T22:46:17.6283086', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1078, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T22:46:43.1783380', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1079, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T22:51:06.9388129', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1080, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T22:51:22.6085371', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1081, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T22:51:58.2081701', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1082, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T22:52:12.0726013', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1083, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T22:52:53.4494539', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1084, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T22:52:56.6403755', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1085, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T22:52:59.5552533', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1086, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T22:53:31.5896008', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1087, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T22:53:35.0514109', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1088, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T22:53:38.4684994', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (1089, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T22:55:12.3383454', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (2062, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-24T23:35:51.8885164', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (2063, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-24T23:36:26.6172341', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (2064, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-24T23:36:40.7324840', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3062, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-25T00:08:34.9723817', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3063, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-25T00:09:24.1321901', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3064, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-25T00:09:38.3250463', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3065, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-25T00:10:15.5913900', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3066, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-25T00:10:42.3592915', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3067, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-25T00:10:45.7455601', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3068, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-25T00:11:25.4191172', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3069, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-25T00:11:59.7658195', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3070, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-25T00:12:06.9289717', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3071, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-25T00:12:37.7393134', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3072, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-25T00:12:40.4973346', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3073, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-25T00:12:57.9480113', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3074, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-25T00:13:20.3192478', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3075, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-25T00:13:24.3670414', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (3076, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-25T00:14:02.6005599', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (4062, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-25T00:51:10.3872376', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (4063, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-25T00:51:21.5238679', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (4064, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-25T00:51:43.2173649', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (4065, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-25T01:02:08.0090051', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (4066, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-25T02:03:18.2831592', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (4067, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-25T02:08:00.4264371', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (4068, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-25T02:23:07.9712086', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (4069, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-25T02:47:23.4179860', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (5069, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-29T20:43:38.8667747', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (5070, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-29T20:45:18.7548465', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (6069, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-29T20:55:24.5727541', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (7069, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-29T21:22:57.4587139', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (7070, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-29T21:23:31.0909418', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (7071, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-10-29T22:15:41.5514493', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (7072, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-10-29T22:45:20.7095674', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (7073, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-03T20:00:57.2335938', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (8073, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-11-03T23:08:49.0382958', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (8074, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-03T23:10:18.3622200', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (8075, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-11-03T23:10:25.2581408', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (8076, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-03T23:10:50.8495386', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (8077, N'a8995675-309a-4237-adcb-2b86a331a936', '2026-11-03T23:10:59.8429478', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (8078, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-03T23:11:26.7613430', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (8079, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-11-03T23:11:59.5151315', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (8080, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-03T23:12:10.0757484', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (8081, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-11-03T23:14:25.3399445', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (8082, N'1539dea9-3d60-454d-bf56-622240e8c807', '2026-11-04T20:25:28.2503442', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (9082, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-05T18:48:28.8801642', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/157.0.0.0 Safari/537.36');
INSERT INTO [dbo].[LoginHistories] ([Id], [PersonId], [LoggedInAt], [Succeeded], [IpAddress], [UserAgent]) VALUES (10082, N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', '2026-11-06T20:36:16.8329402', 1, N'::1', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/157.0.0.0 Safari/537.36');
SET IDENTITY_INSERT [dbo].[LoginHistories] OFF;
GO
-- [dbo].[MealLogs] (3 rows)
SET IDENTITY_INSERT [dbo].[MealLogs] ON;
INSERT INTO [dbo].[MealLogs] ([MealLogId], [ClientId], [MealType], [Calories], [FoodItems], [LoggedAt], [DietPlanMealId]) VALUES (1, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Lunch', 10, NULL, '2026-10-23T20:02:00.0000000', NULL);
INSERT INTO [dbo].[MealLogs] ([MealLogId], [ClientId], [MealType], [Calories], [FoodItems], [LoggedAt], [DietPlanMealId]) VALUES (2, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Breakfast', 500, N'Oatmeal with sliced banana, protein shake', '2026-10-24T12:22:00.0000000', 1);
INSERT INTO [dbo].[MealLogs] ([MealLogId], [ClientId], [MealType], [Calories], [FoodItems], [LoggedAt], [DietPlanMealId]) VALUES (3, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Breakfast', 400, N'Oatmeal with, protein shake', '2026-10-29T21:19:00.0000000', 1);
SET IDENTITY_INSERT [dbo].[MealLogs] OFF;
GO
-- [dbo].[Notifications] (138 rows)
SET IDENTITY_INSERT [dbo].[Notifications] ON;
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1, N'1539dea9-3d60-454d-bf56-622240e8c807', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 1, '2026-10-22T22:51:16.2834443', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Don''t forget to log your meals for today.', N'Meal', 1, '2026-10-22T22:51:16.3020941', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Coach Alex Rivera is now your Trainer.', N'General', 1, '2026-10-22T23:11:50.8341285', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4, N'a8995675-309a-4237-adcb-2b86a331a936', N'Demo Client has been assigned to you.', N'General', 1, '2026-10-22T23:11:50.8376579', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Your Trainer assigned a new diet plan: Callory Burn', N'Meal', 1, '2026-10-23T00:03:08.8793428', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (6, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Your Trainer assigned a new workout plan: Weekly Plan', N'Workout', 1, '2026-10-23T00:24:13.6791048', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (7, N'1539dea9-3d60-454d-bf56-622240e8c807', N'New Trainer Feedback received: ''Feedback''', N'General', 1, '2026-10-23T19:25:59.9975942', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (8, N'1539dea9-3d60-454d-bf56-622240e8c807', N'New Trainer Feedback received: ''Feedback''', N'General', 1, '2026-10-23T19:27:05.7154364', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (9, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Your Trainer assigned a new workout plan: 4 Weeks', N'Workout', 1, '2026-10-23T19:51:45.0955020', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (10, N'1539dea9-3d60-454d-bf56-622240e8c807', N'You have a Great News', N'General', 1, '2026-10-23T20:29:39.7112207', '2026-10-23T20:30:00.0000000', NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (11, N'1539dea9-3d60-454d-bf56-622240e8c807', N'test', N'General', 1, '2026-10-23T20:56:45.2731695', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (12, N'0335c890-3be6-4566-a5b0-7213527bea12', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-23T21:04:53.1762122', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (13, N'0335c890-3be6-4566-a5b0-7213527bea12', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-23T21:04:53.2239080', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (14, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-23T21:04:53.3486585', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (15, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-23T21:04:53.3502956', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (16, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-23T21:04:53.3607631', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (17, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-23T21:04:53.3619052', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (18, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-23T21:04:53.3716955', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (19, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-23T21:04:53.3729461', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (20, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-23T21:04:53.3830707', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (21, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-23T21:04:53.3842222', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (22, N'0335c890-3be6-4566-a5b0-7213527bea12', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-24T11:46:41.1256300', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (23, N'0335c890-3be6-4566-a5b0-7213527bea12', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-24T11:46:41.1813172', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (24, N'1539dea9-3d60-454d-bf56-622240e8c807', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 1, '2026-10-24T11:46:41.4338885', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (25, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Don''t forget to log your meals for today.', N'Meal', 1, '2026-10-24T11:46:41.4360755', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (26, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-24T11:46:41.4569475', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (27, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-24T11:46:41.4589823', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (28, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-24T11:46:41.4800128', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (29, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-24T11:46:41.4833924', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (30, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-24T11:46:41.5039906', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (31, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-24T11:46:41.5061440', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (32, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-24T11:46:41.5228111', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (33, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-24T11:46:41.5244458', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (34, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'Sofia Alvarez is now your Trainer.', N'General', 0, '2026-10-24T12:01:55.9796851', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (35, N'26301733-5473-43dc-b2e6-6c64d569bf94', N'Liam Fernando has been assigned to you.', N'General', 0, '2026-10-24T12:01:56.0370620', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (36, N'db83af30-11fa-4541-8750-304a72e21ca8', N'Liam Fernando is no longer assigned to you.', N'General', 0, '2026-10-24T12:01:56.0441316', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (37, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Aadhil is now your Trainer.', N'General', 0, '2026-10-24T12:02:38.2069261', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (39, N'8f12a9fa-4f0c-4419-918e-50f3c0053c5d', N'Aisha Khan is no longer assigned to you.', N'General', 0, '2026-10-24T12:02:38.2204940', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (41, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Your consultation appointment on Oct 25, 2026 at 09:00 AM was APPROVED by your Trainer.', N'Appointment', 1, '2026-10-24T12:16:41.9515951', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (43, N'a8995675-309a-4237-adcb-2b86a331a936', N'Demo Client asked to reschedule the consultation on Oct 25, 2026 at 09:00 AM. Review it on your Consultations page.', N'Appointment', 0, '2026-10-24T12:17:08.4780023', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (44, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Your trainer ACCEPTED your request to reschedule the consultation on Oct 25, 2026 at 09:00 AM. Open the appointment to choose a new time.', N'Appointment', 1, '2026-10-24T12:17:20.5389517', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (45, N'a8995675-309a-4237-adcb-2b86a331a936', N'Demo Client moved their consultation from Oct 25 at 09:00 AM to Oct 25, 2026 at 09:00 AM.', N'Appointment', 0, '2026-10-24T12:17:48.3975992', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (46, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Your consultation appointment on Oct 30, 2026 at 10:00 AM was CANCELLED by your Trainer.', N'Appointment', 0, '2026-10-25T00:13:21.8676576', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1046, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Marcus Chen is now your Trainer.', N'General', 0, '2026-10-25T01:58:29.4418300', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1047, N'db83af30-11fa-4541-8750-304a72e21ca8', N'Aisha Khan has been assigned to you.', N'General', 0, '2026-10-25T01:58:29.4611807', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1049, N'0335c890-3be6-4566-a5b0-7213527bea12', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-25T18:38:19.2955552', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1050, N'0335c890-3be6-4566-a5b0-7213527bea12', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-25T18:38:19.3406683', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1051, N'1539dea9-3d60-454d-bf56-622240e8c807', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-25T18:38:19.5926523', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1052, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-25T18:38:19.5959771', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1053, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-25T18:38:19.6520955', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1054, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-25T18:38:19.6557135', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1055, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-25T18:38:19.7323988', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1056, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-25T18:38:19.7364898', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1057, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-25T18:38:19.7957335', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1058, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-25T18:38:19.8002031', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1059, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-25T18:38:19.8420485', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1060, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-25T18:38:19.8453626', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1061, N'0335c890-3be6-4566-a5b0-7213527bea12', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-29T20:25:43.8246834', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1062, N'0335c890-3be6-4566-a5b0-7213527bea12', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-29T20:25:43.9158230', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1063, N'1539dea9-3d60-454d-bf56-622240e8c807', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-29T20:25:44.2333693', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1064, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-29T20:25:44.2349894', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1065, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-29T20:25:44.2532971', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1066, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-29T20:25:44.2552388', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1067, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-29T20:25:44.2701688', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1068, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-29T20:25:44.2717913', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1069, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-29T20:25:44.2863523', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1070, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-29T20:25:44.2878857', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1071, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-10-29T20:25:44.3019404', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (1072, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-10-29T20:25:44.3036736', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2061, N'a8995675-309a-4237-adcb-2b86a331a936', N'Your client replied to your feedback "Feedback".', N'General', 0, '2026-10-29T21:25:43.7982872', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2063, N'0335c890-3be6-4566-a5b0-7213527bea12', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-03T19:44:03.9781945', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2064, N'0335c890-3be6-4566-a5b0-7213527bea12', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-03T19:44:04.0222153', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2065, N'0335c890-3be6-4566-a5b0-7213527bea12', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-03T19:44:04.0448200', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2066, N'1539dea9-3d60-454d-bf56-622240e8c807', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-03T19:44:04.1391322', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2067, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-03T19:44:04.1397188', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2068, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-03T19:44:04.1564838', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2069, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-03T19:44:04.1570552', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2070, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-03T19:44:04.1575574', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2071, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-03T19:44:04.1609845', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2072, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-03T19:44:04.1615369', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2073, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-03T19:44:04.1620247', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2074, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-03T19:44:04.1651745', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2075, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-03T19:44:04.1658156', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2076, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-03T19:44:04.1663007', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2077, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-03T19:44:04.1691014', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2078, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-03T19:44:04.1695990', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (2079, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-03T19:44:04.1700758', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3063, N'0335c890-3be6-4566-a5b0-7213527bea12', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-04T08:13:36.0499614', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3064, N'0335c890-3be6-4566-a5b0-7213527bea12', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-04T08:13:36.1189287', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3065, N'0335c890-3be6-4566-a5b0-7213527bea12', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-04T08:13:36.1254828', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3066, N'1539dea9-3d60-454d-bf56-622240e8c807', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-04T08:13:36.2461851', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3067, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-04T08:13:36.2484289', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3068, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-04T08:13:36.2711679', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3069, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-04T08:13:36.2737084', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3070, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-04T08:13:36.2760705', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3071, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-04T08:13:36.2999401', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3072, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-04T08:13:36.3019886', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3073, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-04T08:13:36.3045373', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3074, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-04T08:13:36.3234447', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3075, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-04T08:13:36.3262373', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3076, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-04T08:13:36.3288311', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3077, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-04T08:13:36.3478254', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3078, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-04T08:13:36.3501202', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (3079, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-04T08:13:36.3524113', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4063, N'0335c890-3be6-4566-a5b0-7213527bea12', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-05T18:22:17.8015262', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4064, N'0335c890-3be6-4566-a5b0-7213527bea12', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-05T18:22:17.8015262', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4065, N'0335c890-3be6-4566-a5b0-7213527bea12', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-05T18:22:17.8015262', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4066, N'1539dea9-3d60-454d-bf56-622240e8c807', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-05T18:22:18.2802661', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4067, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-05T18:22:18.2802661', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4068, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-05T18:22:18.3147467', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4069, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-05T18:22:18.3147467', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4070, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-05T18:22:18.3147467', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4071, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-05T18:22:18.3363613', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4072, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-05T18:22:18.3363613', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4073, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-05T18:22:18.3363613', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4074, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-05T18:22:18.3521154', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4075, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-05T18:22:18.3521154', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4076, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-05T18:22:18.3521154', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4077, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-05T18:22:18.3675154', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4078, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-05T18:22:18.3675154', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (4079, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-05T18:22:18.3675154', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5063, N'0335c890-3be6-4566-a5b0-7213527bea12', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-06T18:03:21.3291614', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5064, N'0335c890-3be6-4566-a5b0-7213527bea12', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-06T18:03:21.3291614', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5065, N'0335c890-3be6-4566-a5b0-7213527bea12', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-06T18:03:21.3291614', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5066, N'1539dea9-3d60-454d-bf56-622240e8c807', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-06T18:03:21.9171157', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5067, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-06T18:03:21.9171157', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5068, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-06T18:03:21.9499392', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5069, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-06T18:03:21.9499392', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5070, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-06T18:03:21.9499392', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5071, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-06T18:03:21.9632197', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5072, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-06T18:03:21.9632197', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5073, N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-06T18:03:21.9632197', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5074, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-06T18:03:21.9767244', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5075, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-06T18:03:21.9767244', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5076, N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-06T18:03:21.9767244', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5077, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'You haven''t logged a workout today. Keep your streak going!', N'Workout', 0, '2026-11-06T18:03:21.9893850', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5078, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Don''t forget to log your meals for today.', N'Meal', 0, '2026-11-06T18:03:21.9893850', NULL, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Notifications] ([NotificationId], [PersonId], [Message], [Type], [IsRead], [CreatedAt], [ScheduledFor], [AppointmentId], [AppointmentStartUtc], [IsDismissed], [AdminBroadcastId]) VALUES (5079, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'It''s been over a week since your last weight check-in. Record a new measurement.', N'General', 0, '2026-11-06T18:03:21.9893850', NULL, NULL, NULL, 0, NULL);
SET IDENTITY_INSERT [dbo].[Notifications] OFF;
GO
-- [dbo].[RescheduleRequests] (1 rows)
SET IDENTITY_INSERT [dbo].[RescheduleRequests] ON;
INSERT INTO [dbo].[RescheduleRequests] ([Id], [AppointmentId], [Reason], [Status], [TrainerNote], [RequestedAtUtc], [DecidedAtUtc]) VALUES (1, 1, N'I have no time', 3, NULL, '2026-10-24T06:47:08.4481416', '2026-10-24T06:47:20.5315288');
SET IDENTITY_INSERT [dbo].[RescheduleRequests] OFF;
GO
-- [dbo].[Roles] (3 rows)
INSERT INTO [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'01b2d6cc-43bf-4917-ace6-d1dc0f1a0ea8', N'Trainer', N'TRAINER', NULL);
INSERT INTO [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'8583cdaf-a61a-405e-844c-defd5882fff6', N'Admin', N'ADMIN', NULL);
INSERT INTO [dbo].[Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'a3c6ab12-e916-4190-a2cd-956af51687f0', N'Client', N'CLIENT', NULL);
GO
-- [dbo].[TrainerAssignmentHistories] (3 rows)
SET IDENTITY_INSERT [dbo].[TrainerAssignmentHistories] ON;
INSERT INTO [dbo].[TrainerAssignmentHistories] ([Id], [ClientId], [TrainerId], [AssignedAtUtc], [EndedAtUtc]) VALUES (1, N'1539dea9-3d60-454d-bf56-622240e8c807', N'a8995675-309a-4237-adcb-2b86a331a936', '2026-10-22T17:41:50.7960582', NULL);
INSERT INTO [dbo].[TrainerAssignmentHistories] ([Id], [ClientId], [TrainerId], [AssignedAtUtc], [EndedAtUtc]) VALUES (2, N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'26301733-5473-43dc-b2e6-6c64d569bf94', '2026-10-24T06:31:55.9333064', NULL);
INSERT INTO [dbo].[TrainerAssignmentHistories] ([Id], [ClientId], [TrainerId], [AssignedAtUtc], [EndedAtUtc]) VALUES (4, N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'db83af30-11fa-4541-8750-304a72e21ca8', '2026-10-24T20:28:29.3978074', NULL);
SET IDENTITY_INSERT [dbo].[TrainerAssignmentHistories] OFF;
GO
-- [dbo].[TrainerFeedbacks] (2 rows)
SET IDENTITY_INSERT [dbo].[TrainerFeedbacks] ON;
INSERT INTO [dbo].[TrainerFeedbacks] ([Id], [TrainerId], [ClientId], [Title], [Comments], [Category], [CreatedAtUtc], [IsReadByClient], [ClientRepliedAtUtc], [ClientReply]) VALUES (1, N'a8995675-309a-4237-adcb-2b86a331a936', N'1539dea9-3d60-454d-bf56-622240e8c807', N'Feedback', N'Good', N'General', '2026-10-23T13:55:59.9358807', 1, '2026-10-29T15:55:43.7925684', N'Keep Going,');
INSERT INTO [dbo].[TrainerFeedbacks] ([Id], [TrainerId], [ClientId], [Title], [Comments], [Category], [CreatedAtUtc], [IsReadByClient], [ClientRepliedAtUtc], [ClientReply]) VALUES (2, N'a8995675-309a-4237-adcb-2b86a331a936', N'1539dea9-3d60-454d-bf56-622240e8c807', N'Feedback', N'Test', N'General', '2026-10-23T13:57:05.7095535', 1, '2026-10-23T13:57:30.5450372', N'Thank You');
SET IDENTITY_INSERT [dbo].[TrainerFeedbacks] OFF;
GO
-- [dbo].[UserRoles] (12 rows)
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'26301733-5473-43dc-b2e6-6c64d569bf94', N'01b2d6cc-43bf-4917-ace6-d1dc0f1a0ea8');
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'343bc79d-db62-4be4-b01f-1f5c9bd3ec0a', N'01b2d6cc-43bf-4917-ace6-d1dc0f1a0ea8');
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'8f12a9fa-4f0c-4419-918e-50f3c0053c5d', N'01b2d6cc-43bf-4917-ace6-d1dc0f1a0ea8');
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'a8995675-309a-4237-adcb-2b86a331a936', N'01b2d6cc-43bf-4917-ace6-d1dc0f1a0ea8');
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'db83af30-11fa-4541-8750-304a72e21ca8', N'01b2d6cc-43bf-4917-ace6-d1dc0f1a0ea8');
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'8583cdaf-a61a-405e-844c-defd5882fff6');
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'0335c890-3be6-4566-a5b0-7213527bea12', N'a3c6ab12-e916-4190-a2cd-956af51687f0');
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'1539dea9-3d60-454d-bf56-622240e8c807', N'a3c6ab12-e916-4190-a2cd-956af51687f0');
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'a3c6ab12-e916-4190-a2cd-956af51687f0');
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'a3c6ab12-e916-4190-a2cd-956af51687f0');
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'a3c6ab12-e916-4190-a2cd-956af51687f0');
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'a3c6ab12-e916-4190-a2cd-956af51687f0');
GO
-- [dbo].[Users] (12 rows)
INSERT INTO [dbo].[Users] ([Id], [Name], [DateOfBirth], [CreatedAt], [LastLoginAt], [Role], [AccessLevel], [HeightCm], [FitnessGoal], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePhoto], [ProfilePhotoContentType], [MealReminderEnabled], [MealReminderHour], [WeightReminderEnabled], [WeightReminderHour], [WorkoutReminderEnabled], [WorkoutReminderHour], [EmailNotificationsEnabled], [TargetDate], [TargetWeightKg], [MustChangePassword], [TemporaryPasswordExpiresAtUtc]) VALUES (N'0335c890-3be6-4566-a5b0-7213527bea12', N'Daniel Silva', '1987-06-05T00:00:00.0000000', '2026-10-23T21:04:52.2898000', NULL, N'Client', NULL, 182, N'Lower resting heart rate', N'daniel.silva@healthtrack.com', N'DANIEL.SILVA@HEALTHTRACK.COM', N'daniel.silva@healthtrack.com', N'DANIEL.SILVA@HEALTHTRACK.COM', 1, N'AQAAAAIAAYagAAAAEJMV58Ge54ljnh9GTj7KDODatO5W/CSXZKXETneCbmLGh4tXG1l+AFisUFb+ajNpSQ==', N'SVICW3RWHRGRQ45OJDDHN3JMY4A5CE2D', N'6e6665ca-6885-4e52-9262-b92c008753d8', NULL, 0, 0, NULL, 1, 0, NULL, NULL, 1, 8, 1, 8, 1, 8, 1, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Users] ([Id], [Name], [DateOfBirth], [CreatedAt], [LastLoginAt], [Role], [AccessLevel], [HeightCm], [FitnessGoal], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePhoto], [ProfilePhotoContentType], [MealReminderEnabled], [MealReminderHour], [WeightReminderEnabled], [WeightReminderHour], [WorkoutReminderEnabled], [WorkoutReminderHour], [EmailNotificationsEnabled], [TargetDate], [TargetWeightKg], [MustChangePassword], [TemporaryPasswordExpiresAtUtc]) VALUES (N'1539dea9-3d60-454d-bf56-622240e8c807', N'Demo Client', '2000-01-01T00:00:00.0000000', '2026-10-22T22:51:16.0938210', '2026-11-04T20:25:28.5891738', N'Client', NULL, 170, N'Improve general fitness', N'client@healthtrack.com', N'CLIENT@HEALTHTRACK.COM', N'client@healthtrack.com', N'CLIENT@HEALTHTRACK.COM', 0, N'AQAAAAIAAYagAAAAELXPc+y++LkN44ayeBdAAKflRTe1y6yA+DdVA69vAvbCT3O8fg0TxKDg+j63G2T+hA==', N'E5058940-98CA-4FDA-ADE1-A1FAC66AA54C', N'b8b4328b-2413-448a-bcf8-3bc71aef5e26', NULL, 0, 0, NULL, 1, 0, NULL, NULL, 1, 8, 1, 8, 1, 8, 1, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Users] ([Id], [Name], [DateOfBirth], [CreatedAt], [LastLoginAt], [Role], [AccessLevel], [HeightCm], [FitnessGoal], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePhoto], [ProfilePhotoContentType], [MealReminderEnabled], [MealReminderHour], [WeightReminderEnabled], [WeightReminderHour], [WorkoutReminderEnabled], [WorkoutReminderHour], [EmailNotificationsEnabled], [TargetDate], [TargetWeightKg], [MustChangePassword], [TemporaryPasswordExpiresAtUtc]) VALUES (N'16f3121b-cf0d-40f5-88e4-85928b270b90', N'Liam Fernando', '1995-09-30T00:00:00.0000000', '2026-10-23T21:04:52.0161117', NULL, N'Client', NULL, 178, N'Build muscle and improve strength', N'liam.fernando@healthtrack.com', N'LIAM.FERNANDO@HEALTHTRACK.COM', N'liam.fernando@healthtrack.com', N'LIAM.FERNANDO@HEALTHTRACK.COM', 1, N'AQAAAAIAAYagAAAAEIRGpPxllmJ4phC3p8samFfURMHEIvEzIGQ6TykLYMDI8i+lZyK/X6fhQ8CDRZV0dg==', N'QRYVXRUSCBA5MI4KTAS2ACHF2N4EC4KE', N'83c3cd1f-06dd-40b8-af01-c250e1e77998', NULL, 0, 0, NULL, 1, 0, NULL, NULL, 1, 8, 1, 8, 1, 8, 1, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Users] ([Id], [Name], [DateOfBirth], [CreatedAt], [LastLoginAt], [Role], [AccessLevel], [HeightCm], [FitnessGoal], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePhoto], [ProfilePhotoContentType], [MealReminderEnabled], [MealReminderHour], [WeightReminderEnabled], [WeightReminderHour], [WorkoutReminderEnabled], [WorkoutReminderHour], [EmailNotificationsEnabled], [TargetDate], [TargetWeightKg], [MustChangePassword], [TemporaryPasswordExpiresAtUtc]) VALUES (N'26301733-5473-43dc-b2e6-6c64d569bf94', N'Sofia Alvarez', '1994-07-02T00:00:00.0000000', '2026-10-23T21:04:51.4941994', NULL, N'Trainer', NULL, NULL, NULL, N'sofia.alvarez@healthtrack.com', N'SOFIA.ALVAREZ@HEALTHTRACK.COM', N'sofia.alvarez@healthtrack.com', N'SOFIA.ALVAREZ@HEALTHTRACK.COM', 1, N'AQAAAAIAAYagAAAAELtqL0ZaG7xzPEmZ/w/Fg9aKZpThK6megpX1F4KgBjfsoJxQ2DiylCjVL+6ZD5/lAw==', N'24RNQOF6NN2ANZAJ54IIMGJHQ54W4V7N', N'b1e18dbc-e7fe-4cd2-b108-378f57cba315', NULL, 0, 0, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Users] ([Id], [Name], [DateOfBirth], [CreatedAt], [LastLoginAt], [Role], [AccessLevel], [HeightCm], [FitnessGoal], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePhoto], [ProfilePhotoContentType], [MealReminderEnabled], [MealReminderHour], [WeightReminderEnabled], [WeightReminderHour], [WorkoutReminderEnabled], [WorkoutReminderHour], [EmailNotificationsEnabled], [TargetDate], [TargetWeightKg], [MustChangePassword], [TemporaryPasswordExpiresAtUtc]) VALUES (N'343bc79d-db62-4be4-b01f-1f5c9bd3ec0a', N'Kamel', '2000-10-24T00:00:00.0000000', '2026-10-29T22:47:58.3540212', NULL, N'Trainer', NULL, NULL, NULL, N'kamel123@gmail.com', N'KAMEL123@GMAIL.COM', N'kamel123@gmail.com', N'KAMEL123@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAEFz+5ZXMx6nCGJJkW5c1f4FlAe1KHOyVL+rNy+CV/+/8SIUeo05wKbTLA2Ba4IGrqg==', N'GMYSPDAGEWIBV6TLOH7XWQAZUAUOTUFK', N'a0822803-ad4b-474c-a80f-92696b4d61fc', NULL, 0, 0, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Users] ([Id], [Name], [DateOfBirth], [CreatedAt], [LastLoginAt], [Role], [AccessLevel], [HeightCm], [FitnessGoal], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePhoto], [ProfilePhotoContentType], [MealReminderEnabled], [MealReminderHour], [WeightReminderEnabled], [WeightReminderHour], [WorkoutReminderEnabled], [WorkoutReminderHour], [EmailNotificationsEnabled], [TargetDate], [TargetWeightKg], [MustChangePassword], [TemporaryPasswordExpiresAtUtc]) VALUES (N'5bf3f2cd-73c9-4671-b1c2-2e2b2d3289c8', N'Nethmi Perera', '2000-01-12T00:00:00.0000000', '2026-10-23T21:04:52.1498640', NULL, N'Client', NULL, 158, N'Run a 10K', N'nethmi.perera@healthtrack.com', N'NETHMI.PERERA@HEALTHTRACK.COM', N'nethmi.perera@healthtrack.com', N'NETHMI.PERERA@HEALTHTRACK.COM', 1, N'AQAAAAIAAYagAAAAEF4WbdBHWlQcfA3L46g0jn5OwAIj+MIO0LgfwdZOb3oWVYBztIO0kSgio9igIOghvA==', N'EMJFYHYKY3O5KHHCLTHHVZ6RTNZQV3J2', N'11c5f418-db0f-41f7-96b8-24865b1425cb', NULL, 0, 0, NULL, 1, 0, NULL, NULL, 1, 8, 1, 8, 1, 8, 1, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Users] ([Id], [Name], [DateOfBirth], [CreatedAt], [LastLoginAt], [Role], [AccessLevel], [HeightCm], [FitnessGoal], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePhoto], [ProfilePhotoContentType], [MealReminderEnabled], [MealReminderHour], [WeightReminderEnabled], [WeightReminderHour], [WorkoutReminderEnabled], [WorkoutReminderHour], [EmailNotificationsEnabled], [TargetDate], [TargetWeightKg], [MustChangePassword], [TemporaryPasswordExpiresAtUtc]) VALUES (N'69903e05-c9ee-47b5-9d58-972aaea51e78', N'Maya Thomas', '1992-12-24T00:00:00.0000000', '2026-10-23T21:04:52.4257498', NULL, N'Client', NULL, 168, N'Eat healthier and sleep better', N'maya.thomas@healthtrack.com', N'MAYA.THOMAS@HEALTHTRACK.COM', N'maya.thomas@healthtrack.com', N'MAYA.THOMAS@HEALTHTRACK.COM', 1, N'AQAAAAIAAYagAAAAEK3lFu2X+gbEb35Dre755nkw+UvyfOczVVFUEIiamq5NGmiNahKVy+hU3WdJRmV3Ug==', N'NGAVVZIFM6TTKUESNDSNXNCI23WO7AEF', N'6641634d-a346-4b96-8555-3b313133f411', NULL, 0, 0, NULL, 1, 0, NULL, NULL, 1, 8, 1, 8, 1, 8, 1, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Users] ([Id], [Name], [DateOfBirth], [CreatedAt], [LastLoginAt], [Role], [AccessLevel], [HeightCm], [FitnessGoal], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePhoto], [ProfilePhotoContentType], [MealReminderEnabled], [MealReminderHour], [WeightReminderEnabled], [WeightReminderHour], [WorkoutReminderEnabled], [WorkoutReminderHour], [EmailNotificationsEnabled], [TargetDate], [TargetWeightKg], [MustChangePassword], [TemporaryPasswordExpiresAtUtc]) VALUES (N'84d73410-70c6-4dcb-a2d3-0db4d70c936e', N'System Administrator', '0001-01-01T00:00:00.0000000', '2026-10-22T22:44:00.1629033', '2026-11-06T20:36:16.8648765', N'Admin', 1, NULL, NULL, N'admin@healthtrack.com', N'ADMIN@HEALTHTRACK.COM', N'admin@healthtrack.com', N'ADMIN@HEALTHTRACK.COM', 1, N'AQAAAAIAAYagAAAAEAlP5zhM1mR1z0U1MFf09LUKZI7OrBkp29TAaejEw0YulpPQNBKFBUewD3/Z2z3cxw==', N'GX6CL2VITKCY4ZRZIJKU5ATXN6CKJMWT', N'1344e345-3586-4ccf-8e8a-d40c3a0b2b97', NULL, 0, 0, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Users] ([Id], [Name], [DateOfBirth], [CreatedAt], [LastLoginAt], [Role], [AccessLevel], [HeightCm], [FitnessGoal], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePhoto], [ProfilePhotoContentType], [MealReminderEnabled], [MealReminderHour], [WeightReminderEnabled], [WeightReminderHour], [WorkoutReminderEnabled], [WorkoutReminderHour], [EmailNotificationsEnabled], [TargetDate], [TargetWeightKg], [MustChangePassword], [TemporaryPasswordExpiresAtUtc]) VALUES (N'8f12a9fa-4f0c-4419-918e-50f3c0053c5d', N'Priya Nair', '1990-03-08T00:00:00.0000000', '2026-10-23T21:04:50.6739209', NULL, N'Trainer', NULL, NULL, NULL, N'priya.nair@healthtrack.com', N'PRIYA.NAIR@HEALTHTRACK.COM', N'priya.nair@healthtrack.com', N'PRIYA.NAIR@HEALTHTRACK.COM', 1, N'AQAAAAIAAYagAAAAEGqByr6aRex5BoUbuzxw2XN+j4HwjaXDnf1Ucueo6dVPG08nYMtkTYJlrd7Ks28P3w==', N'NFY3TP5PPNDNCWAB2IR5ZWQ7F7HLETWV', N'2a023a47-ffeb-41eb-83cf-e2784cd55b66', NULL, 0, 0, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Users] ([Id], [Name], [DateOfBirth], [CreatedAt], [LastLoginAt], [Role], [AccessLevel], [HeightCm], [FitnessGoal], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePhoto], [ProfilePhotoContentType], [MealReminderEnabled], [MealReminderHour], [WeightReminderEnabled], [WeightReminderHour], [WorkoutReminderEnabled], [WorkoutReminderHour], [EmailNotificationsEnabled], [TargetDate], [TargetWeightKg], [MustChangePassword], [TemporaryPasswordExpiresAtUtc]) VALUES (N'a8995675-309a-4237-adcb-2b86a331a936', N'Coach Alex Rivera', '1992-05-14T00:00:00.0000000', '2026-10-22T22:44:00.3898242', '2026-11-03T23:10:59.8454907', N'Trainer', NULL, NULL, NULL, N'trainer@healthtrack.com', N'TRAINER@HEALTHTRACK.COM', N'trainer@healthtrack.com', N'TRAINER@HEALTHTRACK.COM', 1, N'AQAAAAIAAYagAAAAEAdwLDYI9QbL79uOMedeWNG9bIfLtTi2GGu7o+c4Oi79GXo38SaAGIO7VHiR67ADMw==', N'NODFSJ4GKKZYGLNLDW5VOKLXZ3MDHWA4', N'b14dca60-fe58-49d6-9600-dc8ee2a6bfb6', NULL, 0, 0, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Users] ([Id], [Name], [DateOfBirth], [CreatedAt], [LastLoginAt], [Role], [AccessLevel], [HeightCm], [FitnessGoal], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePhoto], [ProfilePhotoContentType], [MealReminderEnabled], [MealReminderHour], [WeightReminderEnabled], [WeightReminderHour], [WorkoutReminderEnabled], [WorkoutReminderHour], [EmailNotificationsEnabled], [TargetDate], [TargetWeightKg], [MustChangePassword], [TemporaryPasswordExpiresAtUtc]) VALUES (N'b3745867-bbf7-4d08-a4d6-3840c7fa21e7', N'Aisha Khan', '1998-04-17T00:00:00.0000000', '2026-10-23T21:04:51.6610848', NULL, N'Client', NULL, 163, N'Lose 6 kg before December', N'aisha.khan@healthtrack.com', N'AISHA.KHAN@HEALTHTRACK.COM', N'aisha.khan@healthtrack.com', N'AISHA.KHAN@HEALTHTRACK.COM', 1, N'AQAAAAIAAYagAAAAEKlA8KVeph6ZR4r8nFKKAxvORxoqGzku2Ftan7upEJXrLccGy8rG13i8YGt2hdoGnQ==', N'LBQID6XRGVYMYZOEURSWZV235D4J2BIM', N'e86aee2e-b094-4999-9a6c-7d04feaee698', NULL, 0, 0, NULL, 1, 0, NULL, NULL, 1, 8, 1, 8, 1, 8, 1, NULL, NULL, 0, NULL);
INSERT INTO [dbo].[Users] ([Id], [Name], [DateOfBirth], [CreatedAt], [LastLoginAt], [Role], [AccessLevel], [HeightCm], [FitnessGoal], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePhoto], [ProfilePhotoContentType], [MealReminderEnabled], [MealReminderHour], [WeightReminderEnabled], [WeightReminderHour], [WorkoutReminderEnabled], [WorkoutReminderHour], [EmailNotificationsEnabled], [TargetDate], [TargetWeightKg], [MustChangePassword], [TemporaryPasswordExpiresAtUtc]) VALUES (N'db83af30-11fa-4541-8750-304a72e21ca8', N'Marcus Chen', '1988-11-21T00:00:00.0000000', '2026-10-23T21:04:51.3145693', NULL, N'Trainer', NULL, NULL, NULL, N'marcus.chen@healthtrack.com', N'MARCUS.CHEN@HEALTHTRACK.COM', N'marcus.chen@healthtrack.com', N'MARCUS.CHEN@HEALTHTRACK.COM', 1, N'AQAAAAIAAYagAAAAEOumCSmOX58pfb1ecO6D5SbVoK+RoFNmuTGxcspe6GcStqclqSfbrsSQsm0mGcioBA==', N'J2NZGXQUW7ZWPECOLDGLMCPGGTT3O52E', N'aee78d0d-e0f3-45fa-955c-d366eb0b5540', NULL, 0, 0, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, NULL, NULL, 0, NULL);
GO
-- [dbo].[WorkoutLogs] (3 rows)
SET IDENTITY_INSERT [dbo].[WorkoutLogs] ON;
INSERT INTO [dbo].[WorkoutLogs] ([WorkoutLogId], [ClientId], [Activity], [DurationMinutes], [CaloriesBurned], [LoggedAt], [WorkoutPlanItemId]) VALUES (1, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Running', 4, 50, '2026-10-23T19:59:00.0000000', NULL);
INSERT INTO [dbo].[WorkoutLogs] ([WorkoutLogId], [ClientId], [Activity], [DurationMinutes], [CaloriesBurned], [LoggedAt], [WorkoutPlanItemId]) VALUES (2, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Press', 10, 0, '2026-10-23T20:00:00.0000000', 2);
INSERT INTO [dbo].[WorkoutLogs] ([WorkoutLogId], [ClientId], [Activity], [DurationMinutes], [CaloriesBurned], [LoggedAt], [WorkoutPlanItemId]) VALUES (3, N'1539dea9-3d60-454d-bf56-622240e8c807', N'Test', 20, 20, '2026-10-23T20:08:00.0000000', 1);
SET IDENTITY_INSERT [dbo].[WorkoutLogs] OFF;
GO
-- [dbo].[WorkoutPlanItems] (2 rows)
SET IDENTITY_INSERT [dbo].[WorkoutPlanItems] ON;
INSERT INTO [dbo].[WorkoutPlanItems] ([Id], [WorkoutPlanId], [DayOfWeek], [ExerciseName], [Sets], [Reps], [DurationMinutes], [RestSeconds], [Instructions]) VALUES (1, 1, NULL, N'Test', NULL, NULL, NULL, NULL, NULL);
INSERT INTO [dbo].[WorkoutPlanItems] ([Id], [WorkoutPlanId], [DayOfWeek], [ExerciseName], [Sets], [Reps], [DurationMinutes], [RestSeconds], [Instructions]) VALUES (2, 2, 1, N'Press', 4, 3, 20, NULL, NULL);
SET IDENTITY_INSERT [dbo].[WorkoutPlanItems] OFF;
GO
-- [dbo].[WorkoutPlans] (2 rows)
SET IDENTITY_INSERT [dbo].[WorkoutPlans] ON;
INSERT INTO [dbo].[WorkoutPlans] ([Id], [TrainerId], [ClientId], [Title], [Goal], [Description], [StartDate], [EndDate], [IsActive], [CreatedAtUtc]) VALUES (1, N'a8995675-309a-4237-adcb-2b86a331a936', N'1539dea9-3d60-454d-bf56-622240e8c807', N'Weekly Plan', NULL, NULL, '2026-10-21T00:00:00.0000000', '2026-10-22T00:00:00.0000000', 1, '2026-10-22T18:54:13.6127935');
INSERT INTO [dbo].[WorkoutPlans] ([Id], [TrainerId], [ClientId], [Title], [Goal], [Description], [StartDate], [EndDate], [IsActive], [CreatedAtUtc]) VALUES (2, N'a8995675-309a-4237-adcb-2b86a331a936', N'1539dea9-3d60-454d-bf56-622240e8c807', N'4 Weeks', NULL, N'Overall Follow All', '2026-10-23T00:00:00.0000000', '2026-11-13T00:00:00.0000000', 1, '2026-10-23T14:21:45.0414873');
SET IDENTITY_INSERT [dbo].[WorkoutPlans] OFF;
GO
ALTER TABLE [dbo].[AdminBroadcasts] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[Appointments] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[AuditLogs] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[AvailabilitySlots] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[ChatFaqs] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[ChatQuestions] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[ClientTrainerAssignments] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[ConsultationProviders] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[DietPlanMeals] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[DietPlans] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[HealthMetrics] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[LoginHistories] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[MealLogs] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[NotificationEmails] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[Notifications] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[RescheduleRequests] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[RoleClaims] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[Roles] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[SystemSettings] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[TrainerAssignmentHistories] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[TrainerFeedbacks] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[UserClaims] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[UserLogins] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[UserRoles] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[Users] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[UserTokens] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[WeeklyCheckIns] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[WorkoutLogs] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[WorkoutPlanCompletions] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[WorkoutPlanItems] WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE [dbo].[WorkoutPlans] WITH CHECK CHECK CONSTRAINT ALL;
GO
