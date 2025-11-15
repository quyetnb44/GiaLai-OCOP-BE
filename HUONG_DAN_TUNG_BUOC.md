# 📝 Hướng Dẫn Từng Bước - Cấu Hình Gmail & Test OTP

Hướng dẫn chi tiết từng bước để cấu hình Gmail và test chức năng OTP.

---

## 🎯 Mục Tiêu

Sau khi hoàn thành, hệ thống sẽ:
- ✅ Gửi OTP về email thật của user
- ✅ Tất cả user phải xác thực email trước khi đăng nhập
- ✅ OTP được gửi tự động khi đăng ký/đăng nhập

---

## 📋 BƯỚC 1: Tạo Gmail App Password

### 1.1. Truy cập Google Account
1. Mở trình duyệt và vào: https://myaccount.google.com/
2. Đăng nhập bằng tài khoản Gmail của bạn

### 1.2. Bật 2-Step Verification
1. Click vào **Security** (Bảo mật) ở menu bên trái
2. Tìm mục **2-Step Verification** (Xác minh 2 bước)
3. Click vào **2-Step Verification**
4. Click nút **Get started** hoặc **Turn on**
5. Làm theo hướng dẫn:
   - Chọn phương thức xác minh (SMS hoặc Google Authenticator)
   - Nhập số điện thoại (nếu chọn SMS)
   - Nhập mã xác minh được gửi đến
6. Click **Turn On** để hoàn tất

**⚠️ LƯU Ý:** Bạn PHẢI bật 2-Step Verification trước khi tạo App Password!

---

### 1.3. Tạo App Password
1. Vẫn trong trang **Security**
2. Tìm lại mục **2-Step Verification** → Click vào
3. Cuộn xuống tìm **App passwords** (Mật khẩu ứng dụng)
4. Click **App passwords**

   **Nếu không thấy "App passwords":**
   - Đảm bảo đã bật 2-Step Verification
   - Có thể cần xác minh danh tính lại
   - Hoặc truy cập trực tiếp: https://myaccount.google.com/apppasswords

5. Chọn app: **Mail**
6. Chọn device: **Other (Custom name)**
7. Nhập tên: `GiaLai OCOP API` (hoặc tên bạn muốn)
8. Click **Generate**

9. **QUAN TRỌNG:** Copy mã 16 ký tự ngay lập tức!
   - Mã sẽ hiển thị dạng: `abcd efgh ijkl mnop`
   - Copy toàn bộ (có thể có hoặc không có dấu cách)
   - Mã này chỉ hiển thị 1 lần, không xem lại được!

**Ví dụ mã App Password:** `abcd efgh ijkl mnop` hoặc `abcdefghijklmnop`

---

## 📋 BƯỚC 2: Cấu Hình Trong appsettings.json

### 2.1. Mở file appsettings.json
1. Mở dự án trong Visual Studio hoặc editor
2. Tìm file `appsettings.json` trong thư mục gốc

### 2.2. Cập nhật thông tin Email
Tìm section `Email` và cập nhật như sau:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@gmail.com",        // ← Thay bằng email Gmail của bạn
    "SmtpPassword": "abcd efgh ijkl mnop",        // ← Thay bằng App Password đã copy
    "FromEmail": "your-email@gmail.com",           // ← Cùng email trên
    "FromName": "GiaLai OCOP"
  }
}
```

**Ví dụ thực tế:**
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "gialaiocop@gmail.com",
    "SmtpPassword": "abcd efgh ijkl mnop",
    "FromEmail": "gialaiocop@gmail.com",
    "FromName": "GiaLai OCOP"
  }
}
```

**⚠️ LƯU Ý:**
- `SmtpUsername`: Email Gmail của bạn (email thật)
- `SmtpPassword`: App Password 16 ký tự (KHÔNG phải mật khẩu Gmail thông thường!)
- `FromEmail`: Thường giống với `SmtpUsername`

---

## 📋 BƯỚC 3: Restart Ứng Dụng

### 3.1. Dừng ứng dụng (nếu đang chạy)
1. Trong terminal/console, nhấn `Ctrl + C` để dừng
2. Hoặc đóng cửa sổ terminal

### 3.2. Chạy lại ứng dụng
```bash
dotnet run
```

**Hoặc trong Visual Studio:**
- Click nút **Stop** (nếu đang chạy)
- Click nút **Run** hoặc nhấn `F5`

### 3.3. Kiểm tra log
Xem log trong console, nếu thấy:
- ✅ Không có lỗi về email configuration
- ✅ Ứng dụng chạy thành công

---

## 📋 BƯỚC 4: Test Gửi OTP

### 4.1. Mở Swagger UI
1. Mở trình duyệt
2. Truy cập: `https://localhost:5001/swagger` (hoặc port hiển thị trong terminal)
3. Nếu có cảnh báo SSL, click **Advanced** → **Proceed**

### 4.2. Test gửi OTP
1. Tìm endpoint `POST /api/auth/send-otp`
2. Click để mở rộng
3. Click nút **"Try it out"**
4. Nhập thông tin:
   ```json
   {
     "email": "your-test-email@gmail.com",  // ← Email thật của bạn để test
     "purpose": "Register"
   }
   ```
5. Click **"Execute"**

### 4.3. Kiểm tra kết quả

**Nếu thành công:**
```json
{
  "message": "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư."
}
```

**Nếu có lỗi:**
- Kiểm tra lại cấu hình trong `appsettings.json`
- Xem log trong console để biết lỗi cụ thể
- Đảm bảo App Password đúng (16 ký tự)

### 4.4. Kiểm tra email
1. Mở hộp thư Gmail của email bạn vừa nhập
2. Tìm email từ "GiaLai OCOP"
3. Subject: "Xác thực email đăng ký - GiaLai OCOP"
4. Mã OTP sẽ nằm trong email (6 chữ số)

**Lưu ý:**
- Email có thể vào thư mục **Spam/Junk**, hãy kiểm tra
- Đợi vài giây nếu chưa thấy email

---

## 📋 BƯỚC 5: Test Đăng Ký Với OTP

### 5.1. Gửi OTP (nếu chưa gửi)
Làm lại Bước 4.2 để gửi OTP

### 5.2. Copy mã OTP
- Mở email và copy mã OTP 6 chữ số
- Ví dụ: `123456`

### 5.3. Đăng ký với OTP
1. Tìm endpoint `POST /api/auth/register-with-otp`
2. Click **"Try it out"**
3. Nhập thông tin:
   ```json
   {
     "name": "Nguyễn Văn A",
     "email": "your-test-email@gmail.com",  // ← Cùng email ở bước 4.2
     "password": "Password123",
     "otpCode": "123456"                    // ← Dán mã OTP từ email
   }
   ```
4. Click **"Execute"**

### 5.4. Kiểm tra kết quả

**Nếu thành công:**
```json
{
  "id": 1,
  "name": "Nguyễn Văn A",
  "email": "your-test-email@gmail.com",
  "role": "Customer",
  "isEmailVerified": true,
  "message": "Đăng ký thành công. Email đã được xác thực."
}
```

**Nếu OTP sai hoặc hết hạn:**
```json
{
  "message": "Mã OTP không hợp lệ hoặc đã hết hạn."
}
```
→ Gửi lại OTP mới và thử lại

---

## 📋 BƯỚC 6: Test Đăng Nhập

### 6.1. Đăng nhập thông thường
1. Tìm endpoint `POST /api/auth/login`
2. Click **"Try it out"**
3. Nhập thông tin:
   ```json
   {
     "email": "your-test-email@gmail.com",
     "password": "Password123"
   }
   ```
4. Click **"Execute"**

### 6.2. Kiểm tra kết quả

**Nếu thành công (email đã verify):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": "2024-11-14T10:30:00Z"
}
```

**Nếu email chưa verify:**
```json
{
  "message": "Email chưa được xác thực. Vui lòng xác thực email trước khi đăng nhập.",
  "isEmailVerified": false,
  "instruction": "Gọi POST /api/auth/resend-verification-otp để nhận mã OTP xác thực email"
}
```

---

## 🔧 Xử Lý Lỗi Thường Gặp

### ❌ Lỗi: "Email configuration is missing"
**Nguyên nhân:** Chưa cấu hình email trong `appsettings.json`

**Giải pháp:**
1. Kiểm tra lại file `appsettings.json`
2. Đảm bảo section `Email` có đầy đủ các trường
3. Restart ứng dụng

---

### ❌ Lỗi: "Email configuration is using placeholder values"
**Nguyên nhân:** Đang dùng giá trị placeholder (`your-email@gmail.com`, `your-app-password`)

**Giải pháp:**
1. Thay `your-email@gmail.com` bằng email Gmail thật
2. Thay `your-app-password` bằng App Password thật (16 ký tự)

---

### ❌ Lỗi: "Authentication failed"
**Nguyên nhân:** 
- App Password sai
- Chưa bật 2-Step Verification
- Email không đúng

**Giải pháp:**
1. Kiểm tra lại App Password (copy đúng 16 ký tự)
2. Đảm bảo đã bật 2-Step Verification
3. Tạo lại App Password nếu cần

---

### ❌ Email không đến
**Kiểm tra:**
1. Xem log trong console - có thông báo "OTP email sent successfully" không?
2. Kiểm tra thư mục **Spam/Junk** trong Gmail
3. Đợi vài phút (email có thể bị delay)
4. Kiểm tra lại địa chỉ email nhận có đúng không
5. Thử gửi lại OTP

---

### ❌ OTP hết hạn
**Nguyên nhân:** OTP có hiệu lực 10 phút

**Giải pháp:**
1. Gửi lại OTP mới
2. Sử dụng OTP mới ngay sau khi nhận

---

## ✅ Checklist Hoàn Thành

Sau khi làm xong, đảm bảo:

- [ ] Đã bật 2-Step Verification trên Google Account
- [ ] Đã tạo App Password (16 ký tự)
- [ ] Đã cập nhật `appsettings.json` với email và App Password thật
- [ ] Đã restart ứng dụng sau khi cấu hình
- [ ] Đã test gửi OTP và nhận được email
- [ ] Đã test đăng ký với OTP thành công
- [ ] Đã test đăng nhập thành công

---

## 🎉 Hoàn Thành!

Nếu tất cả các bước trên đều thành công, bạn đã cấu hình xong!

**Bây giờ:**
- ✅ Tất cả user mới phải xác thực email bằng OTP khi đăng ký
- ✅ OTP được gửi về email thật của user
- ✅ User chưa verify email không thể đăng nhập
- ✅ Hệ thống an toàn và đáng tin cậy hơn

---

## 📞 Cần Hỗ Trợ?

Nếu gặp vấn đề:
1. Kiểm tra lại từng bước
2. Xem log trong console để biết lỗi cụ thể
3. Đảm bảo App Password đúng và đã bật 2-Step Verification

**Chúc bạn thành công! 🎉**

