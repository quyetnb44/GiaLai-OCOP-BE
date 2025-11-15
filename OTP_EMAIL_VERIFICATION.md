# 📧 Xác Thực Email Bằng Mã OTP

Tài liệu này mô tả chức năng xác thực email bằng mã OTP đã được tích hợp vào hệ thống.

---

## ✨ Tính Năng

Hệ thống hỗ trợ xác thực email bằng mã OTP cho các mục đích:
- **Register**: Xác thực email khi đăng ký tài khoản
- **Login**: Đăng nhập bằng OTP (không cần mật khẩu)
- **ResetPassword**: Đặt lại mật khẩu (tính năng tương lai)

---

## 🔧 Cấu Hình Email

### 1. Cấu hình trong `appsettings.json`

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "GiaLai OCOP"
  }
}
```

### 2. Cấu hình Gmail App Password

Để sử dụng Gmail, bạn cần tạo **App Password** (không dùng mật khẩu thông thường):

1. Vào [Google Account Settings](https://myaccount.google.com/)
2. Bật **2-Step Verification**
3. Tạo **App Password**:
   - Vào **Security** → **2-Step Verification** → **App passwords**
   - Chọn **Mail** và **Other (Custom name)**
   - Nhập tên: "GiaLai OCOP API"
   - Copy mã 16 ký tự và dán vào `SmtpPassword`

**Lưu ý:** Nếu không cấu hình email, hệ thống vẫn hoạt động nhưng không gửi được email. Trong môi trường Development, OTP sẽ được trả về trong response để test.

---

## 📋 API Endpoints

### 1. Gửi Mã OTP

**Endpoint:** `POST /api/auth/send-otp`

**Request:**
```json
{
  "email": "user@example.com",
  "purpose": "Register"  // Register, Login, ResetPassword
}
```

**Response:**
```json
{
  "message": "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư."
}
```

**Lưu ý:**
- Rate limiting: Chỉ cho phép gửi 1 OTP mỗi phút cho mỗi email
- OTP có hiệu lực 10 phút
- Trong Development mode, OTP sẽ được trả về trong response để test

**Development Mode Response:**
```json
{
  "message": "Mã OTP đã được tạo. (Development mode - OTP: 123456)",
  "otpCode": "123456"
}
```

---

### 2. Xác Thực Mã OTP

**Endpoint:** `POST /api/auth/verify-otp`

**Request:**
```json
{
  "email": "user@example.com",
  "otpCode": "123456",
  "purpose": "Register"
}
```

**Response:**
```json
{
  "message": "Xác thực OTP thành công.",
  "verified": true
}
```

**Error Responses:**
- `400 Bad Request`: Mã OTP không hợp lệ hoặc đã hết hạn
- `400 Bad Request`: Validation errors

---

### 3. Đăng Ký Với OTP

**Endpoint:** `POST /api/auth/register-with-otp`

**Request:**
```json
{
  "name": "Nguyễn Văn A",
  "email": "user@example.com",
  "password": "password123",
  "otpCode": "123456"
}
```

**Response:**
```json
{
  "id": 1,
  "name": "Nguyễn Văn A",
  "email": "user@example.com",
  "role": "Customer",
  "isEmailVerified": true,
  "message": "Đăng ký thành công. Email đã được xác thực."
}
```

**Error Responses:**
- `400 Bad Request`: Mã OTP không hợp lệ hoặc đã hết hạn
- `409 Conflict`: Email đã được sử dụng

**Luồng:**
1. Gọi `POST /api/auth/send-otp` với `purpose: "Register"`
2. Nhập mã OTP từ email
3. Gọi `POST /api/auth/register-with-otp` với thông tin đăng ký và mã OTP

---

### 4. Đăng Nhập Bằng OTP

**Endpoint:** `POST /api/auth/login-with-otp`

**Request:**
```json
{
  "email": "user@example.com",
  "otpCode": "123456"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": "2024-11-14T10:30:00Z",
  "message": "Đăng nhập thành công bằng OTP."
}
```

**Error Responses:**
- `400 Bad Request`: Mã OTP không hợp lệ hoặc đã hết hạn
- `401 Unauthorized`: Email không tồn tại trong hệ thống

**Luồng:**
1. Gọi `POST /api/auth/send-otp` với `purpose: "Login"`
2. Nhập mã OTP từ email
3. Gọi `POST /api/auth/login-with-otp` với email và mã OTP
4. Nhận JWT token để sử dụng cho các request tiếp theo

---

## 🔄 Luồng Hoạt Động

### Đăng Ký Với OTP:
```
1. User nhập email → POST /api/auth/send-otp (purpose: "Register")
2. Hệ thống gửi OTP đến email
3. User nhập OTP từ email
4. User gọi POST /api/auth/register-with-otp với thông tin đăng ký + OTP
5. Hệ thống tạo tài khoản với IsEmailVerified = true
```

### Đăng Nhập Bằng OTP:
```
1. User nhập email → POST /api/auth/send-otp (purpose: "Login")
2. Hệ thống gửi OTP đến email
3. User nhập OTP từ email
4. User gọi POST /api/auth/login-with-otp với email + OTP
5. Hệ thống trả về JWT token
```

---

## 🗄️ Database Schema

### EmailVerification Table
```sql
CREATE TABLE "EmailVerifications" (
    "Id" SERIAL PRIMARY KEY,
    "Email" TEXT NOT NULL,
    "OtpCode" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "IsUsed" BOOLEAN NOT NULL DEFAULT FALSE,
    "Purpose" TEXT  -- "Register", "Login", "ResetPassword"
);
```

### User Table (Updated)
- Thêm trường `IsEmailVerified` (BOOLEAN, default: false)

---

## 🔒 Bảo Mật

1. **Rate Limiting**: Chỉ cho phép gửi 1 OTP mỗi phút cho mỗi email
2. **OTP Expiry**: OTP có hiệu lực 10 phút
3. **One-Time Use**: Mỗi OTP chỉ được sử dụng 1 lần
4. **Auto Cleanup**: OTP cũ đã hết hạn hoặc đã sử dụng sẽ tự động bị xóa

---

## 🧪 Testing

### Development Mode

Trong môi trường Development, nếu không cấu hình email hoặc email service fail, OTP sẽ được trả về trong response để test:

```json
{
  "message": "Mã OTP đã được tạo. (Development mode - OTP: 123456)",
  "otpCode": "123456"
}
```

### Test với Postman/Swagger

1. **Gửi OTP:**
   ```
   POST /api/auth/send-otp
   {
     "email": "test@example.com",
     "purpose": "Register"
   }
   ```

2. **Đăng ký với OTP:**
   ```
   POST /api/auth/register-with-otp
   {
     "name": "Test User",
     "email": "test@example.com",
     "password": "password123",
     "otpCode": "123456"  // Lấy từ response bước 1 (Development mode)
   }
   ```

---

## 📝 Lưu Ý

1. **Email Configuration**: Phải cấu hình đúng thông tin SMTP trong `appsettings.json`
2. **Gmail App Password**: Không dùng mật khẩu thông thường, phải dùng App Password
3. **Development Mode**: Trong môi trường development, OTP sẽ hiển thị trong response để test
4. **Production**: Trong production, không nên trả về OTP trong response
5. **Migration**: Cần chạy migration để tạo bảng `EmailVerifications`:
   ```bash
   dotnet ef database update
   ```

---

## 🚀 Migration

Sau khi pull code, chạy migration:

```bash
dotnet ef database update
```

Migration mới:
- `AddEmailVerificationAndIsEmailVerified` - Tạo bảng EmailVerifications và thêm trường IsEmailVerified vào User

---

**Cập nhật lần cuối:** 2024-11-14

