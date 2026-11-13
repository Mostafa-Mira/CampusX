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

CREATE TABLE [ApplicationUsers] (
    [Id] uniqueidentifier NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [CollegeName] nvarchar(max) NOT NULL,
    [ProfileImageUrl] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsBlocked] bit NOT NULL,
    [Role] int NOT NULL,
    CONSTRAINT [PK_ApplicationUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Categories] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Products] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [SellerId] uniqueidentifier NOT NULL,
    [CategoryId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_ApplicationUsers_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ContactInquiries] (
    [Id] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [BuyerId] uniqueidentifier NOT NULL,
    [SellerId] uniqueidentifier NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ContactInquiries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContactInquiries_ApplicationUsers_BuyerId] FOREIGN KEY ([BuyerId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContactInquiries_ApplicationUsers_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContactInquiries_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Reports] (
    [Id] uniqueidentifier NOT NULL,
    [ReporterId] uniqueidentifier NOT NULL,
    [TargetProductId] uniqueidentifier NULL,
    [TargetUserId] uniqueidentifier NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Reports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reports_ApplicationUsers_ReporterId] FOREIGN KEY ([ReporterId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reports_ApplicationUsers_TargetUserId] FOREIGN KEY ([TargetUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reports_Products_TargetProductId] FOREIGN KEY ([TargetProductId]) REFERENCES [Products] ([Id])
);
GO

CREATE INDEX [IX_ContactInquiries_BuyerId] ON [ContactInquiries] ([BuyerId]);
GO

CREATE INDEX [IX_ContactInquiries_ProductId] ON [ContactInquiries] ([ProductId]);
GO

CREATE INDEX [IX_ContactInquiries_SellerId] ON [ContactInquiries] ([SellerId]);
GO

CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
GO

CREATE INDEX [IX_Products_SellerId] ON [Products] ([SellerId]);
GO

CREATE INDEX [IX_Reports_ReporterId] ON [Reports] ([ReporterId]);
GO

CREATE INDEX [IX_Reports_TargetProductId] ON [Reports] ([TargetProductId]);
GO

CREATE INDEX [IX_Reports_TargetUserId] ON [Reports] ([TargetUserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261019193910_InitialCreate', N'8.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF EXISTS (SELECT 1 FROM [ApplicationUsers])
    THROW 51000, 'Existing accounts need a password migration plan before this migration.', 1;
IF EXISTS (SELECT 1 FROM [Products] WHERE [Price] < 0 OR [Status] NOT IN (0, 1))
    THROW 51001, 'Products contain invalid prices or legacy statuses.', 1;
IF EXISTS (SELECT 1 FROM [Reports]
    WHERE ([TargetProductId] IS NULL AND [TargetUserId] IS NULL)
       OR ([TargetProductId] IS NOT NULL AND [TargetUserId] IS NOT NULL))
    THROW 51002, 'Reports must target exactly one product or user.', 1;
IF EXISTS (SELECT 1 FROM [ContactInquiries] AS i
    JOIN [Products] AS p ON p.[Id] = i.[ProductId]
    WHERE i.[SellerId] <> p.[SellerId])
    THROW 51003, 'Contact inquiries contain a seller different from the product seller.', 1;
GO

ALTER TABLE [ContactInquiries] DROP CONSTRAINT [FK_ContactInquiries_ApplicationUsers_SellerId];
GO

ALTER TABLE [ContactInquiries] DROP CONSTRAINT [FK_ContactInquiries_Products_ProductId];
GO

ALTER TABLE [Products] DROP CONSTRAINT [FK_Products_ApplicationUsers_SellerId];
GO

ALTER TABLE [Products] DROP CONSTRAINT [FK_Products_Categories_CategoryId];
GO

ALTER TABLE [Reports] DROP CONSTRAINT [FK_Reports_Products_TargetProductId];
GO

DROP INDEX [IX_ContactInquiries_SellerId] ON [ContactInquiries];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ContactInquiries]') AND [c].[name] = N'SellerId');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [ContactInquiries] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [ContactInquiries] DROP COLUMN [SellerId];
GO

EXEC sp_rename N'[Products].[Title]', N'Name', N'COLUMN';
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ApplicationUsers]') AND [c].[name] = N'Email');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ApplicationUsers] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [ApplicationUsers] ALTER COLUMN [Email] nvarchar(256) NOT NULL;
GO

ALTER TABLE [ApplicationUsers] ADD [PasswordHash] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Reports] ADD CONSTRAINT [CK_Reports_ExactlyOneTarget] CHECK (([TargetProductId] IS NOT NULL AND [TargetUserId] IS NULL) OR ([TargetProductId] IS NULL AND [TargetUserId] IS NOT NULL));
GO

ALTER TABLE [Products] ADD CONSTRAINT [CK_Products_Price_NonNegative] CHECK ([Price] >= 0);
GO

CREATE UNIQUE INDEX [IX_ApplicationUsers_Email] ON [ApplicationUsers] ([Email]);
GO

ALTER TABLE [ContactInquiries] ADD CONSTRAINT [FK_ContactInquiries_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_ApplicationUsers_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [Reports] ADD CONSTRAINT [FK_Reports_Products_TargetProductId] FOREIGN KEY ([TargetProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261026153452_DatabaseCorrections', N'8.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Products] ADD [College] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Products] ADD [PhoneNumber] nvarchar(max) NOT NULL DEFAULT N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261028125101_AddCollegeAndPhoneToProduct', N'8.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261028132315_MakeCollegeNullable', N'8.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ProductPromotions] (
    [Id] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [ActivatedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProductPromotions] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ProductPromotions_Expiry] CHECK ([ExpiresAt] > [ActivatedAt]),
    CONSTRAINT [FK_ProductPromotions_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [PromotionCreditPurchases] (
    [Id] uniqueidentifier NOT NULL,
    [BuyerId] uniqueidentifier NOT NULL,
    [Package] int NOT NULL,
    [CreditsPurchased] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [ProviderReference] nvarchar(128) NOT NULL,
    [PurchasedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PromotionCreditPurchases] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_PromotionCreditPurchases_Credits_Positive] CHECK ([CreditsPurchased] > 0),
    CONSTRAINT [FK_PromotionCreditPurchases_ApplicationUsers_BuyerId] FOREIGN KEY ([BuyerId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_ProductPromotions_ProductId_ExpiresAt] ON [ProductPromotions] ([ProductId], [ExpiresAt]);
GO

CREATE INDEX [IX_PromotionCreditPurchases_BuyerId] ON [PromotionCreditPurchases] ([BuyerId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261110065800_AddRevenueSystem', N'8.0.4');
GO

COMMIT;
GO

