BEGIN TRANSACTION;
GO

CREATE TABLE [RateLimitBuckets] (
    [Id] varchar(64) NOT NULL,
    [Count] int NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_RateLimitBuckets] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_RateLimitBuckets_ExpiresAtUtc] ON [RateLimitBuckets] ([ExpiresAtUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261021152026_ProductionOperations', N'8.0.21');
GO

COMMIT;
GO

