-- Tenant status: Active, Suspended, Cancelled. Default Active.
-- SQL Server. Run after 025.

IF NOT EXISTS (SELECT 1 FROM sys.[columns] WHERE [object_id] = OBJECT_ID(N'dbo.[Tenant]') AND [name] = N'Status')
BEGIN
    ALTER TABLE dbo.[Tenant] ADD [Status] NVARCHAR(50) NOT NULL DEFAULT N'Active';
    ALTER TABLE dbo.[Tenant] ADD CONSTRAINT [CK_Tenant_Status] CHECK ([Status] IN (N'Active', N'Suspended', N'Cancelled'));
END;

GO
