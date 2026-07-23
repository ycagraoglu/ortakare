# 005 — Orphan File Cleanup

## Problem

Object storage upload can succeed while the following database transaction fails. In that case, the object remains in R2 without an `EventGuestPhoto` or `GalleryExport` row and silently consumes storage.

## Goals

- Detect storage objects with no database reference.
- Delete only objects managed by Ortakare.
- Avoid deleting recently uploaded objects that may still be inside a valid request or recovery window.
- Bound storage listing and delete work per execution.
- Continue processing after an individual delete failure.

## Managed prefixes

Only these prefixes are scanned:

- `events/`
- `exports/`

Objects under any other prefix are never considered by this job.

## Safety rules

An object is deleted only when all conditions are true:

1. Its key starts with a managed prefix.
2. Its last-modified time is older than the configured grace period.
3. Its key does not exist in `EventGuestPhotos.StorageKey`.
4. Its key does not exist in `GalleryExports.StorageKey`.
5. The per-run delete limit has not been reached.

The default grace period is 24 hours. This protects uploads that temporarily exist before their database transaction completes.

## Configuration

```json
{
  "OrphanFileCleanup": {
    "Enabled": true,
    "IntervalHours": 24,
    "GracePeriodHours": 24,
    "MaxObjectsPerPrefix": 5000,
    "MaxDeletesPerRun": 200
  }
}
```

The options have the same safe defaults even when the section is omitted.

## Processing flow

```text
List events/ and exports/
        ↓
Load referenced photo/export keys from PostgreSQL
        ↓
Ignore recent and referenced objects
        ↓
Delete old unreferenced objects
        ↓
Log scanned/orphan/deleted/failed counts
```

## Failure behavior

A failed storage deletion is logged and processing continues with the next object. Since no database row exists for an orphan, there is no database mutation to roll back.

## Limits

This first production version scans a bounded number of objects per managed prefix. Very large buckets should later use a persistent continuation cursor so every page is eventually scanned without repeatedly starting from the first page.

## Tests

Integration tests cover:

- old unreferenced managed object is deleted;
- referenced object is preserved;
- recent object is preserved;
- unmanaged prefix is preserved;
- storage deletion failure leaves the object intact and increments the failure count.

## Acceptance criteria

- No object younger than the grace period is deleted.
- No database-referenced object is deleted.
- No unmanaged prefix is scanned or deleted.
- One deletion failure does not stop the batch.
- Work is bounded by listing and deletion limits.