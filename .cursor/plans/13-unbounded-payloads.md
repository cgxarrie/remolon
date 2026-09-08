---
name: Unbounded payloads and avatar cache
overview: Cap item and action-item description length and set private Cache-Control on avatar responses.
todos:
  - id: maxlength
    content: Add MaxLength on create/update item and action-item DTOs; match DB if you add a column max
    status: pending
  - id: kestrel
    content: Confirm request body limits; keep avatar 2MB as today
    status: pending
  - id: avatar-cache
    content: Cache-Control private on GET avatar; do not cache on shared CDNs
    status: pending
isProject: false
---

# Unbounded descriptions and avatar caching

## Problem

[CreateItemRequest](RetroBackend/Dtos/ItemDtos.cs) / `UpdateItemRequest` / action-item DTOs require `Description` but have no `[MaxLength]`. [Item.Description](RetroBackend/Models/Item.cs) maps to unbounded text in [RetroDbContext](RetroBackend/Data/RetroDbContext.cs). Avatars are capped at 2 MB in [AvatarImage](RetroBackend/Services/AvatarImage.cs) and [AvatarsController](RetroBackend/Controllers/AvatarsController.cs), but `File(...)` has no `Cache-Control`. Authenticated org-scoped GET can still be stored by shared caches if headers are wrong.

## Impact

Large JSON bodies and huge text fields cause CPU, memory, and DB bloat. Mis-cached avatars could leak to another user on a shared proxy (lower risk with `Authorization` as a cache key, but explicit `private` is safer).

## Approach

- Add `[MaxLength(4000)]` (or similar) on create/update description fields; return `400` on oversize.
- Optionally set `HasMaxLength` on the EF property (migration).
- Leave avatar size as 2 MB; keep magic-byte checks.
- On `Get` avatar: `Response.Headers.CacheControl = "private, max-age=3600"` (and `Vary: Authorization` if needed). `404`/`403` unchanged (same-org check stays).

## Files

- [RetroBackend/Dtos/ItemDtos.cs](RetroBackend/Dtos/ItemDtos.cs)
- [RetroBackend/Data/RetroDbContext.cs](RetroBackend/Data/RetroDbContext.cs) + migration if column length is set
- [RetroBackend/Controllers/AvatarsController.cs](RetroBackend/Controllers/AvatarsController.cs)
- [RetroBackend/Program.cs](RetroBackend/Program.cs) only if you set global `RequestSizeLimit`

## Tests / verify

- POST item with 5001-char description → 400.
- Valid short description → 201.
- `GET /api/users/{id}/avatar` includes `Cache-Control: private`.
- Cross-org avatar GET still 403.

## Depends on

None.
