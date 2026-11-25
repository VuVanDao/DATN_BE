const fs = require('fs');
const path = require('path');

const variables = [
  { key: 'baseUrl', value: 'http://localhost:5025', type: 'string' },
  { key: 'accessToken', value: '', type: 'string' },
  { key: 'loginEmail', value: 'admin@gmail.com', type: 'string' },
  { key: 'loginPassword', value: 'Dao11082004@', type: 'string' },
  { key: 'registerEmail', value: 'member_demo@gmail.com', type: 'string' },
  { key: 'registerPassword', value: 'Password123@', type: 'string' },
  { key: 'workspaceId', value: '01a1109d-851f-7597-8212-bc3b2da26599', type: 'string' },
  { key: 'workspaceSlug', value: 'workspace-so-1-name', type: 'string' },
  { key: 'projectId', value: '01a18563-4b9c-7154-8b7f-27f7a882b42d', type: 'string' },
  { key: 'projectKey', value: 'TUP289021', type: 'string' },
  { key: 'boardId', value: '01a18563-4bb7-72c4-bb89-9cce7209b9bb', type: 'string' },
  { key: 'columnId', value: '01a19316-b852-7751-8842-c32ee8c8ecf4', type: 'string' },
  { key: 'cardId', value: '01a19546-e44d-7f06-9ade-0a0fdbdaec43', type: 'string' },
  { key: 'userId', value: '01a0f716-70d9-7c16-9750-b9a13ec47823', type: 'string' },
  { key: 'boardSlug', value: 'bang-1', type: 'string' },
  { key: 'sprintId', value: '01a19546-e44d-7f06-9ade-0a0fdbdaec44', type: 'string' },
  { key: 'dependencyId', value: '01a19546-e44d-7f06-9ade-0a0fdbdaec45', type: 'string' },
  { key: 'notificationId', value: '01a19546-e44d-7f06-9ade-0a0fdbdaec46', type: 'string' }
];

// Helper to build environment object
function buildEnvironment(name) {
  return {
    id: 'e81d89e4-6992-4f36-9b62-38d7bf837a71',
    name: name,
    values: variables.map(v => ({
      key: v.key,
      value: v.value,
      type: v.key.toLowerCase().includes('password') || v.key === 'accessToken' ? 'secret' : 'default',
      enabled: true
    })),
    _postman_variable_scope: 'environment',
    _postman_exported_at: new Date().toISOString(),
    _postman_exported_using: 'Postman/11.0.0'
  };
}

// Helper for raw json request
function createJsonRequest(name, method, urlPath, bodyObj = null, queryParams = [], description = '', noAuth = false, testScript = null) {
  const pathSegments = urlPath.replace(/^\//, '').split('/');
  const rawUrl = `{{baseUrl}}/${urlPath.replace(/^\//, '')}${queryParams.length > 0 ? '?' + queryParams.map(q => `${q.key}=${q.value}`).join('&') : ''}`;

  const req = {
    name,
    request: {
      method,
      header: [
        {
          key: 'Content-Type',
          value: 'application/json',
          type: 'text'
        }
      ],
      url: {
        raw: rawUrl,
        host: ['{{baseUrl}}'],
        path: pathSegments,
        query: queryParams.map(q => ({
          key: q.key,
          value: q.value,
          description: q.description || '',
          disabled: q.disabled || false
        }))
      },
      description
    },
    response: []
  };

  if (noAuth) {
    req.request.auth = {
      type: 'noauth'
    };
  }

  if (bodyObj !== null && method !== 'GET') {
    req.request.body = {
      mode: 'raw',
      raw: JSON.stringify(bodyObj, null, 2),
      options: {
        raw: {
          language: 'json'
        }
      }
    };
  }

  if (testScript) {
    req.event = [
      {
        listen: 'test',
        script: {
          exec: testScript,
          type: 'text/javascript'
        }
      }
    ];
  }

  return req;
}

// Helper for formdata request
function createFormDataRequest(name, method, urlPath, formFields = [], description = '', testScript = null) {
  const pathSegments = urlPath.replace(/^\//, '').split('/');
  const req = {
    name,
    request: {
      method,
      header: [],
      body: {
        mode: 'formdata',
        formdata: formFields.map(f => ({
          key: f.key,
          type: f.type || 'text',
          value: f.type === 'file' ? '' : (f.value || ''),
          description: f.description || '',
          src: f.type === 'file' ? [] : undefined
        }))
      },
      url: {
        raw: `{{baseUrl}}/${urlPath.replace(/^\//, '')}`,
        host: ['{{baseUrl}}'],
        path: pathSegments
      },
      description
    },
    response: []
  };

  if (testScript) {
    req.event = [
      {
        listen: 'test',
        script: {
          exec: testScript,
          type: 'text/javascript'
        }
      }
    ];
  }

  return req;
}

// Test Scripts
const authLoginScript = [
  "if (pm.response.code === 200) {",
  "    var response = pm.response.json();",
  "    var token = (response.data && (response.data.token || response.data.accessToken)) || null;",
  "    if (token) {",
  "        pm.collectionVariables.set('accessToken', token);",
  "        try { pm.environment.set('accessToken', token); } catch(e) {}",
  "        console.log('✅ AccessToken updated successfully in Environment and Collection.');",
  "    }",
  "    if (response.data && response.data.id) {",
  "        pm.collectionVariables.set('userId', response.data.id);",
  "        try { pm.environment.set('userId', response.data.id); } catch(e) {}",
  "    }",
  "}"
];

const createWorkspaceScript = [
  "if (pm.response.code === 200) {",
  "    var response = pm.response.json();",
  "    if (response.data) {",
  "        if (response.data.id) {",
  "            pm.collectionVariables.set('workspaceId', response.data.id);",
  "            try { pm.environment.set('workspaceId', response.data.id); } catch(e) {}",
  "        }",
  "        if (response.data.slug) {",
  "            pm.collectionVariables.set('workspaceSlug', response.data.slug);",
  "            try { pm.environment.set('workspaceSlug', response.data.slug); } catch(e) {}",
  "        }",
  "        console.log('✅ Workspace variables captured: ID=' + response.data.id + ', Slug=' + response.data.slug);",
  "    }",
  "}"
];

const createProjectScript = [
  "if (pm.response.code === 200) {",
  "    var response = pm.response.json();",
  "    if (response.data) {",
  "        if (response.data.id) {",
  "            pm.collectionVariables.set('projectId', response.data.id);",
  "            try { pm.environment.set('projectId', response.data.id); } catch(e) {}",
  "        }",
  "        if (response.data.projectKey) {",
  "            pm.collectionVariables.set('projectKey', response.data.projectKey);",
  "            try { pm.environment.set('projectKey', response.data.projectKey); } catch(e) {}",
  "        }",
  "        console.log('✅ Project variables captured: ID=' + response.data.id + ', Key=' + response.data.projectKey);",
  "    }",
  "}"
];

const createBoardScript = [
  "if (pm.response.code === 200) {",
  "    var response = pm.response.json();",
  "    if (response.data) {",
  "        if (response.data.id) {",
  "            pm.collectionVariables.set('boardId', response.data.id);",
  "            try { pm.environment.set('boardId', response.data.id); } catch(e) {}",
  "        }",
  "        if (response.data.slug) {",
  "            pm.collectionVariables.set('boardSlug', response.data.slug);",
  "            try { pm.environment.set('boardSlug', response.data.slug); } catch(e) {}",
  "        }",
  "        console.log('✅ Board variables captured: ID=' + response.data.id + ', Slug=' + response.data.slug);",
  "    }",
  "}"
];

const createColumnScript = [
  "if (pm.response.code === 200) {",
  "    var response = pm.response.json();",
  "    if (response.data && response.data.id) {",
  "        pm.collectionVariables.set('columnId', response.data.id);",
  "        try { pm.environment.set('columnId', response.data.id); } catch(e) {}",
  "        console.log('✅ Column variable captured: ID=' + response.data.id);",
  "    }",
  "}"
];

const createCardScript = [
  "if (pm.response.code === 200) {",
  "    var response = pm.response.json();",
  "    if (response.data && response.data.id) {",
  "        pm.collectionVariables.set('cardId', response.data.id);",
  "        try { pm.environment.set('cardId', response.data.id); } catch(e) {}",
  "        console.log('✅ Card variable captured: ID=' + response.data.id);",
  "    }",
  "}"
];

const createSprintScript = [
  "if (pm.response.code === 200) {",
  "    var response = pm.response.json();",
  "    if (response.data && response.data.id) {",
  "        pm.collectionVariables.set('sprintId', response.data.id);",
  "        try { pm.environment.set('sprintId', response.data.id); } catch(e) {}",
  "        console.log('✅ Sprint variable captured: ID=' + response.data.id);",
  "    }",
  "}"
];

// Modules and Endpoints
const collection = {
  info: {
    _postman_id: 'd91fa591-1b94-4340-9a29-b472e391cb4d',
    name: 'DATN - Task Management & AI Collaboration API',
    description: 'Bộ sưu tập API cho Đồ Án Tốt Nghiệp: Xây dựng hệ thống quản lý công việc và cộng tác nhóm tích hợp trí tuệ nhân tạo.\\nCông nghệ: .NET 8 (Clean Architecture + Modular Monolith + CQRS MediatR) & Angular v22.',
    schema: 'https://schema.getpostman.com/json/collection/v2.1.0/collection.json'
  },
  auth: {
    type: 'bearer',
    bearer: [
      {
        key: 'token',
        value: '{{accessToken}}',
        type: 'string'
      }
    ]
  },
  variable: variables.map(v => ({
    key: v.key,
    value: v.value,
    type: v.type
  })),
  item: [
    // 1. Authentication
    {
      name: '01. Authentication (Xác thực)',
      item: [
        createJsonRequest(
          'Đăng nhập hệ thống (Login)',
          'POST',
          'api/auth/login',
          {
            email: '{{loginEmail}}',
            password: '{{loginPassword}}'
          },
          [],
          'Đăng nhập hệ thống bằng email và mật khẩu (biến {{loginEmail}} và {{loginPassword}}). Tự động lưu token.',
          true,
          authLoginScript
        ),
        createJsonRequest(
          'Đăng ký tài khoản (Register)',
          'POST',
          'api/auth/register',
          {
            email: '{{registerEmail}}',
            username: 'member_demo',
            password: '{{registerPassword}}',
            bio: 'Sinh viên thực hiện đồ án tốt nghiệp'
          },
          [],
          'Đăng ký tài khoản người dùng mới vào hệ thống. Tự động lưu token.',
          true,
          authLoginScript
        ),
        createJsonRequest(
          'Cấp lại Access Token (Refresh Token)',
          'POST',
          'api/auth/refresh-token',
          {
            refreshToken: ''
          },
          [],
          'Cấp Access Token mới từ Refresh Token lưu trong Cookie hoặc truyền trong Body.',
          true,
          authLoginScript
        ),
        createJsonRequest(
          'Đăng xuất / Thu hồi Token (Revoke Token)',
          'POST',
          'api/auth/revoke-token',
          {
            refreshToken: ''
          },
          [],
          'Thu hồi Refresh Token và xóa Cookie đăng nhập.',
          true
        ),
        createJsonRequest(
          'Yêu cầu đặt lại mật khẩu (Forgot Password)',
          'POST',
          'api/auth/forgot-password',
          {
            email: '{{loginEmail}}'
          },
          [],
          'Gửi email chứa mã Token khôi phục mật khẩu đến hòm thư người dùng (sử dụng {{loginEmail}}).',
          true
        ),
        createJsonRequest(
          'Xác nhận đặt lại mật khẩu (Reset Password)',
          'POST',
          'api/auth/reset-password',
          {
            email: '{{loginEmail}}',
            token: 'RESET_TOKEN_FROM_EMAIL',
            newPassword: '{{loginPassword}}',
            confirmNewPassword: '{{loginPassword}}'
          },
          [],
          'Nhập mã token từ email và thiết lập mật khẩu mới.',
          true
        )
      ]
    },

    // 2. User Profile
    {
      name: '02. User Profile (Hồ sơ cá nhân)',
      item: [
        createJsonRequest(
          'Lấy thông tin cá nhân (Get My Profile)',
          'GET',
          'api/users/me',
          null,
          [],
          'Lấy thông tin tài khoản hiện tại đang đăng nhập qua JWT Bearer Token.'
        ),
        createJsonRequest(
          'Cập nhật thông tin cá nhân (Update Profile)',
          'PUT',
          'api/users/me',
          {
            username: 'Dao Le (Updated)',
            bio: 'Fullstack Software Engineer & AI Researcher'
          },
          [],
          'Cập nhật tên hiển thị (Username) và tiểu sử (Bio).'
        ),
        createJsonRequest(
          'Đổi mật khẩu (Change Password)',
          'POST',
          'api/users/change-password',
          {
            currentPassword: '{{loginPassword}}',
            newPassword: 'NewPassword123@',
            confirmNewPassword: 'NewPassword123@'
          },
          [],
          'Đổi mật khẩu cho người dùng đang đăng nhập.'
        ),
        createFormDataRequest(
          'Upload ảnh đại diện (Upload Avatar Cloudinary)',
          'POST',
          'api/users/me/avatar',
          [
            { key: 'File', type: 'file', description: 'Chọn tệp hình ảnh đại diện (JPG, PNG, WEBP)' }
          ],
          'Tải ảnh đại diện lên Cloudinary thông qua ICloudinaryService và lưu URL vào hồ sơ.'
        )
      ]
    },

    // 3. System Administration
    {
      name: '03. System Administration (Quản trị hệ thống - Admin)',
      item: [
        createJsonRequest(
          '[Admin] Danh sách người dùng (Get Users - Paged)',
          'GET',
          'api/admin/users',
          null,
          [
            { key: 'search', value: '', description: 'Tìm theo username hoặc email', disabled: true },
            { key: 'isActive', value: 'true', description: 'Lọc trạng thái hoạt động', disabled: true },
            { key: 'pageIndex', value: '1', description: 'Trang hiện tại' },
            { key: 'pageSize', value: '10', description: 'Số bản ghi trên trang' }
          ],
          'Yêu cầu vai trò SystemAdmin. Lấy danh sách toàn bộ người dùng kèm phân trang.'
        ),
        createJsonRequest(
          '[Admin] Tạo người dùng mới (Create User)',
          'POST',
          'api/admin/users',
          {
            email: 'member01@gmail.com',
            username: 'member01',
            password: 'Password123@',
            bio: 'Team Member'
          },
          [],
          'Tạo mới tài khoản người dùng trực tiếp từ trang Admin.'
        ),
        createJsonRequest(
          '[Admin] Cập nhật người dùng (Update User)',
          'PUT',
          'api/admin/users/{{userId}}',
          {
            username: 'member01_updated',
            bio: 'Senior Backend Engineer'
          },
          [],
          'Cập nhật thông tin người dùng theo Id.'
        ),
        createJsonRequest(
          '[Admin] Khóa / Mở khóa tài khoản (Update Status)',
          'PATCH',
          'api/admin/users/{{userId}}/status',
          {
            isActive: false,
            reason: 'Tạm khóa để kiểm tra bảo mật'
          },
          [],
          'Khóa (isActive=false) hoặc mở khóa (isActive=true) tài khoản người dùng.'
        ),
        createJsonRequest(
          '[Admin] Gán vai trò cho người dùng (Assign Role)',
          'POST',
          'api/admin/users/{{userId}}/role',
          {
            roleId: '01a0f716-70d9-7c16-9750-b9a13ec47800'
          },
          [],
          'Gán Role (SystemAdmin hoặc User) cho tài khoản.'
        ),
        createJsonRequest(
          '[Admin] Xóa mềm người dùng (Delete User)',
          'DELETE',
          'api/admin/users/{{userId}}',
          null,
          [],
          'Xóa mềm tài khoản người dùng (IsDeleted = true).'
        ),
        createJsonRequest(
          '[Admin] Danh sách tất cả vai trò (Get Roles)',
          'GET',
          'api/admin/roles',
          null,
          [],
          'Lấy danh sách các vai trò (Global, Workspace, Project) trong hệ thống.'
        )
      ]
    },

    // 4. Security & Audit
    {
      name: '04. Security & Audit (Bảo mật & Nhật ký)',
      item: [
        createJsonRequest(
          'Lịch sử đăng nhập cá nhân (My Login History)',
          'GET',
          'api/security/me/login-history',
          null,
          [
            { key: 'limit', value: '20', description: 'Số lượng bản ghi lấy về' }
          ],
          'Xem danh sách lịch sử đăng nhập gồm IP, Thiết bị, Trình duyệt, Trạng thái thành công/thất bại.'
        )
      ]
    },

    // 5. Workspaces
    {
      name: '05. Workspaces (Không gian làm việc)',
      item: [
        createJsonRequest(
          'Tạo Không gian làm việc mới (Create Workspace)',
          'POST',
          'api/workspaces',
          {
            name: 'Workspace Đồ Án Tốt Nghiệp',
            description: 'Không gian làm việc chính cho nhóm phát triển hệ thống Task Management AI'
          },
          [],
          'Tạo mới Workspace. Tự động lưu workspaceId và workspaceSlug vừa tạo vào biến môi trường.',
          false,
          createWorkspaceScript
        ),
        createJsonRequest(
          'Lấy danh sách Workspace của tôi (Get My Workspaces)',
          'GET',
          'api/workspaces',
          null,
          [],
          'Lấy toàn bộ Workspace mà người dùng đang sở hữu hoặc tham gia thành viên.'
        ),
        createJsonRequest(
          'Chi tiết Workspace theo Slug (Get By Slug)',
          'GET',
          'api/workspaces/{{workspaceSlug}}',
          null,
          [],
          'Lấy thông tin chi tiết Workspace bao gồm số lượng dự án và thành viên theo Slug URL.'
        ),
        createFormDataRequest(
          'Cập nhật thông tin Workspace (Update Workspace)',
          'PUT',
          'api/workspaces/{{workspaceId}}',
          [
            { key: 'Name', value: 'Workspace Đồ Án Tốt Nghiệp (Updated)', description: 'Tên workspace' },
            { key: 'Description', value: 'Mô tả cập nhật', description: 'Mô tả' },
            { key: 'Logo', type: 'file', description: 'Logo đại diện Workspace (Ảnh)' },
            { key: 'CoverImage', type: 'file', description: 'Ảnh bìa Workspace (Ảnh)' }
          ],
          'Cập nhật tên, mô tả, logo và cover image cho Workspace (Yêu cầu WorkspaceOwner).'
        ),
        createJsonRequest(
          'Danh sách Dự án trong Workspace (Get Projects)',
          'GET',
          'api/workspaces/{{workspaceId}}/projects',
          null,
          [
            { key: 'pageIndex', value: '1', description: 'Trang hiện tại' },
            { key: 'pageSize', value: '20', description: 'Số lượng dự án trên trang' }
          ],
          'Lấy danh sách các Dự án (Projects) thuộc một Workspace có phân trang.'
        ),
        createJsonRequest(
          'Danh sách thành viên Workspace (Get Members)',
          'GET',
          'api/workspaces/{{workspaceId}}/members',
          null,
          [
            { key: 'pageIndex', value: '1', description: 'Trang hiện tại' },
            { key: 'pageSize', value: '20', description: 'Số lượng thành viên trên trang' }
          ],
          'Lấy danh sách thành viên trong Workspace kèm vai trò (WorkspaceOwner/WorkspaceMember).'
        ),
        createJsonRequest(
          'Thêm thành viên vào Workspace (Add Members)',
          'POST',
          'api/workspaces/{{workspaceId}}/members',
          {
            members: [
              {
                email: 'member01@gmail.com',
                roleId: null
              },
              {
                email: 'member02@gmail.com',
                roleId: null
              }
            ]
          },
          [],
          'Mời nhiều thành viên vào Workspace bằng địa chỉ email.'
        ),
        createJsonRequest(
          'Xem ảnh hưởng trước khi xóa thành viên (Removal Impact)',
          'GET',
          'api/workspaces/{{workspaceId}}/members/{{userId}}/removal-impact',
          null,
          [],
          'Kiểm tra thành viên đang là Trưởng dự án nào hoặc đang giữ bao nhiêu card trước khi quyết định gỡ khỏi Workspace.'
        ),
        createJsonRequest(
          'Xóa thành viên khỏi Workspace (Remove Member)',
          'DELETE',
          'api/workspaces/{{workspaceId}}/members/{{userId}}',
          null,
          [],
          'Gỡ bỏ thành viên khỏi Workspace (hoặc thành viên tự rời).'
        ),
        createJsonRequest(
          'Danh sách vai trò trong Workspace (Get Roles)',
          'GET',
          'api/workspaces/roles',
          null,
          [],
          'Lấy danh sách các vai trò phạm vi Workspace (WorkspaceOwner, WorkspaceMember).'
        ),
        createJsonRequest(
          'Nhật ký quản trị Workspace (Workspace Audit Logs)',
          'GET',
          'api/workspaces/{{workspaceId}}/audit-logs',
          null,
          [
            { key: 'pageIndex', value: '1', description: 'Trang hiện tại' },
            { key: 'pageSize', value: '20', description: 'Số lượng nhật ký' }
          ],
          'Xem lịch sử các thao tác quản trị diễn ra trên Workspace (Chỉ Owner).'
        ),
        createJsonRequest(
          'Xóa Workspace (Delete Workspace)',
          'DELETE',
          'api/workspaces/{{workspaceId}}',
          null,
          [],
          'Xóa mềm Workspace (Đặt ở cuối phân hệ Workspace để không ảnh hưởng các test phía trước).'
        )
      ]
    },

    // 6. Projects
    {
      name: '06. Projects (Quản lý Dự án)',
      item: [
        createJsonRequest(
          'Tạo Dự án mới trong Workspace (Create Project)',
          'POST',
          'api/project',
          {
            workspaceId: '{{workspaceId}}',
            name: 'Task Management AI System',
            projectKey: 'TMAI',
            description: 'Dự án xây dựng nền tảng quản lý công việc và cộng tác tích hợp trí tuệ nhân tạo',
            isAssignAsLeader: true
          },
          [],
          'Tạo một Project mới trực thuộc Workspace. Tự động lưu projectId và projectKey vừa tạo.',
          false,
          createProjectScript
        ),
        createJsonRequest(
          'Dự án tôi đã tham gia (Get My Projects)',
          'GET',
          'api/project/my-projects',
          null,
          [
            { key: 'workspaceId', value: '{{workspaceId}}', description: 'Lọc theo workspace tùy chọn', disabled: true }
          ],
          'Lấy danh sách dự án mà người dùng hiện tại tham gia thành viên, nhóm theo Workspace.'
        ),
        createJsonRequest(
          'Chi tiết Dự án (Get Project Detail)',
          'GET',
          'api/project/{{projectKey}}/workspace/{{workspaceId}}',
          null,
          [],
          'Lấy thông tin chi tiết một Dự án theo ProjectKey và WorkspaceId.'
        ),
        createJsonRequest(
          'Cập nhật thông tin Dự án (Update Project)',
          'PUT',
          'api/project/{{projectId}}',
          {
            name: 'Task Management AI System (v2)',
            projectKey: 'TMAI2',
            description: 'Mô tả dự án được cập nhật'
          },
          [],
          'Cập nhật tên, mã viết tắt (projectKey) và mô tả dự án (WorkspaceOwner hoặc ProjectLeader).'
        ),
        createJsonRequest(
          'Danh sách thành viên Dự án (Get Members)',
          'GET',
          'api/project/{{projectId}}/members',
          null,
          [],
          'Lấy danh sách thành viên trong Dự án kèm chức danh (JobTitle) và vai trò (Leader/Member).'
        ),
        createJsonRequest(
          'Thêm thành viên vào Dự án (Add Members)',
          'POST',
          'api/project/{{projectId}}/members',
          {
            members: [
              {
                userId: '{{userId}}',
                roleId: null,
                jobTitle: 'Frontend Engineer'
              }
            ]
          },
          [],
          'Thêm các thành viên từ Workspace vào trong Dự án này.'
        ),
        createJsonRequest(
          'Cập nhật vai trò thành viên Dự án (Update Member Role)',
          'PUT',
          'api/project/{{projectId}}/members/{{userId}}/role',
          {
            newRoleId: '01a0f716-70d9-7c16-9750-b9a13ec47805'
          },
          [],
          'Chuyển giao quyền Trưởng dự án (ProjectLeader) hoặc đổi sang ProjectMember.'
        ),
        createJsonRequest(
          'Cập nhật chức danh chuyên môn (Update Job Title)',
          'PUT',
          'api/project/{{projectId}}/members/{{userId}}/job-title',
          {
            jobTitle: 'Senior Fullstack Engineer'
          },
          [],
          'Cập nhật chức danh công việc của thành viên trong dự án (VD: Backend Lead, QA Tester).'
        ),
        createJsonRequest(
          'Xóa thành viên khỏi Dự án (Remove Member)',
          'DELETE',
          'api/project/{{projectId}}/members/{{userId}}',
          null,
          [],
          'Gỡ thành viên khỏi Dự án.'
        ),
        createJsonRequest(
          'Danh sách vai trò trong Dự án (Get Roles)',
          'GET',
          'api/project/roles',
          null,
          [],
          'Lấy danh sách các vai trò phạm vi Project (ProjectLeader, ProjectMember).'
        ),
        createJsonRequest(
          'Nhật ký quản trị Dự án (Project Audit Logs)',
          'GET',
          'api/project/{{projectId}}/audit-logs',
          null,
          [
            { key: 'pageIndex', value: '1', description: 'Trang hiện tại' },
            { key: 'pageSize', value: '20', description: 'Số lượng nhật ký' }
          ],
          'Lấy lịch sử hoạt động và các thay đổi quản trị trong Dự án.'
        ),
        createJsonRequest(
          'Xóa mềm Dự án (Delete Project)',
          'DELETE',
          'api/project/{{projectId}}',
          null,
          [],
          'Xóa mềm Dự án và toàn bộ Bảng, Cột, Thẻ liên quan (Đặt ở cuối phân hệ Dự án).'
        )
      ]
    },

    // 7. Boards & Columns
    {
      name: '07. Boards & Columns (Bảng Kanban & Cột)',
      item: [
        createJsonRequest(
          'Tạo Bảng mới trong Dự án (Create Board)',
          'POST',
          'api/board',
          {
            projectId: '{{projectId}}',
            title: 'Bảng Phát Triển Phần Mềm',
            slug: 'bang-phat-trien-phan-mem',
            type: 'Kanban'
          },
          [],
          'Tạo mới Bảng công việc. Tự động lưu boardId và boardSlug vừa tạo.',
          false,
          createBoardScript
        ),
        createJsonRequest(
          'Danh sách Bảng trong Dự án (Get Project Boards)',
          'GET',
          'api/board/project/{{projectId}}',
          null,
          [],
          'Lấy danh sách tất cả các Bảng thuộc một Dự án.'
        ),
        createJsonRequest(
          'Danh sách Bảng cá nhân (My Boards - Recent & Pinned)',
          'GET',
          'api/board/my-boards',
          null,
          [
            { key: 'limit', value: '10', description: 'Số lượng bảng tối đa mỗi nhóm' }
          ],
          'Lấy danh sách các Bảng vừa xem gần đây và các Bảng đã ghim của người đăng nhập.'
        ),
        createJsonRequest(
          'Chi tiết Bảng kèm Cột & Thẻ (Get Board Detail)',
          'GET',
          'api/board/{{boardSlug}}/project/{{projectId}}',
          null,
          [
            { key: 'sprintId', value: '{{sprintId}}', description: 'Lọc thẻ theo Sprint nếu là bảng Scrum', disabled: true }
          ],
          'Lấy chi tiết bảng Kanban, bao gồm các Cột và danh sách Thẻ công việc sắp xếp theo vị trí Position.'
        ),
        createJsonRequest(
          'Cập nhật thông tin Bảng (Update Board)',
          'PUT',
          'api/board/{{boardId}}',
          {
            title: 'Bảng Phát Triển Phần Mềm (Updated)',
            slug: 'bang-phat-trien-v2',
            type: 'Kanban'
          },
          [],
          'Cập nhật tiêu đề, đường dẫn slug và loại bảng.'
        ),
        createJsonRequest(
          'Ghim hoặc Đánh dấu đã xem Bảng (Update Preference)',
          'PATCH',
          'api/board/{{boardId}}/preferences',
          {
            isPinned: true,
            markViewed: true
          },
          [],
          'Ghim bảng lên đầu danh sách hoặc ghi nhận lượt xem mới nhất.'
        ),
        createJsonRequest(
          'Tạo Cột mới trong Bảng (Create Column)',
          'POST',
          'api/board/{{boardId}}/columns',
          {
            title: 'In Review',
            isDoneColumn: false
          },
          [],
          'Tạo một Cột mới trong quy trình làm việc. Tự động lưu columnId vừa tạo.',
          false,
          createColumnScript
        ),
        createJsonRequest(
          'Cập nhật thông tin Cột (Update Column)',
          'PUT',
          'api/board/{{boardId}}/columns/{{columnId}}',
          {
            title: 'Done / Completed',
            isDoneColumn: true
          },
          [],
          'Cập nhật tiêu đề cột hoặc đánh dấu cột này là cột Hoàn thành công việc (IsDoneColumn).'
        ),
        createJsonRequest(
          'Sắp xếp lại thứ tự Cột (Reorder Columns)',
          'PUT',
          'api/board/{{boardId}}/columns/reorder',
          {
            orderedColumnIds: [
              '{{columnId}}'
            ]
          },
          [],
          'Cập nhật thứ tự các Cột sau khi người dùng kéo thả trên giao diện Kanban.'
        ),
        createJsonRequest(
          'Lịch sử hoạt động của Cột (Column Activities)',
          'GET',
          'api/board/columns/{{columnId}}/activities',
          null,
          [
            { key: 'pageIndex', value: '1', description: 'Trang' },
            { key: 'pageSize', value: '20', description: 'Số lượng' }
          ],
          'Lấy nhật ký hoạt động (thẻ được chuyển đến/đi) của Cột.'
        ),
        createJsonRequest(
          'Xóa Cột trong Bảng (Delete Column)',
          'DELETE',
          'api/board/{{boardId}}/columns/{{columnId}}',
          null,
          [],
          'Xóa mềm Cột và các Thẻ bên trong Cột.'
        ),
        createJsonRequest(
          'Xóa mềm Bảng (Delete Board)',
          'DELETE',
          'api/board/{{boardId}}',
          null,
          [],
          'Xóa mềm Bảng và toàn bộ Cột, Thẻ bên trong (Đặt ở cuối phân hệ Bảng).'
        )
      ]
    },

    // 8. Sprints & Agile Backlog
    {
      name: '08. Sprints & Agile Backlog (Quản lý Sprint & Backlog)',
      item: [
        createJsonRequest(
          'Tạo mới Sprint cho Dự án (Create Sprint)',
          'POST',
          'api/sprint',
          {
            projectId: '{{projectId}}',
            boardId: '{{boardId}}',
            name: 'Sprint 1 - Foundation & Auth',
            goal: 'Hoàn thiện luồng đăng ký, đăng nhập và cấu trúc bảng Kanban',
            startDate: '2026-11-20T00:00:00Z',
            endDate: '2026-12-04T00:00:00Z'
          },
          [],
          'Tạo mới một Sprint với trạng thái ban đầu là Future. Tự động lưu sprintId vừa tạo.',
          false,
          createSprintScript
        ),
        createJsonRequest(
          'Cập nhật thông tin Sprint (Update Sprint)',
          'PUT',
          'api/sprint/{{sprintId}}',
          {
            name: 'Sprint 1 - Foundation & Core Features',
            goal: 'Mục tiêu hoàn thành giao diện kéo thả Kanban',
            startDate: '2026-11-20T00:00:00Z',
            endDate: '2026-12-04T00:00:00Z'
          },
          [],
          'Cập nhật tên, mục tiêu và thời hạn chạy của Sprint.'
        ),
        createJsonRequest(
          'Bắt đầu Sprint (Start Sprint)',
          'POST',
          'api/sprint/{{sprintId}}/start',
          {
            startDate: '2026-11-20T00:00:00Z',
            endDate: '2026-12-04T00:00:00Z',
            goal: 'Bắt đầu triển khai các User Story của Sprint 1'
          },
          [],
          'Kích hoạt Sprint từ trạng thái Future sang Active.'
        ),
        createJsonRequest(
          'Gán Thẻ vào Sprint hoặc đưa về Backlog (Move Card To Sprint)',
          'POST',
          'api/sprint/move-card',
          {
            cardId: '{{cardId}}',
            sprintId: '{{sprintId}}'
          },
          [],
          'Chuyển một Thẻ công việc vào Sprint (truyền sprintId) hoặc đưa về Backlog (truyền sprintId = null).'
        ),
        createJsonRequest(
          'Danh sách Sprint của Dự án (Get Project Sprints)',
          'GET',
          'api/sprint/project/{{projectId}}',
          null,
          [
            { key: 'boardId', value: '{{boardId}}', description: 'Lọc theo Board Scrum', disabled: true },
            { key: 'includeClosed', value: 'false', description: 'Bao gồm các Sprint đã hoàn thành' }
          ],
          'Lấy danh sách các Sprint trong dự án.'
        ),
        createJsonRequest(
          'Dữ liệu màn hình Backlog (Get Project Backlog)',
          'GET',
          'api/sprint/project/{{projectId}}/backlog',
          null,
          [
            { key: 'boardId', value: '{{boardId}}', description: 'Lọc theo Board Scrum', disabled: true }
          ],
          'Lấy toàn bộ danh sách thẻ chưa gán Sprint (Product Backlog) và các Sprint chưa hoàn thành phục vụ giao diện lập kế hoạch Sprint.'
        ),
        createJsonRequest(
          'Hoàn thành Sprint (Complete Sprint)',
          'POST',
          'api/sprint/{{sprintId}}/complete',
          {
            moveUnfinishedToSprintId: null
          },
          [],
          'Đóng Sprint (chuyển sang Closed). Các công việc chưa hoàn thành có thể chuyển sang Sprint kế tiếp hoặc đưa về Backlog.'
        ),
        createJsonRequest(
          'Xóa Sprint (Delete Sprint)',
          'DELETE',
          'api/sprint/{{sprintId}}',
          null,
          [],
          'Xóa Sprint; các thẻ bên trong Sprint sẽ được tự động chuyển về Backlog.'
        )
      ]
    },

    // 9. Tasks & Cards
    {
      name: '09. Tasks & Cards (Thẻ công việc & Liên kết)',
      item: [
        createJsonRequest(
          'Tạo Thẻ công việc mới trong Cột (Create Card)',
          'POST',
          'api/cards',
          {
            columnId: '{{columnId}}',
            title: 'Tích hợp kéo thả thẻ Kanban bằng Angular CDK',
            description: 'Hiện thực hóa tính năng di chuyển thẻ giữa các cột và sắp xếp thứ tự mượt mà',
            type: 'Story',
            priority: 'High',
            assigneeId: '{{userId}}',
            startDate: '2026-11-20T00:00:00Z',
            dueDate: '2026-11-25T00:00:00Z',
            storyPoints: 5,
            sprintId: null
          },
          [],
          'Tạo mới một thẻ công việc trực tiếp vào một Cột trên Bảng Kanban. Tự động lưu cardId vừa tạo.',
          false,
          createCardScript
        ),
        createJsonRequest(
          'Tạo Thẻ mới vào Backlog (Create Backlog Card)',
          'POST',
          'api/cards/backlog',
          {
            projectId: '{{projectId}}',
            boardId: '{{boardId}}',
            title: 'Xây dựng Module AI RAG với pgvector',
            description: 'Chunking tài liệu đồ án và lưu trữ vector embeddings trong PostgreSQL',
            type: 'Task',
            priority: 'Urgent',
            storyPoints: 8,
            sprintId: null
          },
          [],
          'Tạo thẻ công việc trong Product Backlog (chưa gán vào cột hoặc gán vào cột đầu tiên của Board Scrum).'
        ),
        createJsonRequest(
          'Chi tiết Thẻ công việc (Get Card Detail)',
          'GET',
          'api/cards/{{cardId}}',
          null,
          [],
          'Lấy đầy đủ thông tin chi tiết thẻ: người giao, người nhận, độ ưu tiên, bình luận, ngày bắt đầu và ngày hết hạn.'
        ),
        createFormDataRequest(
          'Cập nhật thông tin Thẻ (Update Card)',
          'PUT',
          'api/cards/{{cardId}}',
          [
            { key: 'Title', value: 'Tích hợp kéo thả thẻ Kanban (Đã cập nhật)', description: 'Tiêu đề' },
            { key: 'Description', value: 'Mô tả chi tiết công việc', description: 'Mô tả' },
            { key: 'CoverImage', type: 'file', description: 'Ảnh bìa thẻ (Upload file)' },
            { key: 'CoverUrl', value: '', description: 'URL ảnh bìa (nếu không upload file)' },
            { key: 'Type', value: 'Story', description: 'Loại thẻ: Task, Bug, Story, Epic' },
            { key: 'Priority', value: 'High', description: 'Mức ưu tiên: Low, Medium, High, Urgent' },
            { key: 'AssigneeId', value: '{{userId}}', description: 'Id người được giao việc' },
            { key: 'StartDate', value: '2026-11-20T00:00:00Z', description: 'Ngày bắt đầu' },
            { key: 'DueDate', value: '2026-11-25T00:00:00Z', description: 'Hạn hoàn thành' },
            { key: 'StoryPoints', value: '5', description: 'Điểm độ phức tạp (Scrum)' },
            { key: 'IsCompleted', value: 'false', description: 'Trạng thái hoàn thành' },
            { key: 'SprintId', value: '', description: 'Id của Sprint' }
          ],
          'Cập nhật chi tiết thẻ công việc (Hỗ trợ upload ảnh bìa cover qua form-data).'
        ),
        createJsonRequest(
          'Di chuyển thẻ khi kéo thả (Move Card)',
          'POST',
          'api/cards/{{cardId}}/move',
          {
            targetColumnId: '{{columnId}}',
            newPosition: 0
          },
          [],
          'Đổi vị trí thẻ trong cùng một cột hoặc chuyển thẻ sang cột khác với thứ tự Position mới.'
        ),
        createJsonRequest(
          'Thêm bình luận vào Thẻ (Add Comment)',
          'POST',
          'api/cards/{{cardId}}/comments',
          {
            contentText: 'Đã hoàn thành merge pull request và test case thành công trên staging.'
          },
          [],
          'Viết bình luận thảo luận bên dưới thẻ công việc.'
        ),
        createJsonRequest(
          'Lịch sử hoạt động của Thẻ (Get Card Activities)',
          'GET',
          'api/cards/{{cardId}}/activities',
          null,
          [
            { key: 'pageIndex', value: '1', description: 'Trang' },
            { key: 'pageSize', value: '20', description: 'Số lượng' }
          ],
          'Lấy luồng hoạt động (Activity Stream / Audit Log) chi tiết của Thẻ.'
        ),
        createJsonRequest(
          'Danh sách thẻ trong Backlog (Get Backlog Cards)',
          'GET',
          'api/cards/project/{{projectId}}/backlog',
          null,
          [
            { key: 'boardId', value: '{{boardId}}', description: 'Lọc theo board', disabled: true }
          ],
          'Lấy danh sách các thẻ chưa được gán vào Sprint nào của Dự án.'
        ),
        createJsonRequest(
          'Thêm liên kết phụ thuộc giữa các Thẻ (Add Dependency)',
          'POST',
          'api/cards/{{cardId}}/dependencies',
          {
            targetCardId: '01a19546-e44d-7f06-9ade-0a0fdbdaec99',
            direction: 'BlockedBy'
          },
          [],
          'Thiết lập mối quan hệ phụ thuộc công việc: BlockedBy (bị chặn bởi thẻ khác) hoặc Blocking (đang chặn thẻ khác).'
        ),
        createJsonRequest(
          'Lấy danh sách quan hệ phụ thuộc của Thẻ (Get Dependencies)',
          'GET',
          'api/cards/{{cardId}}/dependencies',
          null,
          [],
          'Xem danh sách các thẻ đang chặn hoặc bị chặn bởi thẻ hiện tại.'
        ),
        createJsonRequest(
          'Tìm kiếm thẻ ứng viên để liên kết phụ thuộc (Search Candidates)',
          'GET',
          'api/cards/{{cardId}}/dependency-candidates',
          null,
          [
            { key: 'query', value: 'Tích hợp', description: 'Từ khóa tìm kiếm tiêu đề thẻ' }
          ],
          'Tìm kiếm các thẻ cùng dự án để liên kết phụ thuộc (loại trừ chính thẻ này và các thẻ đã tạo chu trình vòng lặp).'
        ),
        createJsonRequest(
          'Xóa liên kết phụ thuộc (Remove Dependency)',
          'DELETE',
          'api/cards/dependencies/{{dependencyId}}',
          null,
          [],
          'Hủy bỏ quan hệ phụ thuộc giữa 2 thẻ.'
        ),
        createJsonRequest(
          'Xóa Thẻ công việc (Delete Card)',
          'DELETE',
          'api/cards/{{cardId}}',
          null,
          [],
          'Xóa mềm thẻ công việc (Đặt ở cuối phân hệ Thẻ công việc).'
        )
      ]
    },

    // 10. Notifications
    {
      name: '10. Notifications (Thông báo)',
      item: [
        createJsonRequest(
          'Lấy danh sách thông báo (Get Notifications)',
          'GET',
          'api/notifications',
          null,
          [
            { key: 'pageIndex', value: '1', description: 'Trang' },
            { key: 'pageSize', value: '20', description: 'Số lượng' },
            { key: 'unreadOnly', value: 'false', description: 'Chỉ lấy thông báo chưa đọc' }
          ],
          'Lấy danh sách thông báo của người dùng hiện tại kèm số lượng thông báo chưa đọc.'
        ),
        createJsonRequest(
          'Đánh dấu một thông báo đã đọc (Mark As Read)',
          'PATCH',
          'api/notifications/{{notificationId}}/read',
          null,
          [],
          'Cập nhật trạng thái thông báo thành đã đọc (IsRead = true).'
        ),
        createJsonRequest(
          'Đánh dấu tất cả thông báo là đã đọc (Mark All Read)',
          'PATCH',
          'api/notifications/read-all',
          null,
          [],
          'Đánh dấu toàn bộ thông báo chưa đọc của người dùng hiện tại là đã đọc.'
        )
      ]
    },

    // 11. Realtime Hubs (SignalR WebSocket)
    {
      name: '11. Real-Time Hubs (SignalR WebSocket)',
      item: [
        {
          name: 'SignalR Board Hub (Realtime Kanban Drag & Drop)',
          request: {
            method: 'GET',
            header: [],
            url: {
              raw: '{{baseUrl}}/hubs/board?access_token={{accessToken}}',
              host: ['{{baseUrl}}'],
              path: ['hubs', 'board'],
              query: [
                {
                  key: 'access_token',
                  value: '{{accessToken}}',
                  description: 'Truyền JWT Token qua query param cho SignalR WebSocket kết nối'
                }
              ]
            },
            description: 'Kênh SignalR phục vụ đồng bộ tức thì thao tác kéo thả thẻ, cập nhật cột, thêm thẻ mới giữa các thành viên đang cùng mở bảng.'
          },
          response: []
        },
        {
          name: 'SignalR Notification Hub (Realtime Instant Push)',
          request: {
            method: 'GET',
            header: [],
            url: {
              raw: '{{baseUrl}}/hubs/notifications?access_token={{accessToken}}',
              host: ['{{baseUrl}}'],
              path: ['hubs', 'notifications'],
              query: [
                {
                  key: 'access_token',
                  value: '{{accessToken}}',
                  description: 'Truyền JWT Token qua query param'
                }
              ]
            },
            description: 'Kênh SignalR đẩy thông báo tức thì (Toast notification) cho người dùng khi được mời vào workspace, gán thẻ hoặc được nhắc tên.'
          },
          response: []
        }
      ]
    }
  ]
};

// Target directory: Source/DATN_BE
const targetDir = path.resolve(__dirname, '..');
const collectionPath = path.join(targetDir, 'DATN_TaskManagement_API.postman_collection.json');
const environmentPath = path.join(targetDir, 'DATN_Environment.postman_environment.json');
const localEnvironmentPath = path.join(targetDir, 'DATN_BE_Local_Environment.postman_environment.json');

fs.writeFileSync(collectionPath, JSON.stringify(collection, null, 2), 'utf-8');
console.log(`Successfully generated Collection at: ${collectionPath}`);

fs.writeFileSync(environmentPath, JSON.stringify(buildEnvironment('DATN - Development Environment'), null, 2), 'utf-8');
console.log(`Successfully generated Environment at: ${environmentPath}`);

fs.writeFileSync(localEnvironmentPath, JSON.stringify(buildEnvironment('DATN_BE - Local Environment'), null, 2), 'utf-8');
console.log(`Successfully generated Local Environment at: ${localEnvironmentPath}`);
