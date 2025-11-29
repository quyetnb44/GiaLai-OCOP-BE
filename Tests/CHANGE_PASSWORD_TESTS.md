# Hướng dẫn Test Chức năng Đổi Mật khẩu

## Tổng quan

Các test này kiểm tra chức năng đổi mật khẩu (`PUT /api/auth/change-password`) đảm bảo:
- ✅ Chỉ cần token hợp lệ để thực hiện đổi mật khẩu
- ✅ Backend cập nhật `PasswordUpdatedAt` sau khi đổi mật khẩu
- ✅ Backend tạo và trả về token mới
- ✅ Token mới khác token cũ
- ✅ Mật khẩu mới có thể dùng để login

## Các Test Cases

### 1. `ChangePassword_WithValidTokenAndPassword_ShouldReturnNewToken`
**Mục đích:** Test đổi mật khẩu thành công với token hợp lệ

**Kiểm tra:**
- ✅ Status code = 200 OK
- ✅ Response có token mới
- ✅ Token mới khác token cũ
- ✅ `PasswordUpdatedAt` được cập nhật
- ✅ Mật khẩu mới có thể dùng để login

**Cách chạy:**
```bash
dotnet test --filter "FullyQualifiedName~ChangePassword_WithValidTokenAndPassword_ShouldReturnNewToken"
```

### 2. `ChangePassword_WithInvalidCurrentPassword_ShouldReturnBadRequest`
**Mục đích:** Test khi mật khẩu hiện tại sai

**Kiểm tra:**
- ✅ Status code = 400 Bad Request
- ✅ Error message chứa "Mật khẩu hiện tại không đúng"

**Cách chạy:**
```bash
dotnet test --filter "FullyQualifiedName~ChangePassword_WithInvalidCurrentPassword_ShouldReturnBadRequest"
```

### 3. `ChangePassword_WithInvalidNewPasswordFormat_ShouldReturnBadRequest`
**Mục đích:** Test khi mật khẩu mới không đáp ứng yêu cầu format (thiếu chữ hoa, chữ thường hoặc số)

**Kiểm tra:**
- ✅ Status code = 400 Bad Request
- ✅ Error message chứa yêu cầu về chữ hoa, chữ thường hoặc số

**Cách chạy:**
```bash
dotnet test --filter "FullyQualifiedName~ChangePassword_WithInvalidNewPasswordFormat_ShouldReturnBadRequest"
```

### 4. `ChangePassword_WithSamePassword_ShouldReturnBadRequest`
**Mục đích:** Test khi mật khẩu mới trùng với mật khẩu hiện tại

**Kiểm tra:**
- ✅ Status code = 400 Bad Request
- ✅ Error message chứa "Mật khẩu mới phải khác mật khẩu hiện tại"

**Cách chạy:**
```bash
dotnet test --filter "FullyQualifiedName~ChangePassword_WithSamePassword_ShouldReturnBadRequest"
```

### 5. `ChangePassword_WithoutToken_ShouldReturnUnauthorized`
**Mục đích:** Test khi không có token

**Kiểm tra:**
- ✅ Status code = 401 Unauthorized

**Cách chạy:**
```bash
dotnet test --filter "FullyQualifiedName~ChangePassword_WithoutToken_ShouldReturnUnauthorized"
```

### 6. `ChangePassword_WithInvalidToken_ShouldReturnUnauthorized`
**Mục đích:** Test khi token không hợp lệ

**Kiểm tra:**
- ✅ Status code = 401 Unauthorized

**Cách chạy:**
```bash
dotnet test --filter "FullyQualifiedName~ChangePassword_WithInvalidToken_ShouldReturnUnauthorized"
```

## Chạy tất cả các test

```bash
# Chạy tất cả test trong project
dotnet test

# Chạy tất cả test đổi mật khẩu
dotnet test --filter "FullyQualifiedName~ChangePassword"

# Chạy tất cả integration test
dotnet test --filter "FullyQualifiedName~AuthControllerIntegrationTests"
```

## Cấu trúc Test

### Helper Method
Đã thêm `CreateJwtToken` vào `TestHelpers.cs` để tạo JWT token hợp lệ cho testing:
```csharp
TestHelpers.CreateJwtToken(userId, email, name, role, jwtKey, issuer, audience, lifetimeMinutes)
```

### Test Setup
- Sử dụng in-memory database
- Tạo user test với mật khẩu đã hash
- Tạo JWT token hợp lệ từ config
- Set Authorization header với Bearer token

## Lưu ý

1. **Migration cần được chạy trước:** Đảm bảo đã chạy migration `AddPasswordUpdatedAtToUser` để thêm trường `PasswordUpdatedAt` vào database
   ```bash
   dotnet ef database update
   ```

2. **JWT Config:** Test sử dụng JWT config từ `appsettings.json` hoặc `appsettings.Development.json`

3. **Database:** Test sử dụng in-memory database, không ảnh hưởng đến database thật

4. **Vấn đề đã biết:** Một số test có thể fail với lỗi 401 Unauthorized do in-memory database không share data giữa các HTTP requests trong test environment. Đây là vấn đề phổ biến với EF Core in-memory database và integration tests. 

   **Giải pháp tạm thời:**
   - Test chức năng đổi mật khẩu bằng cách gọi API trực tiếp (Postman, Swagger, etc.)
   - Hoặc sử dụng test database thật thay vì in-memory database
   - Hoặc đảm bảo DbContext được register với Scoped lifetime và database name được share giữa các requests

## Kết quả mong đợi

Tất cả 6 test cases phải pass để đảm bảo chức năng đổi mật khẩu hoạt động đúng theo yêu cầu:
- ✅ Chỉ cần token hợp lệ để đổi mật khẩu
- ✅ Cập nhật PasswordUpdatedAt
- ✅ Tạo và trả về token mới
- ✅ Xử lý đúng các trường hợp lỗi

