-- Stripe billing: customer and subscription IDs; plan price IDs for Checkout.
-- SQL Server. Run after 023.

IF NOT EXISTS (SELECT 1 FROM sys.[columns] WHERE [object_id] = OBJECT_ID(N'dbo.[Tenant]') AND [name] = N'StripeCustomerId')
    ALTER TABLE dbo.[Tenant] ADD [StripeCustomerId] NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.[columns] WHERE [object_id] = OBJECT_ID(N'dbo.[TenantSubscription]') AND [name] = N'StripeSubscriptionId')
    ALTER TABLE dbo.[TenantSubscription] ADD [StripeSubscriptionId] NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.[columns] WHERE [object_id] = OBJECT_ID(N'dbo.[Plan]') AND [name] = N'StripePriceIdMonthly')
    ALTER TABLE dbo.[Plan] ADD [StripePriceIdMonthly] NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.[columns] WHERE [object_id] = OBJECT_ID(N'dbo.[Plan]') AND [name] = N'StripePriceIdYearly')
    ALTER TABLE dbo.[Plan] ADD [StripePriceIdYearly] NVARCHAR(255) NULL;

GO
