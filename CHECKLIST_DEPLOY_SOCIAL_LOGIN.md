# ✅ Checklist Deploy Social Login

Tài liệu kiểm tra trước khi deploy tính năng Social Login (Google & Facebook).

---

## 📋 Kiểm Tra Code

### ✅ Backend Implementation

- [x] **AuthController** - Đã có 2 endpoints:
  - [x] `POST /api/auth/google` - Line 864-959
  - [x] `POST /api/auth/facebook` - Line 962-1057

- [x] **SocialAuthService** - Đã implement:
  - [x] `VerifyGoogleTokenAsync()` - Xác thực Google id_token
  - [x] `VerifyFacebookTokenAsync()` - Xác thực Facebook access_token

- [x] **DTOs** - Đã tạo:
  - [x] `GoogleLoginDto.cs`
  - [x] `FacebookLoginDto.cs`

- [x] **User Model** - Đã thêm fields:
  - [x] `GoogleId` (string?)
  - [x] `FacebookId` (string?)

- [x] **AuthResponseDto** - Đã cập nhật:
  - [x] Thêm property `User` để trả về thông tin user

- [x] **Program.cs** - Đã register service:
  - [x] `builder.Services.AddHttpClient<ISocialAuthService, SocialAuthService>();`

- [x] **Migration** - Đã tạo:
  - [x] `20251201174746_AddSocialLoginIds.cs`

- [x] **Build** - Đã test:
  - [x] Build thành công (0 errors)
  - [x] Chỉ có warnings không liên quan (EnterprisesController)

---

## 🗄️ Database Migration

### ⚠️ QUAN TRỌNG: Cần chạy migration trước khi deploy!

```bash
# Kiểm tra migration chưa chạy
dotnet ef migrations list

# Chạy migration
dotnet ef database update

# Hoặc trên production (Render)
# Migration sẽ tự động chạy khi deploy nếu cấu hình đúng
```

**Migration sẽ thêm 2 cột vào bảng Users:**
- `GoogleId` (nvarchar, nullable)
- `FacebookId` (nvarchar, nullable)

---

## 🔧 Cấu Hình (Tùy chọn)

### Google Client ID (Optional)

Nếu muốn validate Google Client ID trên backend, thêm vào `appsettings.json`:

```json
{
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com"
  }
}
```

**Lưu ý:** Không bắt buộc. Backend vẫn hoạt động bình thường nếu không có.

### Facebook App ID

Không cần cấu hình trên backend. Backend chỉ cần access_token từ frontend.

---

## 🧪 Testing Checklist

### Trước khi deploy:

- [ ] **Test Google Login:**
  - [ ] Lấy Google id_token từ frontend/test tool
  - [ ] Gọi `POST /api/auth/google` với id_token hợp lệ
  - [ ] Kiểm tra response có `token`, `user`, `expires`
  - [ ] Kiểm tra user được tạo trong database với `GoogleId`
  - [ ] Test đăng nhập lại với cùng Google account
  - [ ] Test với token hết hạn (phải trả về 401)

- [ ] **Test Facebook Login:**
  - [ ] Lấy Facebook access_token từ frontend/test tool
  - [ ] Gọi `POST /api/auth/facebook` với access_token hợp lệ
  - [ ] Kiểm tra response có `token`, `user`, `expires`
  - [ ] Kiểm tra user được tạo trong database với `FacebookId`
  - [ ] Test đăng nhập lại với cùng Facebook account
  - [ ] Test với token hết hạn (phải trả về 401)

- [ ] **Test Edge Cases:**
  - [ ] User đã có account (đăng ký bằng email/password) → đăng nhập bằng Google/Facebook với cùng email
  - [ ] User đăng nhập Google lần đầu → tạo user mới
  - [ ] User đăng nhập Facebook lần đầu → tạo user mới
  - [ ] User đăng nhập Google → sau đó đăng nhập Facebook với cùng email → liên kết 2 accounts

---

## 🚀 Deploy Checklist

### Trước khi deploy lên Render:

- [ ] **Database:**
  - [ ] Đã chạy migration `AddSocialLoginIds` trên database production
  - [ ] Hoặc cấu hình để migration tự động chạy khi deploy

- [ ] **Environment Variables:**
  - [ ] Không cần thêm env vars mới (nếu không dùng Google Client ID validation)
  - [ ] Nếu dùng Google Client ID validation, thêm:
    ```
    Google__ClientId=your-client-id.apps.googleusercontent.com
    ```

- [ ] **CORS:**
  - [ ] Đã cấu hình CORS cho domain frontend trong `appsettings.json` hoặc environment variables
  - [ ] Production domain đã được thêm vào `Cors:AllowedOrigins`

- [ ] **Build:**
  - [ ] Code đã build thành công
  - [ ] Không có errors (warnings không ảnh hưởng)

- [ ] **Documentation:**
  - [ ] Đã gửi `HUONG_DAN_FRONTEND_SOCIAL_LOGIN.md` cho team Frontend
  - [ ] Frontend đã biết cách tích hợp

---

## 📝 Deploy Steps (Render)

### 1. Chạy Migration trên Production Database

**Option 1: Chạy thủ công**
```bash
# Kết nối đến production database
dotnet ef database update --connection "YOUR_PRODUCTION_CONNECTION_STRING"
```

**Option 2: Tự động khi deploy**
- Render có thể tự động chạy migration nếu cấu hình đúng
- Kiểm tra trong Render dashboard → Settings → Build Command

### 2. Deploy Code

- Push code lên Git repository
- Render sẽ tự động build và deploy
- Hoặc trigger manual deploy từ Render dashboard

### 3. Verify sau khi deploy

- [ ] Kiểm tra API health: `GET /health`
- [ ] Test Google login endpoint: `POST /api/auth/google`
- [ ] Test Facebook login endpoint: `POST /api/auth/facebook`
- [ ] Kiểm tra logs trong Render dashboard

---

## 🔍 Post-Deploy Verification

### 1. Test API Endpoints

**Google:**
```bash
curl -X POST https://your-api.onrender.com/api/auth/google \
  -H "Content-Type: application/json" \
  -d '{"idToken": "YOUR_GOOGLE_ID_TOKEN"}'
```

**Facebook:**
```bash
curl -X POST https://your-api.onrender.com/api/auth/facebook \
  -H "Content-Type: application/json" \
  -d '{"accessToken": "YOUR_FACEBOOK_ACCESS_TOKEN"}'
```

### 2. Kiểm tra Database

```sql
-- Kiểm tra cột đã được thêm
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'Users' 
AND column_name IN ('GoogleId', 'FacebookId');

-- Kiểm tra user đã được tạo với GoogleId/FacebookId
SELECT Id, Email, GoogleId, FacebookId, IsEmailVerified 
FROM "Users" 
WHERE GoogleId IS NOT NULL OR FacebookId IS NOT NULL;
```

### 3. Test với Frontend

- [ ] Frontend đã tích hợp Google Sign-In SDK
- [ ] Frontend đã tích hợp Facebook Login SDK
- [ ] Test flow hoàn chỉnh: Click button → Login → Nhận token → Lưu token
- [ ] Test với nhiều browsers: Chrome, Firefox, Safari, Edge
- [ ] Test trên mobile: iOS Safari, Android Chrome

---

## ⚠️ Lưu Ý Quan Trọng

### 1. Migration là BẮT BUỘC

**Nếu không chạy migration:**
- API sẽ báo lỗi khi tạo user mới (thiếu cột `GoogleId`, `FacebookId`)
- Database sẽ không có cột mới

**Cách kiểm tra migration đã chạy:**
```sql
-- PostgreSQL
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'Users' 
AND column_name IN ('GoogleId', 'FacebookId');
```

### 2. CORS Configuration

Đảm bảo frontend domain được thêm vào CORS allowed origins:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://your-frontend-domain.com",
      "http://localhost:3000"
    ]
  }
}
```

### 3. HTTPS Required

- Google và Facebook yêu cầu HTTPS trong production
- Render tự động cung cấp HTTPS
- Đảm bảo frontend cũng dùng HTTPS

### 4. Token Expiration

- Google id_token: ~1 giờ
- Facebook access_token: Tùy loại (short-lived hoặc long-lived)
- Frontend cần xử lý refresh token khi cần

---

## 📊 Status Summary

### ✅ Hoàn thành:

- [x] Backend code implementation
- [x] DTOs và Models
- [x] Services và Interfaces
- [x] Migration file
- [x] Documentation (3 files)
- [x] Build test (successful)

### ⚠️ Cần làm trước khi deploy:

- [ ] **Chạy migration trên database** (QUAN TRỌNG!)
- [ ] Test API endpoints với tokens thật
- [ ] Cấu hình CORS cho production domain
- [ ] Gửi documentation cho Frontend team

### 📝 Sau khi deploy:

- [ ] Verify API hoạt động
- [ ] Test với Frontend
- [ ] Monitor logs và errors
- [ ] Test với nhiều users

---

## 🆘 Troubleshooting

### Lỗi: "Column 'GoogleId' does not exist"

**Nguyên nhân:** Migration chưa chạy

**Giải pháp:**
```bash
dotnet ef database update
```

### Lỗi: "Google token không hợp lệ"

**Nguyên nhân:**
- Token đã hết hạn
- Token không đúng format
- Client ID không khớp (nếu có cấu hình)

**Giải pháp:**
- Lấy token mới từ Google
- Kiểm tra Client ID trong Google Console

### Lỗi: "Facebook token không hợp lệ"

**Nguyên nhân:**
- Access token đã hết hạn
- Token không có quyền email
- App ID không đúng

**Giải pháp:**
- Lấy token mới từ Facebook
- Đảm bảo scope có `email` và `public_profile`

### CORS Error

**Nguyên nhân:** Frontend domain chưa được thêm vào CORS

**Giải pháp:**
- Thêm domain vào `Cors:AllowedOrigins` trong `appsettings.json`
- Hoặc cấu hình qua environment variables trên Render

---

## 📞 Support

Nếu gặp vấn đề:
1. Kiểm tra logs trong Render dashboard
2. Test API với Swagger hoặc Postman
3. Kiểm tra database migration đã chạy chưa
4. Xem lại documentation: `HUONG_DAN_TEST_SOCIAL_LOGIN.md`

---

**Version:** 1.0  
**Last Updated:** 2024-12-01  
**Status:** ✅ Ready for Deploy (sau khi chạy migration)

