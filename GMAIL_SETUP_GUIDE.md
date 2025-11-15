# 📧 Hướng Dẫn Cấu Hình Gmail Để Gửi Email OTP

Hướng dẫn chi tiết cách cấu hình Gmail để hệ thống có thể gửi email OTP thật.

---

## 🔐 Bước 1: Bật 2-Step Verification

1. Truy cập [Google Account Settings](https://myaccount.google.com/)
2. Vào **Security** (Bảo mật)
3. Tìm mục **2-Step Verification** (Xác minh 2 bước)
4. Click **Get started** hoặc **Turn on**
5. Làm theo hướng dẫn để bật xác minh 2 bước
   - Có thể dùng số điện thoại hoặc Google Authenticator

**Lưu ý:** Bạn **PHẢI** bật 2-Step Verification trước khi tạo App Password!

---

## 🔑 Bước 2: Tạo App Password

1. Vẫn trong trang **Security** của Google Account
2. Tìm mục **2-Step Verification** → Click vào
3. Cuộn xuống tìm **App passwords** (Mật khẩu ứng dụng)
4. Click **App passwords**
5. Nếu chưa thấy, có thể cần:
   - Xác minh danh tính lại
   - Hoặc truy cập trực tiếp: https://myaccount.google.com/apppasswords

6. Chọn app: **Mail**
7. Chọn device: **Other (Custom name)**
8. Nhập tên: `GiaLai OCOP API` (hoặc tên bạn muốn)
9. Click **Generate**

10. **Copy mã 16 ký tự** (không có dấu cách)
    - Ví dụ: `abcd efgh ijkl mnop` → Copy: `abcdefghijklmnop`

**⚠️ QUAN TRỌNG:** 
- Mã này chỉ hiển thị 1 lần, hãy copy ngay!
- Đây là mật khẩu riêng cho ứng dụng, không phải mật khẩu Gmail của bạn

---

## ⚙️ Bước 3: Cấu Hình Trong appsettings.json

1. Mở file `appsettings.json` trong dự án
2. Tìm section `Email`
3. Cập nhật thông tin:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-actual-email@gmail.com",  // ← Email Gmail của bạn
    "SmtpPassword": "abcdefghijklmnop",              // ← App Password 16 ký tự (không có dấu cách)
    "FromEmail": "your-actual-email@gmail.com",     // ← Cùng email trên
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
    "SmtpPassword": "abcd efgh ijkl mnop",  // Hoặc "abcdefghijklmnop" (không dấu cách)
    "FromEmail": "gialaiocop@gmail.com",
    "FromName": "GiaLai OCOP"
  }
}
```

---

## ✅ Bước 4: Kiểm Tra Cấu Hình

1. **Restart ứng dụng:**
   ```bash
   # Dừng ứng dụng (Ctrl+C)
   # Chạy lại
   dotnet run
   ```

2. **Test gửi OTP:**
   - Mở Swagger: `https://localhost:5001/swagger`
   - Gọi `POST /api/auth/send-otp`
   - Nhập email của bạn (email thật, không phải email trong config)
   - Click Execute

3. **Kiểm tra email:**
   - Mở hộp thư Gmail của email bạn vừa nhập
   - Tìm email từ "GiaLai OCOP"
   - Subject: "Xác thực email đăng ký - GiaLai OCOP"
   - Mã OTP sẽ nằm trong email

---

## 🔍 Troubleshooting

### ❌ Lỗi: "Email configuration is missing"
**Nguyên nhân:** Chưa cấu hình email trong `appsettings.json`

**Giải pháp:**
- Kiểm tra lại section `Email` trong `appsettings.json`
- Đảm bảo tất cả các trường đều có giá trị

---

### ❌ Lỗi: "Email configuration is using placeholder values"
**Nguyên nhân:** Đang dùng giá trị placeholder (`your-email@gmail.com`, `your-app-password`)

**Giải pháp:**
- Thay thế bằng email thật và App Password thật

---

### ❌ Lỗi: "Authentication failed"
**Nguyên nhân:** 
- App Password sai
- Chưa bật 2-Step Verification
- Email không đúng

**Giải pháp:**
1. Kiểm tra lại App Password (copy đúng 16 ký tự, không có dấu cách)
2. Đảm bảo đã bật 2-Step Verification
3. Tạo lại App Password nếu cần

---

### ❌ Lỗi: "Connection timeout" hoặc "Unable to connect"
**Nguyên nhân:**
- Firewall chặn port 587
- Mạng không cho phép kết nối SMTP

**Giải pháp:**
1. Kiểm tra firewall
2. Thử dùng port 465 với SSL:
   ```json
   {
     "SmtpPort": "465"
   }
   ```
   Và sửa code trong `EmailService.cs`:
   ```csharp
   await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);
   ```

---

### ✅ Email không đến nhưng không có lỗi
**Kiểm tra:**
1. Xem log trong console/terminal - có thông báo "OTP email sent successfully" không?
2. Kiểm tra thư mục **Spam/Junk** trong Gmail
3. Đợi vài phút (email có thể bị delay)
4. Kiểm tra lại địa chỉ email nhận có đúng không

---

## 📝 Lưu Ý Quan Trọng

1. **App Password vs Mật khẩu thường:**
   - ❌ KHÔNG dùng mật khẩu Gmail thông thường
   - ✅ PHẢI dùng App Password (16 ký tự)

2. **Bảo mật:**
   - Không commit `appsettings.json` có thông tin email thật lên Git
   - Dùng `appsettings.Development.json` cho development
   - Dùng Environment Variables cho production

3. **Rate Limiting:**
   - Gmail có giới hạn số email gửi/ngày
   - Nếu gửi quá nhiều có thể bị tạm khóa

4. **Test Email:**
   - Luôn test với email thật trước khi deploy
   - Kiểm tra cả inbox và spam folder

---

## 🎯 Quick Checklist

- [ ] Đã bật 2-Step Verification trên Google Account
- [ ] Đã tạo App Password (16 ký tự)
- [ ] Đã cập nhật `appsettings.json` với email và App Password thật
- [ ] Đã restart ứng dụng sau khi cấu hình
- [ ] Đã test gửi OTP và nhận được email

---

## 🔄 Cấu Hình Cho Production

Trong môi trường production, nên dùng **Environment Variables** thay vì hardcode trong `appsettings.json`:

```bash
# Linux/Mac
export Email__SmtpUsername="your-email@gmail.com"
export Email__SmtpPassword="your-app-password"

# Windows PowerShell
$env:Email__SmtpUsername="your-email@gmail.com"
$env:Email__SmtpPassword="your-app-password"
```

Hoặc trong `appsettings.Production.json`:
```json
{
  "Email": {
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password"
  }
}
```

---

**Chúc bạn cấu hình thành công! 🎉**

Nếu vẫn gặp vấn đề, hãy kiểm tra log trong console để xem chi tiết lỗi.

