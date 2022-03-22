DROP TABLE [dbo].[IncomingMessages]

CREATE TABLE [dbo].[MessageIds]
(
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [MessageId] [nvarchar](50) NOT NULL,
    CONSTRAINT [PK_MessageIds] PRIMARY KEY NONCLUSTERED ([MessageId] ASC))

CREATE UNIQUE CLUSTERED INDEX CIX_IncomingMessages ON [dbo].MessageIds([RecordId])