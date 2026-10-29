# Board preferences

`PATCH /api/Board/{boardId}/preferences` requires a bearer token.

```json
{ "isPinned": true, "markViewed": true }
```

- Send `isPinned: true/false` to set/unset the pin. Omit it to preserve the current value.
- Send `markViewed: true` to set `LastViewedAt` to the server's current UTC time.
- At least one operation is required. Empty requests return 400.
- Identity comes from JWT, never a client-supplied userId.
- Only direct project members can write. Workspace ownership alone is insufficient (403).
- Missing/deleted boards, projects or workspaces return 404.
- Creates the user's preference if absent. The existing entity initializes LastViewedAt on creation, including a first pin-only request. Later pin-only updates preserve LastViewedAt.
- Returns HTTP 200 with `ApiResponse<BoardPreferenceDto>` containing `boardId`, `isPinned`, `lastViewedAt`.
- Persistence failures use the existing logged HTTP 500 response; clients may silently handle view tracking failures. Pin failures should revert optimistic UI state.

No schema migration is required. Local handler tests use EF InMemory; PostgreSQL/JWT integration and concurrent first inserts have not been exercised.
