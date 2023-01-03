/* Create a table type. */
CREATE TYPE EnqueuedMessageType
AS TABLE
(
    Id              uniqueidentifier not null,        
    MessageType     varchar(255)     not null,
    MessageCategory varchar(255)     not null,
    ReceiverId      varchar(255)     not null,
    ReceiverRole    varchar(50)      not null,
    SenderId        varchar(255)     not null,
    SenderRole      varchar(50)      not null,
    ProcessType     varchar(50)      not null,
    MessageRecord   nvarchar(max)    not null
);