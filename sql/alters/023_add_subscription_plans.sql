-- Subscription plans and tenant subscriptions.
-- SQL Server. Run after 022.

-- Plan table (subscription plan definitions)
IF OBJECT_ID(N'dbo.Plan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Plan
    (
        Id                 UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Name               NVARCHAR(200)    NOT NULL,
        PriceMonthly       DECIMAL(18, 2)   NULL,
        PriceYearly        DECIMAL(18, 2)   NULL,
        MaxProjects        INT              NULL,
        MaxUsers           INT              NULL,
        AutomationEnabled  BIT              NOT NULL DEFAULT 0,
        CreatedAt          DATETIME2(7)     NOT NULL
    );
    CREATE NONCLUSTERED INDEX IX_Plan_Name ON dbo.Plan (Name);
END;

-- TenantSubscription table (current subscription per tenant)
IF OBJECT_ID(N'dbo.TenantSubscription', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantSubscription
    (
        Id         UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        TenantId   UNIQUEIDENTIFIER NOT NULL,
        PlanId     UNIQUEIDENTIFIER NOT NULL,
        Status     NVARCHAR(50)     NOT NULL,
        StartDate  DATETIME2(7)     NOT NULL,
        EndDate    DATETIME2(7)     NULL,
        CONSTRAINT FK_TenantSubscription_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenant(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TenantSubscription_Plan FOREIGN KEY (PlanId) REFERENCES dbo.Plan(Id),
        CONSTRAINT CK_TenantSubscription_Status CHECK (Status IN (N'Active', N'Trial', N'Cancelled'))
    );
    CREATE NONCLUSTERED INDEX IX_TenantSubscription_TenantId ON dbo.TenantSubscription (TenantId);
    CREATE NONCLUSTERED INDEX IX_TenantSubscription_PlanId ON dbo.TenantSubscription (PlanId);
END;

-- Seed default plans
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Plan') AND (SELECT COUNT(*) FROM dbo.Plan) = 0
BEGIN
    DECLARE @FreeId UNIQUEIDENTIFIER = NEWID(), @ProId UNIQUEIDENTIFIER = NEWID(), @EnterpriseId UNIQUEIDENTIFIER = NEWID();
    INSERT INTO dbo.Plan (Id, Name, PriceMonthly, PriceYearly, MaxProjects, MaxUsers, AutomationEnabled, CreatedAt) VALUES
    (@FreeId, N'Free', 0, 0, 3, 5, 0, GETUTCDATE()),
    (@ProId, N'Pro', 29, 290, 20, 50, 1, GETUTCDATE()),
    (@EnterpriseId, N'Enterprise', 99, 990, NULL, NULL, 1, GETUTCDATE());
    INSERT INTO dbo.TenantSubscription (Id, TenantId, PlanId, Status, StartDate, EndDate)
    SELECT NEWID(), Id, @FreeId, N'Active', GETUTCDATE(), NULL FROM dbo.Tenant t
    WHERE NOT EXISTS (SELECT 1 FROM dbo.TenantSubscription ts WHERE ts.TenantId = t.Id);
END;

GO
