-- Created: 2021-03-09
-- Author: SSG
-- Purpose: Create tables to store Process Coordinator objects

USE MarketData;

IF OBJECT_ID('dbo.ProcessCoordinators', 'U') IS NOT NULL RAISERROR('''dbo.ProcessCoordinators'' ALREADY EXISTS', 16, 1);
IF OBJECT_ID('dbo.ProcessCoordinatorBusinessProcesses', 'U') IS NOT NULL RAISERROR('''dbo.ProcessCoordinatorBusinessProcesses'' ALREADY EXISTS', 16, 1);

CREATE TABLE dbo.ProcessCoordinators (
    Id INT IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    ProcessCoordinatorId NVARCHAR(36) NOT NULL,
    Version INT NOT NULL
);

CREATE UNIQUE INDEX UX_ProcessCoordinators_ProcessCoordinatorId ON dbo.ProcessCoordinators (ProcessCoordinatorId);
ALTER TABLE dbo.ProcessCoordinators ADD CONSTRAINT DF_ProcessCoordinators_Version DEFAULT 1 FOR Version;

CREATE TABLE dbo.ProcessCoordinatorBusinessProcesses (
    Id INT IDENTITY (1, 1) NOT NULL,
    ProcessCoordinatorId INT NOT NULL,
    EffectiveDate datetime2(7) NOT NULL,
    ProcessType NVARCHAR(32) NOT NULL,
    [State] INT NOT NULL,   
    Intent INT NOT NULL,
    SuspendedByProcessId INT,
    CONSTRAINT NK_ProcessCoordinatorBusinessProcesses
        PRIMARY KEY NONCLUSTERED (ProcessCoordinatorId, EffectiveDate, ProcessType)
);

CREATE UNIQUE CLUSTERED INDEX CI_ProcessCoordinatorsBusinessProcesses 
    ON ProcessCoordinatorBusinessProcesses (Id);

ALTER TABLE dbo.ProcessCoordinatorBusinessProcesses
    ADD FOREIGN KEY (ProcessCoordinatorId) REFERENCES dbo.ProcessCoordinators (Id);

ALTER TABLE dbo.ProcessCoordinatorBusinessProcesses
    ADD FOREIGN KEY (SuspendedByProcessId) REFERENCES dbo.ProcessCoordinatorBusinessProcesses (Id);