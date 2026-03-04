-- Bangkok API - Initial schema
-- SQL Server. Primary keys: UNIQUEIDENTIFIER. Use DATETIME2, NVARCHAR.

IF NOT EXISTS (SELECT * FROM sys.[schemas] WHERE [name] = N'dbo')
    EXEC('CREATE SCHEMA dbo');

-- User table
IF OBJECT_ID(N'dbo.[User]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[User]
    (
        [Id]              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Email]           NVARCHAR(256)    NOT NULL,
        [DisplayName]     NVARCHAR(256)    NULL,
        [PasswordHash]    NVARCHAR(500)    NOT NULL,
        [PasswordSalt]    NVARCHAR(500)    NOT NULL,
        [IsActive]            BIT              NOT NULL DEFAULT 1,
        [CreatedAtUtc]        DATETIME2(7)     NOT NULL,
        [UpdatedAtUtc]        DATETIME2(7)     NULL,
        [RecoverString]       NVARCHAR(200)    NULL,
        [RecoverStringExpiry] DATETIME2(7)     NULL,
        [IsDeleted]           BIT              NOT NULL DEFAULT 0,
        [DeletedAt]           DATETIME2(7)     NULL,
        [FailedLoginAttempts] INT              NOT NULL DEFAULT 0,
        [LockoutEnd]          DATETIME2(7)     NULL,
        CONSTRAINT [UQ_User_Email] UNIQUE ([Email])
    );
    CREATE NONCLUSTERED INDEX [IX_User_Email] ON dbo.[User] ([Email]);
END;

-- RefreshToken table
IF OBJECT_ID(N'dbo.RefreshToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshToken
    (
        Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        UserId          UNIQUEIDENTIFIER NOT NULL,
        Token           NVARCHAR(500)    NOT NULL,
        ExpiresAtUtc    DATETIME2(7)     NOT NULL,
        CreatedAtUtc    DATETIME2(7)     NOT NULL,
        RevokedReason   NVARCHAR(256)    NULL,
        RevokedAtUtc    DATETIME2(7)     NULL,
        CONSTRAINT FK_RefreshToken_User FOREIGN KEY (UserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_RefreshToken_Token ON dbo.RefreshToken (Token);
    CREATE NONCLUSTERED INDEX IX_RefreshToken_UserId ON dbo.RefreshToken (UserId);
END;

-- Role table
IF OBJECT_ID(N'dbo.[Role]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Role]
    (
        [Id]          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Name]        NVARCHAR(100)    NOT NULL,
        [Description] NVARCHAR(255)    NULL,
        [CreatedAt]   DATETIME2(7)     NOT NULL,
        CONSTRAINT [UQ_Role_Name] UNIQUE ([Name])
    );
    CREATE NONCLUSTERED INDEX [IX_Role_Name] ON dbo.[Role] ([Name]);
END;

-- UserRole table
IF OBJECT_ID(N'dbo.[UserRole]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[UserRole]
    (
        [Id]        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [UserId]    UNIQUEIDENTIFIER NOT NULL,
        [RoleId]    UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2(7)     NOT NULL,
        CONSTRAINT [FK_UserRole_User] FOREIGN KEY ([UserId]) REFERENCES dbo.[User]([Id]),
        CONSTRAINT [FK_UserRole_Role] FOREIGN KEY ([RoleId]) REFERENCES dbo.[Role]([Id]),
        CONSTRAINT [UQ_UserRole_UserId_RoleId] UNIQUE ([UserId], [RoleId])
    );
    CREATE NONCLUSTERED INDEX [IX_UserRole_UserId] ON dbo.[UserRole] ([UserId]);
    CREATE NONCLUSTERED INDEX [IX_UserRole_RoleId] ON dbo.[UserRole] ([RoleId]);
END;

-- Permission table
IF OBJECT_ID(N'dbo.[Permission]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Permission]
    (
        [Id]          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Name]        NVARCHAR(100)    NOT NULL,
        [Description] NVARCHAR(255)    NULL,
        CONSTRAINT [UQ_Permission_Name] UNIQUE ([Name])
    );
    CREATE NONCLUSTERED INDEX [IX_Permission_Name] ON dbo.[Permission] ([Name]);
END;

-- RolePermission table
IF OBJECT_ID(N'dbo.RolePermission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermission
    (
        Id           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        RoleId       UNIQUEIDENTIFIER NOT NULL,
        PermissionId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT FK_RolePermission_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role(Id),
        CONSTRAINT FK_RolePermission_Permission FOREIGN KEY (PermissionId) REFERENCES dbo.Permission(Id),
        CONSTRAINT UQ_RolePermission_RoleId_PermissionId UNIQUE (RoleId, PermissionId)
    );
    CREATE NONCLUSTERED INDEX IX_RolePermission_RoleId ON dbo.RolePermission (RoleId);
    CREATE NONCLUSTERED INDEX IX_RolePermission_PermissionId ON dbo.RolePermission (PermissionId);
END;

-- Profile table (1:1 with User)
IF OBJECT_ID(N'dbo.[Profile]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Profile]
    (
        [Id]              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [UserId]          UNIQUEIDENTIFIER NOT NULL,
        [FirstName]       NVARCHAR(100)    NOT NULL,
        [MiddleName]      NVARCHAR(100)    NULL,
        [LastName]        NVARCHAR(100)    NOT NULL,
        [DateOfBirth]     DATETIME2(7)     NOT NULL,
        [PhoneNumber]     NVARCHAR(30)    NULL,
        [AvatarBase64]    NVARCHAR(MAX)    NULL,
        [CreatedAtUtc]    DATETIME2(7)    NOT NULL,
        [UpdatedAtUtc]   DATETIME2(7)    NULL,
        CONSTRAINT [FK_Profile_User] FOREIGN KEY ([UserId]) REFERENCES dbo.[User]([Id]),
        CONSTRAINT [UQ_Profile_UserId] UNIQUE ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_Profile_UserId] ON dbo.[Profile] ([UserId]);
END;

-- Tenant table (multi-tenancy: one per company)
IF OBJECT_ID(N'dbo.Tenant', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Tenant]
    (
        [Id]                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Name]              NVARCHAR(200)    NOT NULL,
        [Slug]              NVARCHAR(100)    NOT NULL,
        [CreatedAt]          DATETIME2(7)     NOT NULL,
        [StripeCustomerId]   NVARCHAR(255)    NULL,
        [Status]             NVARCHAR(50)     NOT NULL DEFAULT N'Active',
        CONSTRAINT [UQ_Tenant_Slug] UNIQUE ([Slug]),
        CONSTRAINT [CK_Tenant_Status] CHECK ([Status] IN (N'Active', N'Suspended', N'Cancelled'))
    );
    CREATE NONCLUSTERED INDEX [IX_Tenant_Slug] ON dbo.[Tenant] ([Slug]);
    INSERT INTO dbo.[Tenant] ([Id], [Name], [Slug], [CreatedAt], [StripeCustomerId], [Status])
    VALUES ('11111111-1111-1111-1111-111111111111', N'Default', N'default', GETUTCDATE(), NULL, N'Active');
END;

-- TenantUser table (user membership in tenant with role)
IF OBJECT_ID(N'dbo.[TenantUser]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TenantUser]
    (
        [Id]        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TenantId]  UNIQUEIDENTIFIER NOT NULL,
        [UserId]    UNIQUEIDENTIFIER NOT NULL,
        [Role]      NVARCHAR(50)     NOT NULL,
        [CreatedAt] DATETIME2(7)     NOT NULL,
        CONSTRAINT [FK_TenantUser_Tenant] FOREIGN KEY ([TenantId]) REFERENCES dbo.[Tenant]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TenantUser_User] FOREIGN KEY ([UserId]) REFERENCES dbo.[User]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_TenantUser_TenantId_UserId] UNIQUE ([TenantId], [UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_TenantUser_TenantId] ON dbo.[TenantUser] ([TenantId]);
    CREATE NONCLUSTERED INDEX [IX_TenantUser_UserId] ON dbo.[TenantUser] ([UserId]);
END;

-- Module table (global catalog of available modules for SaaS)
IF OBJECT_ID(N'dbo.[Module]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Module]
    (
        [Id]          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Name]        NVARCHAR(200)    NOT NULL,
        [Key]         NVARCHAR(100)    NOT NULL,
        [Description] NVARCHAR(500)    NULL,
        CONSTRAINT [UQ_Module_Key] UNIQUE ([Key])
    );
    CREATE NONCLUSTERED INDEX [IX_Module_Key] ON dbo.[Module] ([Key]);
END;

-- TenantModule table (which modules are active per tenant)
IF OBJECT_ID(N'dbo.[TenantModule]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TenantModule]
    (
        [Id]        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TenantId]  UNIQUEIDENTIFIER NOT NULL,
        [ModuleId]  UNIQUEIDENTIFIER NOT NULL,
        [IsActive]  BIT              NOT NULL DEFAULT 1,
        CONSTRAINT [FK_TenantModule_Tenant] FOREIGN KEY ([TenantId]) REFERENCES dbo.[Tenant]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TenantModule_Module] FOREIGN KEY ([ModuleId]) REFERENCES dbo.[Module]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_TenantModule_TenantId_ModuleId] UNIQUE ([TenantId], [ModuleId])
    );
    CREATE NONCLUSTERED INDEX [IX_TenantModule_TenantId] ON dbo.[TenantModule] ([TenantId]);
    CREATE NONCLUSTERED INDEX [IX_TenantModule_ModuleId] ON dbo.[TenantModule] ([ModuleId]);
END;

-- Seed default modules (run once after Module + Tenant exist)
IF EXISTS (SELECT 1 FROM sys.[tables] WHERE [name] = N'Module') AND (SELECT COUNT(*) FROM dbo.[Module]) = 0
BEGIN
    INSERT INTO dbo.[Module] ([Id], [Name], [Key], [Description]) VALUES
    (NEWID(), N'Project Management', N'ProjectManagement', N'Projects, tasks, labels, and collaboration.'),
    (NEWID(), N'CRM', N'CRM', N'Customer relationship management.'),
    (NEWID(), N'Analytics', N'Analytics', N'Reports and analytics.');
    INSERT INTO dbo.[TenantModule] ([Id], [TenantId], [ModuleId], [IsActive])
    SELECT NEWID(), '11111111-1111-1111-1111-111111111111', [Id], 1 FROM dbo.[Module] WHERE [Key] = N'ProjectManagement';
END;

-- TenantModuleUser table (user-level access to modules; Tenant Admin grants/revokes)
IF OBJECT_ID(N'dbo.TenantModuleUser', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantModuleUser
    (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TenantId  UNIQUEIDENTIFIER NOT NULL,
        ModuleId  UNIQUEIDENTIFIER NOT NULL,
        UserId    UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2(7)     NOT NULL,
        CONSTRAINT FK_TenantModuleUser_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenant(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TenantModuleUser_Module FOREIGN KEY (ModuleId) REFERENCES dbo.[Module](Id) ON DELETE CASCADE,
        CONSTRAINT FK_TenantModuleUser_User   FOREIGN KEY (UserId)   REFERENCES dbo.[User](Id) ON DELETE CASCADE,
        CONSTRAINT UQ_TenantModuleUser_TenantId_ModuleId_UserId UNIQUE (TenantId, ModuleId, UserId)
    );
    CREATE NONCLUSTERED INDEX IX_TenantModuleUser_TenantId ON dbo.TenantModuleUser (TenantId);
    CREATE NONCLUSTERED INDEX IX_TenantModuleUser_ModuleId ON dbo.TenantModuleUser (ModuleId);
    CREATE NONCLUSTERED INDEX IX_TenantModuleUser_UserId   ON dbo.TenantModuleUser (UserId);
END;

-- Plan table (subscription plans)
IF OBJECT_ID(N'dbo.[Plan]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Plan]
    (
        [Id]                    UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Name]                  NVARCHAR(200)    NOT NULL,
        [PriceMonthly]          DECIMAL(18, 2)   NULL,
        [PriceYearly]           DECIMAL(18, 2)   NULL,
        [MaxProjects]           INT              NULL,
        [MaxUsers]              INT              NULL,
        [AutomationEnabled]     BIT              NOT NULL DEFAULT 0,
        [CreatedAt]             DATETIME2(7)     NOT NULL,
        [StripePriceIdMonthly]  NVARCHAR(255)    NULL,
        [StripePriceIdYearly]   NVARCHAR(255)    NULL,
        [StorageLimitMB]        DECIMAL(18, 2)   NULL
    );
    CREATE NONCLUSTERED INDEX [IX_Plan_Name] ON dbo.[Plan] ([Name]);
END;

-- TenantSubscription table
IF OBJECT_ID(N'dbo.[TenantSubscription]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TenantSubscription]
    (
        [Id]                    UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TenantId]              UNIQUEIDENTIFIER NOT NULL,
        [PlanId]                UNIQUEIDENTIFIER NOT NULL,
        [Status]                NVARCHAR(50)     NOT NULL,
        [StartDate]             DATETIME2(7)     NOT NULL,
        [EndDate]               DATETIME2(7)     NULL,
        [StripeSubscriptionId] NVARCHAR(255)    NULL,
        CONSTRAINT [FK_TenantSubscription_Tenant] FOREIGN KEY ([TenantId]) REFERENCES dbo.[Tenant]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TenantSubscription_Plan] FOREIGN KEY ([PlanId]) REFERENCES dbo.[Plan]([Id]),
        CONSTRAINT [CK_TenantSubscription_Status] CHECK ([Status] IN (N'Active', N'Trial', N'Cancelled'))
    );
    CREATE NONCLUSTERED INDEX [IX_TenantSubscription_TenantId] ON dbo.[TenantSubscription] ([TenantId]);
    CREATE NONCLUSTERED INDEX [IX_TenantSubscription_PlanId] ON dbo.[TenantSubscription] ([PlanId]);
END;

-- Seed plans and assign Free to default tenant
IF EXISTS (SELECT 1 FROM sys.[tables] WHERE [name] = N'Plan') AND (SELECT COUNT(*) FROM dbo.[Plan]) = 0
BEGIN
    DECLARE @FreeId UNIQUEIDENTIFIER = NEWID(), @ProId UNIQUEIDENTIFIER = NEWID(), @EntId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO dbo.[Plan] ([Id], [Name], [PriceMonthly], [PriceYearly], [MaxProjects], [MaxUsers], [AutomationEnabled], [CreatedAt], [StripePriceIdMonthly], [StripePriceIdYearly], [StorageLimitMB]) VALUES
    (@FreeId, N'Free', 0, 0, 3, 5, 0, GETUTCDATE(), NULL, NULL, 100),
    (@ProId, N'Pro', 29, 290, 20, 50, 1, GETUTCDATE(), NULL, NULL, 1024),
    (@EntId, N'Enterprise', 99, 990, NULL, NULL, 1, GETUTCDATE(), NULL, NULL, NULL);
    INSERT INTO dbo.[TenantSubscription] ([Id], [TenantId], [PlanId], [Status], [StartDate], [EndDate], [StripeSubscriptionId])
    VALUES (NEWID(), '11111111-1111-1111-1111-111111111111', @FreeId, N'Active', GETUTCDATE(), NULL, NULL);
END;

-- TenantUsage table (tracked usage per tenant)
IF OBJECT_ID(N'dbo.[TenantUsage]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TenantUsage]
    (
        [TenantId]       UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [ProjectsCount]  INT              NOT NULL DEFAULT 0,
        [UsersCount]     INT              NOT NULL DEFAULT 0,
        [StorageUsedMB]  DECIMAL(18, 2)   NOT NULL DEFAULT 0,
        [TimeLogsCount]  INT              NOT NULL DEFAULT 0,
        [UpdatedAt]      DATETIME2(7)     NOT NULL,
        CONSTRAINT [FK_TenantUsage_Tenant] FOREIGN KEY ([TenantId]) REFERENCES dbo.[Tenant]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_TenantUsage_UpdatedAt] ON dbo.[TenantUsage] ([UpdatedAt]);
    INSERT INTO dbo.[TenantUsage] ([TenantId], [ProjectsCount], [UsersCount], [StorageUsedMB], [TimeLogsCount], [UpdatedAt])
    VALUES ('11111111-1111-1111-1111-111111111111', 0, 0, 0, 0, GETUTCDATE());
END;

-- Project table
IF OBJECT_ID(N'dbo.[Project]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Project]
    (
        [Id]                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TenantId]          UNIQUEIDENTIFIER NOT NULL,
        [Name]              NVARCHAR(200)    NOT NULL,
        [Description]       NVARCHAR(1000)   NULL,
        [Status]            NVARCHAR(50)     NOT NULL,
        [CreatedByUserId]   UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt]         DATETIME2(7)     NOT NULL,
        [UpdatedAt]         DATETIME2(7)     NULL,
        CONSTRAINT [FK_Project_Tenant] FOREIGN KEY ([TenantId]) REFERENCES dbo.[Tenant]([Id]),
        CONSTRAINT [FK_Project_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES dbo.[User]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Project_TenantId] ON dbo.[Project] ([TenantId]);
    CREATE NONCLUSTERED INDEX [IX_Project_CreatedByUserId] ON dbo.[Project] ([CreatedByUserId]);
    CREATE NONCLUSTERED INDEX [IX_Project_Status] ON dbo.[Project] ([Status]);
END;

-- Task table
IF OBJECT_ID(N'dbo.[Task]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Task]
    (
        [Id]                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [ProjectId]         UNIQUEIDENTIFIER NOT NULL,
        [Title]             NVARCHAR(200)    NOT NULL,
        [Description]       NVARCHAR(1000)   NULL,
        [Status]            NVARCHAR(50)     NOT NULL,
        [Priority]          NVARCHAR(50)     NOT NULL,
        [AssignedToUserId]  UNIQUEIDENTIFIER NULL,
        [DueDate]           DATETIME2(7)     NULL,
        [CreatedByUserId]   UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt]         DATETIME2(7)     NOT NULL,
        [UpdatedAt]             DATETIME2(7)     NULL,
        [EstimatedHours]        DECIMAL(5,2)     NULL,
        [IsRecurring]           BIT              NOT NULL DEFAULT 0,
        [RecurrencePattern]     NVARCHAR(100)   NULL,
        [RecurrenceInterval]    INT              NULL,
        [RecurrenceEndDate]     DATETIME2(7)     NULL,
        [RecurrenceSourceTaskId] UNIQUEIDENTIFIER NULL,
        CONSTRAINT [FK_Task_Project] FOREIGN KEY ([ProjectId]) REFERENCES dbo.[Project]([Id]),
        CONSTRAINT [FK_Task_AssignedToUser] FOREIGN KEY ([AssignedToUserId]) REFERENCES dbo.[User]([Id]),
        CONSTRAINT [FK_Task_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES dbo.[User]([Id]),
        CONSTRAINT [FK_Task_RecurrenceSource] FOREIGN KEY ([RecurrenceSourceTaskId]) REFERENCES dbo.[Task]([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION
    );
    CREATE NONCLUSTERED INDEX [IX_Task_ProjectId] ON dbo.[Task] ([ProjectId]);
    CREATE NONCLUSTERED INDEX [IX_Task_AssignedToUserId] ON dbo.[Task] ([AssignedToUserId]);
    CREATE NONCLUSTERED INDEX [IX_Task_Status] ON dbo.[Task] ([Status]);
    CREATE NONCLUSTERED INDEX [IX_Task_RecurrenceSourceTaskId] ON dbo.[Task] ([RecurrenceSourceTaskId]);
END;

-- TaskComments table
IF OBJECT_ID(N'dbo.[TaskComment]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TaskComment]
    (
        [Id]        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TaskId]    UNIQUEIDENTIFIER NOT NULL,
        [UserId]    UNIQUEIDENTIFIER NOT NULL,
        [Content]   NVARCHAR(2000)   NOT NULL,
        [CreatedAt] DATETIME2(7)     NOT NULL,
        [UpdatedAt] DATETIME2(7)     NULL,
        CONSTRAINT [FK_TaskComment_Task] FOREIGN KEY ([TaskId]) REFERENCES dbo.[Task]([Id]),
        CONSTRAINT [FK_TaskComment_User] FOREIGN KEY ([UserId]) REFERENCES dbo.[User]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_TaskComment_TaskId] ON dbo.[TaskComment] ([TaskId]);
    CREATE NONCLUSTERED INDEX [IX_TaskComment_UserId] ON dbo.[TaskComment] ([UserId]);
END;

-- Notification table (in-app user notifications)
IF OBJECT_ID(N'dbo.Notification', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notification
    (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        UserId      UNIQUEIDENTIFIER NOT NULL,
        Type        NVARCHAR(100)    NOT NULL,
        Title       NVARCHAR(200)    NOT NULL,
        Message     NVARCHAR(1000)   NOT NULL,
        ReferenceId UNIQUEIDENTIFIER NULL,
        IsRead      BIT              NOT NULL DEFAULT 0,
        CreatedAt   DATETIME2(7)     NOT NULL,
        CONSTRAINT FK_Notification_User FOREIGN KEY (UserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_Notification_UserId ON dbo.Notification (UserId);
    CREATE NONCLUSTERED INDEX IX_Notification_UserId_IsRead_CreatedAt ON dbo.Notification (UserId, IsRead, CreatedAt DESC);
END;

-- TaskTimeLog table (time tracking per task)
IF OBJECT_ID(N'dbo.[TaskTimeLog]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TaskTimeLog]
    (
        [Id]          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TaskId]      UNIQUEIDENTIFIER NOT NULL,
        [UserId]      UNIQUEIDENTIFIER NOT NULL,
        [Hours]       DECIMAL(5,2)     NOT NULL,
        [Description] NVARCHAR(500)    NULL,
        [CreatedAt]   DATETIME2(7)     NOT NULL,
        CONSTRAINT [FK_TaskTimeLog_Task] FOREIGN KEY ([TaskId]) REFERENCES dbo.[Task]([Id]),
        CONSTRAINT [FK_TaskTimeLog_User] FOREIGN KEY ([UserId]) REFERENCES dbo.[User]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_TaskTimeLog_TaskId] ON dbo.[TaskTimeLog] ([TaskId]);
    CREATE NONCLUSTERED INDEX [IX_TaskTimeLog_UserId] ON dbo.[TaskTimeLog] ([UserId]);
END;

-- ProjectMembers table
IF OBJECT_ID(N'dbo.[ProjectMember]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ProjectMember]
    (
        [Id]        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [ProjectId] UNIQUEIDENTIFIER NOT NULL,
        [UserId]    UNIQUEIDENTIFIER NOT NULL,
        [Role]      NVARCHAR(50)     NOT NULL,
        [CreatedAt] DATETIME2(7)     NOT NULL,
        CONSTRAINT [FK_ProjectMember_Project] FOREIGN KEY ([ProjectId]) REFERENCES dbo.[Project]([Id]),
        CONSTRAINT [FK_ProjectMember_User] FOREIGN KEY ([UserId]) REFERENCES dbo.[User]([Id]),
        CONSTRAINT [UQ_ProjectMember_ProjectId_UserId] UNIQUE ([ProjectId], [UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectMember_ProjectId] ON dbo.[ProjectMember] ([ProjectId]);
    CREATE NONCLUSTERED INDEX [IX_ProjectMember_UserId] ON dbo.[ProjectMember] ([UserId]);
END;

-- Labels table (project-level; TenantId for multi-tenant scope)
IF OBJECT_ID(N'dbo.[Label]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Label]
    (
        [Id]        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TenantId]  UNIQUEIDENTIFIER NOT NULL,
        [Name]      NVARCHAR(100)    NOT NULL,
        [Color]     NVARCHAR(20)     NOT NULL,
        [ProjectId] UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2(7)     NOT NULL,
        CONSTRAINT [FK_Label_Tenant] FOREIGN KEY ([TenantId]) REFERENCES dbo.[Tenant]([Id]),
        CONSTRAINT [FK_Label_Project] FOREIGN KEY ([ProjectId]) REFERENCES dbo.[Project]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Label_TenantId] ON dbo.[Label] ([TenantId]);
    CREATE NONCLUSTERED INDEX [IX_Label_ProjectId] ON dbo.[Label] ([ProjectId]);
END;

-- TaskLabels table (many-to-many)
IF OBJECT_ID(N'dbo.[TaskLabel]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TaskLabel]
    (
        [Id]      UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TaskId]  UNIQUEIDENTIFIER NOT NULL,
        [LabelId] UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT [FK_TaskLabel_Task] FOREIGN KEY ([TaskId]) REFERENCES dbo.[Task]([Id]),
        CONSTRAINT [FK_TaskLabel_Label] FOREIGN KEY ([LabelId]) REFERENCES dbo.[Label]([Id]),
        CONSTRAINT [UQ_TaskLabel_TaskId_LabelId] UNIQUE ([TaskId], [LabelId])
    );
    CREATE NONCLUSTERED INDEX [IX_TaskLabel_TaskId] ON dbo.[TaskLabel] ([TaskId]);
    CREATE NONCLUSTERED INDEX [IX_TaskLabel_LabelId] ON dbo.[TaskLabel] ([LabelId]);
END;

-- TaskActivities table
IF OBJECT_ID(N'dbo.[TaskActivity]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TaskActivity]
    (
        [Id]        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TaskId]    UNIQUEIDENTIFIER NOT NULL,
        [UserId]    UNIQUEIDENTIFIER NOT NULL,
        [Action]    NVARCHAR(100)    NOT NULL,
        [OldValue]  NVARCHAR(1000)   NULL,
        [NewValue]  NVARCHAR(1000)   NULL,
        [CreatedAt] DATETIME2(7)     NOT NULL,
        CONSTRAINT [FK_TaskActivity_Task] FOREIGN KEY ([TaskId]) REFERENCES dbo.[Task]([Id]),
        CONSTRAINT [FK_TaskActivity_User] FOREIGN KEY ([UserId]) REFERENCES dbo.[User]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_TaskActivity_TaskId] ON dbo.[TaskActivity] ([TaskId]);
    CREATE NONCLUSTERED INDEX [IX_TaskActivity_CreatedAt] ON dbo.[TaskActivity] ([CreatedAt] DESC);
END;

-- TaskAttachment table (file attachments metadata; files stored on disk)
IF OBJECT_ID(N'dbo.TaskAttachment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaskAttachment
    (
        Id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TaskId            UNIQUEIDENTIFIER NOT NULL,
        FileName          NVARCHAR(255)    NOT NULL,
        FilePath          NVARCHAR(500)   NOT NULL,
        FileSize          INT             NOT NULL,
        ContentType       NVARCHAR(100)   NOT NULL,
        UploadedByUserId  UNIQUEIDENTIFIER NOT NULL,
        CreatedAt         DATETIME2(7)    NOT NULL,
        CONSTRAINT FK_TaskAttachment_Task FOREIGN KEY (TaskId) REFERENCES dbo.Task(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TaskAttachment_User FOREIGN KEY (UploadedByUserId) REFERENCES dbo.[User](Id)
    );
    CREATE NONCLUSTERED INDEX IX_TaskAttachment_TaskId ON dbo.TaskAttachment (TaskId);
    CREATE NONCLUSTERED INDEX IX_TaskAttachment_UploadedByUserId ON dbo.TaskAttachment (UploadedByUserId);
END;

-- ProjectCustomField table (custom field definitions per project)
IF OBJECT_ID(N'dbo.ProjectCustomField', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectCustomField
    (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        ProjectId   UNIQUEIDENTIFIER NOT NULL,
        Name        NVARCHAR(100)    NOT NULL,
        FieldType   NVARCHAR(50)     NOT NULL,
        Options     NVARCHAR(MAX)    NULL,
        CreatedAt   DATETIME2(7)     NOT NULL,
        CONSTRAINT FK_ProjectCustomField_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Project(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_ProjectCustomField_ProjectId ON dbo.ProjectCustomField (ProjectId);
END;

-- TaskCustomFieldValue table (custom field values on tasks)
IF OBJECT_ID(N'dbo.[TaskCustomFieldValue]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TaskCustomFieldValue]
    (
        [Id]       UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [TaskId]   UNIQUEIDENTIFIER NOT NULL,
        [FieldId]  UNIQUEIDENTIFIER NOT NULL,
        [Value]    NVARCHAR(MAX)    NULL,
        CONSTRAINT [FK_TaskCustomFieldValue_Task] FOREIGN KEY ([TaskId]) REFERENCES dbo.[Task]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TaskCustomFieldValue_Field] FOREIGN KEY ([FieldId]) REFERENCES dbo.[ProjectCustomField]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_TaskCustomFieldValue_TaskId] ON dbo.[TaskCustomFieldValue] ([TaskId]);
    CREATE NONCLUSTERED INDEX [IX_TaskCustomFieldValue_FieldId] ON dbo.[TaskCustomFieldValue] ([FieldId]);
END;

-- ProjectAutomationRule table (lightweight automation: trigger + action)
IF OBJECT_ID(N'dbo.[ProjectAutomationRule]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ProjectAutomationRule]
    (
        [Id]            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [ProjectId]     UNIQUEIDENTIFIER NOT NULL,
        [Trigger]       NVARCHAR(100)    NOT NULL,
        [Action]        NVARCHAR(100)    NOT NULL,
        [TargetUserId]  UNIQUEIDENTIFIER NULL,
        [TargetValue]   NVARCHAR(100)    NULL,
        CONSTRAINT [FK_ProjectAutomationRule_Project] FOREIGN KEY ([ProjectId]) REFERENCES dbo.[Project]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectAutomationRule_TargetUser] FOREIGN KEY ([TargetUserId]) REFERENCES dbo.[User]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectAutomationRule_ProjectId] ON dbo.[ProjectAutomationRule] ([ProjectId]);
END;

-- ProjectTemplate table (templates for "create project from template")
IF OBJECT_ID(N'dbo.ProjectTemplate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectTemplate
    (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name        NVARCHAR(200)    NOT NULL,
        Description NVARCHAR(500)    NULL,
        CreatedAt   DATETIME2(7)     NOT NULL
    );
END;

-- ProjectTemplateTask table (default tasks per template)
IF OBJECT_ID(N'dbo.ProjectTemplateTask', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectTemplateTask
    (
        Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TemplateId      UNIQUEIDENTIFIER NOT NULL,
        Title           NVARCHAR(200)    NOT NULL,
        Description     NVARCHAR(1000)  NULL,
        DefaultStatus   NVARCHAR(50)     NULL,
        DefaultPriority NVARCHAR(50)    NULL,
        CONSTRAINT FK_ProjectTemplateTask_Template FOREIGN KEY (TemplateId) REFERENCES dbo.ProjectTemplate(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_ProjectTemplateTask_TemplateId ON dbo.ProjectTemplateTask (TemplateId);
END;

GO
