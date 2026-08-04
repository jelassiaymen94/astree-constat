IF DB_ID(N'AstreeClaimsDb') IS NULL
BEGIN
    CREATE DATABASE AstreeClaimsDb;
END;
GO

USE AstreeClaimsDb;
GO

CREATE TABLE Clients (
    ClientId NVARCHAR(20) NOT NULL PRIMARY KEY,
    Nom NVARCHAR(100) NOT NULL,
    Prenom NVARCHAR(100) NOT NULL,
    Gouvernorat NVARCHAR(100) NOT NULL
);
GO

CREATE TABLE Contrats (
    ContractId NVARCHAR(20) NOT NULL PRIMARY KEY,
    ClientId NVARCHAR(20) NOT NULL,
    TypeCouverture NVARCHAR(100) NOT NULL,
    DateDebut DATE NOT NULL,
    DateFin DATE NOT NULL,
    CONSTRAINT FK_Contrats_Clients FOREIGN KEY (ClientId) REFERENCES Clients(ClientId),
    CONSTRAINT CK_Contrats_Dates CHECK (DateFin >= DateDebut)
);
GO

CREATE TABLE Vehicules (
    VehicleId NVARCHAR(20) NOT NULL PRIMARY KEY,
    ContractId NVARCHAR(20) NOT NULL UNIQUE,
    TypeVehicule NVARCHAR(50) NOT NULL,
    Marque NVARCHAR(50) NOT NULL,
    Modele NVARCHAR(100) NOT NULL,
    Immatriculation NVARCHAR(30) NOT NULL,
    CONSTRAINT FK_Vehicules_Contrats FOREIGN KEY (ContractId) REFERENCES Contrats(ContractId)
);
GO

CREATE TABLE Sinistres (
    ClaimId NVARCHAR(20) NOT NULL PRIMARY KEY,
    ContractId NVARCHAR(20) NOT NULL,
    ClientId NVARCHAR(20) NOT NULL,
    VehicleId NVARCHAR(20) NOT NULL,
    DateSinistre DATE NOT NULL,
    TypeSinistre NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    MontantEstime DECIMAL(18,2) NOT NULL DEFAULT 0,
    MontantIndemnisation DECIMAL(18,2) NOT NULL DEFAULT 0,
    Statut NVARCHAR(50) NOT NULL,
    CONSTRAINT FK_Sinistres_Contrats FOREIGN KEY (ContractId) REFERENCES Contrats(ContractId),
    CONSTRAINT FK_Sinistres_Clients FOREIGN KEY (ClientId) REFERENCES Clients(ClientId),
    CONSTRAINT FK_Sinistres_Vehicules FOREIGN KEY (VehicleId) REFERENCES Vehicules(VehicleId),
    CONSTRAINT CK_Sinistres_MontantEstime CHECK (MontantEstime >= 0),
    CONSTRAINT CK_Sinistres_Indemnisation CHECK (MontantIndemnisation >= 0)
);
GO

CREATE TABLE GenerationLogs (
    GenerationId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    ClaimId NVARCHAR(20) NOT NULL,
    GenerationType NVARCHAR(30) NOT NULL,
    UserInstruction NVARCHAR(MAX) NULL,
    GeneratedContent NVARCHAR(MAX) NULL,
    ModelName NVARCHAR(100) NULL,
    PromptVersion NVARCHAR(20) NOT NULL DEFAULT '1.0',
    Success BIT NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    DurationMs INT NULL,
    CONSTRAINT FK_GenerationLogs_Sinistres FOREIGN KEY (ClaimId) REFERENCES Sinistres(ClaimId),
    CONSTRAINT CK_GenerationLogs_Type CHECK (GenerationType IN ('summary', 'letter', 'response')),
    CONSTRAINT CK_GenerationLogs_Duration CHECK (DurationMs IS NULL OR DurationMs >= 0)
);
GO

CREATE INDEX IX_Sinistres_Statut ON Sinistres(Statut);
CREATE INDEX IX_Sinistres_DateSinistre ON Sinistres(DateSinistre);
CREATE INDEX IX_GenerationLogs_ClaimId_CreatedAt ON GenerationLogs(ClaimId, CreatedAt DESC);
GO
