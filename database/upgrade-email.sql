USE AstreeClaimsDb;
GO

IF COL_LENGTH('Clients', 'Email') IS NULL
    ALTER TABLE Clients ADD Email NVARCHAR(254) NULL;
GO

IF OBJECT_ID('EmailLogs', 'U') IS NULL
BEGIN
    CREATE TABLE EmailLogs (
        EmailId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        ClientRequestId UNIQUEIDENTIFIER NOT NULL,
        ClaimId NVARCHAR(20) NOT NULL,
        GenerationId UNIQUEIDENTIFIER NULL,
        RecipientEmail NVARCHAR(254) NOT NULL,
        ActualRecipientEmail NVARCHAR(254) NOT NULL,
        Subject NVARCHAR(200) NOT NULL,
        BodyHtml NVARCHAR(MAX) NOT NULL,
        BodyText NVARCHAR(MAX) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        ProviderMessageId NVARCHAR(200) NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        SentAt DATETIME2 NULL,
        CONSTRAINT FK_EmailLogs_Sinistres FOREIGN KEY (ClaimId) REFERENCES Sinistres(ClaimId),
        CONSTRAINT UQ_EmailLogs_ClientRequestId UNIQUE (ClientRequestId),
        CONSTRAINT CK_EmailLogs_Status CHECK (Status IN ('pending', 'sent', 'failed'))
    );
    CREATE INDEX IX_EmailLogs_ClaimId_CreatedAt ON EmailLogs(ClaimId, CreatedAt DESC);
END;
GO
