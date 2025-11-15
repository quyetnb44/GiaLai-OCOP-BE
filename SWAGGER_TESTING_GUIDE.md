# 🧪 Hướng Dẫn Test API OTP Trên Swagger

Hướng dẫn chi tiết cách test các API xác thực email bằng OTP trên Swagger UI.

---

## 📋 Bước 1: Khởi Động Ứng Dụng

1. Mở terminal trong thư mục dự án
2. Chạy lệnh:
   ```bash
   dotnet run
   ```
3. Mở trình duyệt và truy cập: `https://localhost:5001/swagger` hoặc `http://localhost:5000/swagger`
   (Port có thể khác, xem trong terminal)

---

## 🔐 Test API OTP - Đăng Ký Với OTP

### Bước 1: Gửi Mã OTP

1. Tìm endpoint `POST /api/auth/send-otp` trong Swagger
2. Click vào để mở rộng
3. Click nút **"Try it out"**
4. Nhập thông tin:
   ```json
   {
     "email": "test@example.com",
     "purpose": "Register"
   }
   ```
5. Click **"Execute"**

**Response mong đợi:**
```json
{
  "message": "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư."
}
```

**Lưu ý:** 
- Nếu chưa cấu hình email hoặc trong Development mode, response sẽ có thêm `otpCode`:
```json
{
  "message": "Mã OTP đã được tạo. (Development mode - OTP: 123456)",
  "otpCode": "123456"
}
```
- **Copy mã OTP này** để dùng ở bước tiếp theo!

---

### Bước 2: Đăng Ký Với OTP

1. Tìm endpoint `POST /api/auth/register-with-otp`
2. Click **"Try it out"**
3. Nhập thông tin (dùng OTP từ bước 1):
   ```json
   {
     "name": "Nguyễn Văn A",
     "email": "test@example.com",
     "password": "password123",
     "otpCode": "123456"  // ← Dán mã OTP từ bước 1
   }
   ```
4. Click **"Execute"**

**Response thành công:**
```json
{
  "id": 1,
  "name": "Nguyễn Văn A",
  "email": "test@example.com",
  "role": "Customer",
  "isEmailVerified": true,
  "message": "Đăng ký thành công. Email đã được xác thực."
}
```

---

## 🔑 Test API OTP - Đăng Nhập Bằng OTP

### Bước 1: Gửi Mã OTP (Login)

1. Tìm endpoint `POST /api/auth/send-otp`
2. Click **"Try it out"**
3. Nhập thông tin:
   ```json
   {
     "email": "test@example.com",  // Email đã đăng ký
     "purpose": "Login"
   }
   ```
4. Click **"Execute"**
5. **Copy mã OTP** từ response

---

### Bước 2: Đăng Nhập Với OTP

1. Tìm endpoint `POST /api/auth/login-with-otp`
2. Click **"Try it out"**
3. Nhập thông tin:
   ```json
   {
     "email": "test@example.com",
     "otpCode": "123456"  // ← Dán mã OTP từ bước 1
   }
   ```
4. Click **"Execute"**

**Response thành công:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": "2024-11-14T10:30:00Z",
  "message": "Đăng nhập thành công bằng OTP."
}
```

**Lưu ý:** Copy `token` này để dùng cho các API yêu cầu authentication!

---

## 🔍 Test API Xác Thực OTP (Standalone)

Nếu bạn chỉ muốn test xác thực OTP mà không đăng ký/đăng nhập:

1. Gửi OTP trước (như bước 1 ở trên)
2. Tìm endpoint `POST /api/auth/verify-otp`
3. Click **"Try it out"**
4. Nhập:
   ```json
   {
     "email": "test@example.com",
     "otpCode": "123456",
     "purpose": "Register"
   }
   ```
5. Click **"Execute"**

**Response thành công:**
```json
{
  "message": "Xác thực OTP thành công.",
  "verified": true
}
```

---

## ⚠️ Xử Lý Lỗi Thường Gặp

### 1. Lỗi Rate Limiting
**Error:**
```json
{
  "message": "Vui lòng đợi 1 phút trước khi yêu cầu mã OTP mới."
}
```
**Giải pháp:** Đợi 1 phút hoặc dùng email khác

---

### 2. OTP Hết Hạn
**Error:**
```json
{
  "message": "Mã OTP không hợp lệ hoặc đã hết hạn."
}
```
**Giải pháp:** Gửi lại OTP mới (OTP có hiệu lực 10 phút)

---

### 3. OTP Đã Sử Dụng
**Error:**
```json
{
  "message": "Mã OTP không hợp lệ hoặc đã hết hạn."
}
```
**Giải pháp:** Mỗi OTP chỉ dùng 1 lần, cần gửi OTP mới

---

### 4. Email Đã Tồn Tại
**Error:**
```json
{
  "message": "Email đã được sử dụng."
}
```
**Giải pháp:** Dùng email khác hoặc đăng nhập với email đó

---

## 🎯 Test Flow Hoàn Chỉnh

### Scenario 1: Đăng Ký Mới
```
1. POST /api/auth/send-otp (purpose: "Register")
   → Copy OTP từ response

2. POST /api/auth/register-with-otp
   → Nhập thông tin + OTP
   → Nhận user mới với isEmailVerified = true
```

### Scenario 2: Đăng Nhập Bằng OTP
```
1. POST /api/auth/send-otp (purpose: "Login")
   → Copy OTP từ response

2. POST /api/auth/login-with-otp
   → Nhập email + OTP
   → Nhận JWT token
```

### Scenario 3: Đăng Nhập Thông Thường (Không OTP)
```
POST /api/auth/login
{
  "email": "test@example.com",
  "password": "password123"
}
→ Nhận JWT token
```

---

## 🔐 Sử Dụng JWT Token Trong Swagger

Sau khi đăng nhập thành công (bằng OTP hoặc password), bạn có thể dùng token để test các API yêu cầu authentication:

1. Click nút **"Authorize"** ở đầu trang Swagger (🔒 icon)
2. Nhập: `Bearer <your-token>`
   Ví dụ: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`
3. Click **"Authorize"**
4. Click **"Close"**

Bây giờ tất cả các API yêu cầu authentication sẽ tự động dùng token này!

---

## 📸 Screenshots Flow

### 1. Gửi OTP
```
Swagger UI → POST /api/auth/send-otp
→ Try it out
→ Nhập email và purpose
→ Execute
→ Copy OTP từ response
```

### 2. Đăng Ký/Đăng Nhập
```
Swagger UI → POST /api/auth/register-with-otp (hoặc login-with-otp)
→ Try it out
→ Nhập thông tin + OTP đã copy
→ Execute
→ Nhận kết quả
```

---

## 💡 Tips

1. **Development Mode**: Nếu chưa cấu hình email, OTP sẽ hiển thị trong response để test
2. **Rate Limiting**: Chỉ 1 OTP/phút, nên test với nhiều email khác nhau
3. **OTP Expiry**: OTP có hiệu lực 10 phút, nên test nhanh
4. **Copy Token**: Sau khi đăng nhập, copy token để dùng cho các API khác
5. **Clear OTP**: Nếu test nhiều lần, có thể cần xóa OTP cũ trong database

---

## 🧹 Cleanup (Tùy chọn)

Nếu muốn xóa OTP cũ trong database để test lại:

```sql
DELETE FROM "EmailVerifications" 
WHERE "ExpiresAt" < NOW() OR "IsUsed" = true;
```

---

**Chúc bạn test thành công! 🎉**

