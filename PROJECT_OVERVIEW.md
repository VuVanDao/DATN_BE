# 📘 TỔNG QUAN DỰ ÁN BACKEND - HỆ THỐNG QUẢN LÝ DỰ ÁN AGILE & AI (DATN_BE)

> **Tài liệu tóm tắt & ôn tập kiến trúc, tính năng, API và tiến độ dự án**  
> *Được cập nhật tự động dành cho lập trình viên khi quay trở lại dự án.*

---

## 🎯 1. GIỚI THIỆU DỰ ÁN

* **Tên dự án:** Hệ thống Quản lý Dự án Tích hợp Agile & AI (Trello / Jira / Asana Clone).
* **Mục tiêu:** Xây dựng nền tảng quản lý công việc và dự án hiện đại, kết hợp trải nghiệm kéo thả trực quan (Kanban) của Trello, quy trình quản lý dự án linh hoạt (Scrum/Sprint) của Jira, và trợ lý ảo AI hỗ trợ phân tích công việc.
* **Mục đích:** Đồ Án Tốt Nghiệp (DATN) ngành Kỹ thuật Phần mềm / CNTT.

---

## 🛠️ 2. STACK CÔNG NGHỆ (BACKEND)

| Thành phần | Công nghệ / Thư viện | Vai trò |
| :--- | :--- | :--- |
| **Framework** | **.NET 8 (ASP.NET Core Web API)** | Nền tảng backend hiệu năng cao, LTS |
| **Cơ sở dữ liệu** | **PostgreSQL** | Hệ quản trị CSDL quan hệ chính |
| **ORM** | **Entity Framework Core 8** | Code-First, Interceptor, Global Query Filters |
| **Kiến trúc** | **Modular Monolith + Clean Architecture + CQRS** | Tách module nghiệp vụ độc lập, phân tách tầng rõ ràng |
| **Điều hướng CQRS**| **MediatR** | Xử lý Commands, Queries, Domain Events |
| **Xác thực dữ liệu** | **FluentValidation** | Pipeline Behavior tự động validate request trước khi vào Handler |
| **Bảo mật & Auth** | **JWT Bearer (Access + Refresh Token)** | Phân quyền RBAC 3 cấp kết hợp Custom Policy (`[HasPermission]`) |
| **Lưu trữ ảnh/file** | **Cloudinary** | Lưu avatar, ảnh cover của task |
| **Gửi Email** | **Resend API (HttpClient)** | Gửi email kích hoạt, quên mật khẩu |
| **Tài liệu API** | **OpenAPI (Swagger)** | Tích hợp sẵn tại môi trường Development |

---

## 🏛️ 3. KIẾN TRÚC HỆ THỐNG & TỔ CHỨC SOURCE CODE

Dự án áp dụng mô hình **Modular Monolith** kết hợp nguyên lý **Clean Architecture** trong từng module:

```
Source/DATN_BE/DATN_BE/
│
├── Modules/                         # Các Module nghiệp vụ độc lập
│   ├── Identity/                    # Xác thực, tài khoản, phân quyền Role/Permission
│   │   ├── Domain/                  # Entities (User, Role, Permission, UserRole,...)
│   │   ├── Application/             # Commands, Queries, DTOs, Handlers, Validators
│   │   ├── Infrastructure/          # Services (JWT generator, hashing password,...)
│   │   └── Presentation/            # Controllers (AuthController, UsersController, AdminController)
│   │
│   ├── Workspaces/                  # Không gian làm việc nhóm & thành viên Workspace
│   ├── ProjectManagement/           # Dự án (Project), Bảng (Board), Cột (Column)
│   ├── Task/                        # Thẻ công việc (Card), Bình luận (Comment), Kéo thả (MoveCard)
│   ├── Security/                    # Lịch sử đăng nhập cá nhân (Audit Log, LoginHistory)
│   └── Notifications/               # Thông báo hệ thống & Real-time (Đang chuẩn bị hạ tầng)
│
└── Shared/                          # Hạ tầng dùng chung cho toàn bộ ứng dụng
    ├── Application/                 # Base Models (ApiResponse, PagedResult), Exceptions, Interfaces
    ├── Domain/                      # BaseEntity, ISoftDelete
    ├── Infrastructure/
    │   ├── Persistence/             # ApplicationDbContext, DataSeeder, Soft-delete Interceptor
    │   ├── Security/                # PermissionPolicyProvider, PermissionAuthorizationHandler
    │   └── Services/                # CloudinaryService, ResendEmailService, CurrentUserService
    └── Presentation/
        └── Middlewares/             # ExceptionHandlingMiddleware (Bắt lỗi toàn cục)
```

---

## 💎 4. CÁC CƠ CHẾ ĐẶC BIỆT ĐÃ ĐƯỢC XÂY DỰNG

### 1. Chuẩn hóa Phản hồi API ([`ApiResponse<T>`](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/Shared/Application/Models/ApiResponse.cs))
Tất cả endpoint đều trả về cấu trúc thống nhất:
```json
{
  "success": true,
  "message": "Thông báo",
  "data": { ... },
  "errors": null
}
```

### 2. Xóa mềm Toàn cục Tự động ([`ISoftDelete`](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/Shared/Infrastructure/Persistence/ApplicationDbContext.cs))
* **Khi xóa:** Chỉ cần gọi `_context.Cards.Remove(card);`, EF Core Interceptor tự động đổi lệnh `DELETE` thành `UPDATE IsDeleted = true, DeletedAt = DateTimeOffset.UtcNow`.
* **Khi truy vấn:** EF Core tự động gán Global Query Filter `WHERE IsDeleted = false` cho toàn bộ các entity kế thừa `ISoftDelete`.

### 3. Phân quyền RBAC 3 Cấp (Global ➔ Workspace ➔ Project)
* Quyền hạn linh hoạt dựa trên Permission chuỗi (ví dụ: `user:read`, `user:manage`, `role:manage`).
* Kiểm tra quyền thông qua Attribute `[HasPermission("...")]`.

### 4. Bắt lỗi Tập trung ([`ExceptionHandlingMiddleware`](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/Shared/Presentation/Middlewares/ExceptionHandlingMiddleware.cs))
* Tự động bắt các ngoại lệ nghiệp vụ như `NotFoundException` (trả về 404), `ValidationException` (trả về 400 kèm chi tiết các trường lỗi), `UnauthorizedException` (401),... mà không cần khối `try-catch` lặp đi lặp lại trong Controller.

### 5. Khởi tạo Dữ liệu Mẫu Tự động ([`DataSeeder`](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/Shared/Infrastructure/Persistence/DataSeeder.cs))
* Tự động khởi tạo Roles (`Admin`, `User`), danh sách Permissions chuẩn, và tài khoản Super Admin khi khởi động ứng dụng lần đầu.

---

## 📋 5. TỔNG HỢP CÁC CONTROLLER & API ENDPOINTS ĐANG HOẠT ĐỘNG

Dự án hiện có **8 Controllers**, **21 Commands** và **9 Queries** đã kết nối 100%:

### 1. [`AuthController`](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/Modules/Identity/Presentation/Controllers/AuthController.cs) (`/api/auth`)
* `POST /api/auth/register` : Đăng ký tài khoản.
* `POST /api/auth/login` : Đăng nhập (trả về AccessToken, RefreshToken).
* `POST /api/auth/refresh-token` : Gia hạn Access Token.
* `POST /api/auth/revoke-token` : Thu hồi Token (Đăng xuất).
* `POST /api/auth/forgot-password` : Gửi email đặt lại mật khẩu qua Resend.
* `POST /api/auth/reset-password` : Đặt lại mật khẩu mới.

### 2. [`UsersController`](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/Modules/Identity/Presentation/Controllers/UsersController.cs) (`/api/users`)
* `GET /api/users/me` : Lấy thông tin cá nhân của User hiện tại.
* `PUT /api/users/me` : Cập nhật thông tin (Username, Bio).
* `POST /api/users/change-password` : Đổi mật khẩu.
* `POST /api/users/me/avatar` : Upload ảnh đại diện lên Cloudinary (`multipart/form-data`).

### 3. [`SecurityController`](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/Modules/Security/Presentation/Controllers/SecurityController.cs) (`/api/security`)
* `GET /api/security/me/login-history` : Xem lịch sử đăng nhập cá nhân (IP, thiết bị, thời gian).

### 4. [`AdminController`](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/Modules/Identity/Presentation/Controllers/AdminController.cs) (`/api/admin`)
* `GET /api/admin/users` : Danh sách User toàn hệ thống (phân trang, tìm kiếm).
* `PATCH /api/admin/users/{id}/status` : Khóa / Mở khóa tài khoản.
* `POST /api/admin/users/{id}/roles` : Phân quyền Role cho User.
* `GET /api/admin/roles` : Danh sách Roles & Permissions.
* `GET /api/admin/permissions` : Toàn bộ danh mục Permissions.

### 5. [`WorkspacesController`](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/Modules/Workspaces/Presentation/Controllers/WorkspacesController.cs) (`/api/workspaces`)
* `POST /api/workspaces` : Tạo Không gian làm việc mới.
* `GET /api/workspaces` : Danh sách Workspace của người dùng hiện tại.
* `GET /api/workspaces/{workspaceId}/projects` : Danh sách dự án trong Workspace (phân trang).
* `POST /api/workspaces/{id}/members` : Thêm thành viên vào Workspace.

### 6. [`ProjectController`](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/Modules/ProjectManagement/Presentation/Controllers/ProjectController.cs) (`/api/project`)
* `POST /api/project/project` : Tạo mới Dự án trong Workspace.

### 7. [`BoardController`](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/Modules/ProjectManagement/Presentation/Controllers/BoardController.cs) (`/api/board`)
* `GET /api/board/{id}` : Chi tiết Bảng Kanban, danh sách Cột và Thẻ công việc.
* `POST /api/board/{id}/columns` : Tạo Cột mới trong Bảng.
* `PUT /api/board/{id}/columns/reorder` : Sắp xếp lại thứ tự Cột khi kéo thả.

### 8. [`CardsController`](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/Modules/Task/Presentation/Controller/CardsController.cs) (`/api/cards`)
* `POST /api/cards` : Tạo Thẻ công việc mới.
* `GET /api/cards/{cardId}` : Lấy chi tiết Thẻ kèm bình luận (Comments).
* `PUT /api/cards/{cardId}` : Cập nhật thông tin Thẻ (Title, Description, DueDate,...).
* `POST /api/cards/{cardId}/move` : **Kéo thả Card** (chuyển cột hoặc đổi vị trí trong cột, tự động re-index).
* `POST /api/cards/{id}/comments` : Thêm bình luận vào Thẻ.
* `DELETE /api/cards/{cardId}` : **Xóa thẻ** (Soft Delete).

---

## 🚀 6. HƯỚNG DẪN KHỞI CHẠY DỰ ÁN

### 1. Yêu cầu môi trường
* .NET SDK 8.0 trở lên.
* PostgreSQL 14+ (Port mặc định: `5432`).

### 2. Cấu hình Connection String
Mở file [appsettings.json](file:///d:/ForJavascript/TrelloClone/Source/DATN_BE/DATN_BE/appsettings.json):
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=TaskManagerDb;Username=postgres;Password=YOUR_PASSWORD"
}
```

### 3. Cập nhật Database & Chạy ứng dụng
Mở Terminal tại thư mục `Source/DATN_BE/DATN_BE`:
```bash
# 1. Áp dụng Migration vào PostgreSQL
dotnet ef database update

# 2. Khởi chạy ứng dụng
dotnet run
```
* **Base URL:** `http://localhost:5025`
* **Swagger/OpenAPI:** `http://localhost:5025/openapi/v1.json`

---

## 📌 7. LỘ TRÌNH TIẾP THEO (NEXT STEPS)

- [ ] **Frontend Angular:** Khởi tạo project Angular (Standalone Components + Angular CDK Drag & Drop).
- [ ] **Realtime (SignalR):** Hoàn thiện module `Notifications` và Hub SignalR để cập nhật bảng Kanban tức thời khi nhiều người cùng thao tác.
- [ ] **Sprint & Agile:** Bổ sung tính năng quản lý Sprint, Backlog, Burndown chart theo chuẩn Scrum.
- [ ] **Tích hợp AI:** Kết nối Semantic Kernel / OpenAI để tóm tắt task, gợi ý checklist hoặc ước lượng Story Points tự động.
