---
name: Pre-reveal privacy
overview: Stop leaking other authors’ nicknames and pending action-item text before a retrospective is revealed.
todos:
  - id: hidden-counts
    content: Remove nicknames (and user ids) from HiddenAuthorCounts or drop the DTO until reveal
    status: pending
  - id: pending-filter
    content: Filter Pending Action Items like other columns until reveal unless created by the caller
    status: pending
  - id: frontend
    content: Adjust board UI that assumes pending items and author counts are always visible
    status: pending
isProject: false
---

# Pre-reveal privacy

## Problem

[GetRetrospectiveMappings.ToGetDto](RetroBackend/Mappings/GetRetrospectiveMappings.cs) hides other users’ stickies until `IsRevealed`, but:

- `HiddenAuthorCounts` still returns `CreatedBy`, `CreatedByNickname`, and `Count` per author.
- Action items in the column titled `"Pending Action Items"` are returned to everyone assigned before reveal (`c.Title == "Pending Action Items" || i.CreatedBy == currentUserId`).

## Impact

Participants learn who wrote how many items and can read pending action text before the facilitator reveals the board — against an anonymous retro model.

## Approach

- **Counts:** Either omit `HiddenAuthorCounts` until reveal, or return anonymous totals only (`count` with no `createdBy` / nickname). Pick one and update [GetRetrospectiveDtos](RetroBackend/Dtos/GetRetrospectiveDtos.cs) and the frontend that renders counts.
- **Pending items:** Use the same filter as regular items: `retro.IsRevealed || i.CreatedBy == currentUserId` (no title exception). If product needs carry-over visibility, document that as an explicit exception and still strip `CreatedByNickname` until reveal.
- Frontend: [ColumnView](RetroFrontend/src/components/ColumnView.tsx) / action column / detail page must tolerate empty pending lists and missing author lists.

Do not change reveal/close workflow ([RetrospectivesController.Reveal](RetroBackend/Controllers/RetrospectivesController.cs)).

## Files

- [RetroBackend/Mappings/GetRetrospectiveMappings.cs](RetroBackend/Mappings/GetRetrospectiveMappings.cs)
- [RetroBackend/Dtos/GetRetrospectiveDtos.cs](RetroBackend/Dtos/GetRetrospectiveDtos.cs)
- Frontend consumers of `hiddenAuthorCounts` and pending action columns (search `hiddenAuthor` / `Pending Action`)

## Tests / verify

- Unrevealed GET as user A: no other nicknames in JSON; no other users’ pending descriptions.
- After reveal: full items and authors as today.
- Manager owner unrevealed: confirm product choice (today they see others’ items only after reveal via the same mapping — keep that).

## Depends on

None. Independent of [10-closed-board-mutations.md](10-closed-board-mutations.md).
