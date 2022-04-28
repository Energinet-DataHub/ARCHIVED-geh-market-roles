ALTER TABLE [b2b].[OutgoingMessages]
    ADD 
    [ReceiverId] [nvarchar](50) NOT NULL,
    [ReceiverRole] [nvarchar](50) NOT NULL
GO
ALTER TABLE [b2b].[OutgoingMessages]
DROP COLUMN RecipientId