-- Run sqlcmd -v InboxId="<reviewed UUID>" against the explicitly selected database.
SET XACT_ABORT ON;
DECLARE @Id uniqueidentifier = TRY_CONVERT(uniqueidentifier, '$(InboxId)');
IF @Id IS NULL THROW 50000, 'InboxId must be a UUID', 1;
UPDATE StoreWebhookInbox
SET AttemptCount=0, NextAttemptAtUtc=SYSUTCDATETIME(), ErrorCode=NULL, LeaseOwner=NULL, LeaseExpiresAtUtc=NULL
WHERE Id=@Id AND CompletedAtUtc IS NULL AND AttemptCount>=10
AND (LeaseExpiresAtUtc IS NULL OR LeaseExpiresAtUtc<=SYSUTCDATETIME());
SELECT @@ROWCOUNT AS Requeued;
