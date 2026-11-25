# Workspace/project authorization and management audit

Workspace ownership is resolved from `Workspace.OwnerId` and the actor from `ICurrentUserService` (JWT). An owner does not need a `WorkspaceMembers` or `ProjectMembers` row to exercise workspace ownership.

## Project data

- A WorkspaceOwner can read project boards, columns and cards without joining the project. Existing read rules for other users are retained.
- Creating, updating or deleting a board requires a direct `ProjectMembers` row with the project-scoped `ProjectLeader` role. Workspace ownership does not grant this role.
- Existing column/card writes, reordering, moving cards and adding comments require direct project membership through `EnsureCanModifyProjectAsync`. Workspace ownership does not bypass this requirement.
- When an owner joins a project, their project role determines these write permissions.
- Project creation retains its default-board setup as part of creating a project. Project/workspace deletion retains the cascade into child data, and membership removal retains assignee cleanup; these are management operations.
- Project metadata/lifecycle and membership administration retain their existing owner/leader policies and validations, including restrictions around removing a leader.

## Management audit

`ApplicationDbContext` captures successful authenticated changes to `Workspace`, `Project`, `WorkspaceMember` and `ProjectMember` in the existing `ActivityLogs` table. No schema migration is required.

- Actions: `Created`, `Updated`, `Deleted`, `MemberAdded`, `MemberRemoved`, `RoleChanged`.
- `UserId` identifies the actor; `CreatedAt` is UTC. `EntityType`/`EntityId` identify the workspace or project. Membership events target their parent resource.
- `DetailsJson` is a JSON string with `memberUserId` and `before`/`after` objects. Updated events include the changed scalar fields; membership role changes include previous/new role IDs. Create/remove snapshots include the relevant resource or membership fields.
- Timestamps maintained by EF do not create noise in audit history. No-op saves, reads and rejected commands do not generate management audit rows.
- Leader transfers generate an event for each changed member. Removing a workspace member also records each project membership removed by the cleanup.
- Workspace deletion tracks the cascade and writes project deletion events alongside the workspace event in one `SaveChanges` transaction. Existing deletion timestamps are preserved.
- Audit rows and business rows use the same save/transaction. Failed saves remove their generated audit rows from tracking so retries do not duplicate events.
- Seeder/background writes without an authenticated actor are excluded. Historical changes made before this feature are not reconstructed. Board/card activity and security failure logging are outside this management audit scope.

Example `DetailsJson` for a project rename:

```json
{"memberUserId":null,"before":{"name":"Project"},"after":{"name":"Renamed"}}
```

## Read APIs

- `GET /api/Workspaces/{workspaceId}/audit-logs?pageIndex=1&pageSize=20`: WorkspaceOwner only. Returns workspace metadata/lifecycle and workspace membership events.
- `GET /api/Project/{projectId}/audit-logs?pageIndex=1&pageSize=20`: WorkspaceOwner or the project's ProjectLeader. Returns project metadata/lifecycle and project membership events.

Both endpoints use bearer authentication and return `ApiResponse<PagedResult<ActivityLogDto>>`, ordered by `createdAt` descending, then `id` descending. `pageIndex` accepts 1–1,000,000; `pageSize` accepts 1–100. The default page contains up to 20 items. Each item includes `id`, `userId`, `username`, `entityType`, `entityId`, `actionType`, `detailsJson`, `createdAt`.

History remains readable after soft deletion of the target workspace/project, with the same ownership/leadership checks. A soft-deleted actor does not hide their historical events. A missing resource returns 404, insufficient privileges 403, invalid pagination 400, and an unauthenticated HTTP request 401. Empty history returns 200 with `items: []`.

## Verification

Run `dotnet test DATN_BE.slnx` from this directory. The tests exercise handlers and authorization against EF InMemory, including success/denial, cascade cleanup, audit snapshots, pagination and an injected save failure. HTTP tests use TestHost with synthetic authentication to check the new audit routes, response shape and 200/400/401/403 behavior. They do not establish live PostgreSQL transaction behavior or JWT validation.

If a running app/debugger locks the default build output, the isolated command used during implementation is:

```powershell
dotnet test DATN_BE.Tests/DATN_BE.Tests.csproj --no-restore -p:IntermediateOutputPath=../.verification/test-compile/ -p:OutputPath=../.verification/test-bin/ -p:UseAppHost=false
```
