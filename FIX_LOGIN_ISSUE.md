# 🔧 Hướng Dẫn Sửa Lỗi Đăng Nhập

## ❌ Vấn Đề

Bạn không thể đăng nhập vì:
- Các user cũ trong database có `IsEmailVerified = false` (hoặc NULL)
- Hệ thống yêu cầu email phải được xác thực trước khi đăng nhập
- User cũ được tạo trước khi có tính năng xác thực email

---

## ✅ Giải Pháp

Có 2 cách để sửa:

### **Cách 1: Cập nhật Database (Nhanh nhất - Khuyến nghị)**

Chạy SQL script để set `IsEmailVerified = true` cho tất cả user cũ:

1. **Mở database tool** (pgAdmin, DBeaver, hoặc terminal)
2. **Kết nối database** (thông tin trong `appsettings.json`)
3. **Chạy script:**

```sql
UPDATE "Users"
SET "IsEmailVerified" = true
WHERE "IsEmailVerified" = false OR "IsEmailVerified" IS NULL;
```

4. **Kiểm tra kết quả:**
```sql
SELECT "Id", "Name", "Email", "IsEmailVerified" 
FROM "Users";
```

Sau đó thử đăng nhập lại!

---

### **Cách 2: Xác Thực Email Cho User Cũ**

Nếu bạn muốn user cũ phải xác thực email (an toàn hơn):

1. **Gửi OTP xác thực:**
   ```
   POST /api/auth/resend-verification-otp
   {
     "email": "your-email@gmail.com"
   }
   ```

2. **Kiểm tra email và copy OTP**

3. **Xác thực email:**
   ```
   POST /api/auth/verify-email
   {
     "email": "your-email@gmail.com",
     "otpCode": "123456"
   }
   ```

4. **Đăng nhập lại**

---

## 🎯 Khuyến Nghị

**Cho Development/Testing:**
- Dùng **Cách 1** (update database) - nhanh và đơn giản

**Cho Production:**
- Dùng **Cách 2** (xác thực email) - an toàn hơn, đảm bảo email hợp lệ

---

## 📝 Lưu Ý

- User mới đăng ký qua `register-with-otp` sẽ tự động có `IsEmailVerified = true`
- Chỉ user cũ (tạo trước khi có tính năng này) mới cần xử lý
- Sau khi fix, tất cả user đều có thể đăng nhập bình thường

---

## ✅ Sau Khi Fix

Thử đăng nhập lại:
```
POST /api/auth/login
{
  "email": "your-email@gmail.com",
  "password": "your-password"
}
```

Nếu vẫn lỗi, kiểm tra:
- Email và password có đúng không
- User có tồn tại trong database không
- Xem log trong console để biết lỗi cụ thể

