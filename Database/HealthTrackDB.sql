CREATE DATABASE [HealthTrackDB];
GO

USE [HealthTrackDB];
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


