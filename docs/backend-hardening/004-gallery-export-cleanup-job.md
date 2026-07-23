# 004 — Gallery Export Cleanup Job

## Problem

Completed gallery export ZIP files have a finite download lifetime. Preventing downloads after expiration is not sufficient; expired objects must also be removed from object storage to control cost and reduce unnecessary data retention.

## Goals

- Find expired completed exports in bounded batches.
- Delete the corresponding object from storage before deleting the database record.
- Preserve the database record when storage deletion fails.
- Make repeated executions safe.
- Prevent one failed record from blocking the remaining records in the batch.
- Run automatically on a configurable interval.

## Business Rules

An export is eligible for cleanup when all conditions are true:

- `Status == Completed`
- `ExpiresAtUtc` has a value
- `ExpiresAtUtc <= current UTC time`

Pending, processing, failed, cancelled and unexpired completed exports are not touched.

## Processing Order

```text
Select expired exports
        ↓
Delete storage object
        ↓
Delete database record
        ↓
Commit
```

The storage object is deleted first. If that operation fails, the database record remains available for a later retry.

## Idempotency

Object storage delete operations are expected to be safe when the object is already absent. This covers the failure case where storage deletion succeeds but the database commit fails:

```text
Storage delete succeeds
        ↓
Database commit fails
        ↓
Next cleanup run
        ↓
Storage delete is repeated safely
        ↓
Database record is deleted
```

## Batch Processing

The worker processes at most `BatchSize` records per execution. The default is 100 records.

Bounded batches prevent a large expired backlog from causing excessive memory usage or long database transactions.

## Scheduling

`GalleryExportCleanupWorker` runs once at application startup and then periodically using `PeriodicTimer`.

Configuration section:

```json
{
  "GalleryExportCleanup": {
    "Enabled": true,
    "IntervalMinutes": 60,
    "BatchSize": 100
  }
}
```

Defaults are applied when the section is absent:

- Enabled: `true`
- Interval: `60` minutes
- Batch size: `100`

## Error Handling

- Cancellation during shutdown is treated as expected behavior.
- A storage or database failure is logged with export and event identifiers.
- Failed records remain in the database and are retried by a future execution.
- Other records in the same batch continue processing.

## Observability

Each execution logs:

- scanned record count
- deleted record count
- failed record count

Each successful or failed export cleanup is logged separately with structured identifiers.

## Tests

Integration tests cover:

- expired export object and database record are deleted
- unexpired export remains untouched
- database record remains when storage deletion fails

## Acceptance Criteria

- Expired completed exports are removed from object storage.
- Database records are deleted only after successful storage deletion.
- Unexpired exports are not deleted.
- Failed cleanup attempts remain retryable.
- Processing is bounded by a configurable batch size.
- The worker can be disabled by configuration.
