ALTER TABLE b2b.OutgoingMessages
    ADD 
        [MarketActivityRecord_Id] [nvarchar](50) NULL,
        [MarketActivityRecord_OriginalTransactionId] [nvarchar](100) NULL,
        [MarketActivityRecord_MarketEvaluationPointId] [nvarchar](100) NULL