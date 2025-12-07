# 🚀 Hướng Dẫn Deploy Social Login (Google & Facebook)

Tài liệu này hướng dẫn các bước cần thiết để đảm bảo chức năng đăng nhập Google và Facebook hoạt động đúng khi deploy lên production.

---

## 📋 Checklist Trước Khi Deploy

### 1. ✅ Cấu Hình Google OAuth

- [ ] **Google Cloud Console**
  - [ ] Đã tạo OAuth 2.0 Client ID
  - [ ] Đã cấu hình **Authorized JavaScript origins**:
    ```
    https://your-frontend-domain.com
    http://localhost:3000 (cho development)
    ```
  - [ ] Đã cấu hình **Authorized redirect URIs** (trỏ về frontend):
    ```
    https://your-frontend-domain.com/login
    http://localhost:3000/login (cho development)
    ```
  - [ ] Đã copy **Client ID** và **Client Secret**

- [ ] **Backend Configuration**
  - [ ] Đã thêm `Google:ClientId` vào appsettings.json hoặc environment variables
  - [ ] Đã thêm `Google:ClientSecret` vào appsettings.json hoặc environment variables
  - [ ] Đã test đăng nhập Google thành công ở môi trường development

### 2. ✅ Cấu Hình Facebook OAuth

- [ ] **Facebook Developers**
  - [ ] Đã tạo Facebook App
  - [ ] Đã thêm **Facebook Login** product
  - [ ] Đã cấu hình **Valid OAuth Redirect URIs** (trỏ về frontend):
    ```
    https://your-frontend-domain.com/login
    http://localhost:3000/login (cho development)
    ```
  - [ ] Đã cấu hình **App Domains**:
    ```
    your-frontend-domain.com
    localhost (cho development)
    ```
  - [ ] Đã request permissions: `email`, `public_profile`
  - [ ] Đã copy **App ID** và **App Secret**

- [ ] **Backend Configuration**
  - [ ] Đã thêm `Facebook:AppId` vào appsettings.json hoặc environment variables
  - [ ] Đã thêm `Facebook:AppSecret` vào appsettings.json hoặc environment variables
  - [ ] Đã test đăng nhập Facebook thành công ở môi trường development

### 3. ✅ Cấu Hình CORS

- [ ] **Backend Configuration**
  - [ ] Đã cập nhật `Cors:AllowedOrigins` trong appsettings.json với domain frontend production:
    ```json
    "Cors": {
      "AllowedOrigins": [
        "https://your-frontend-domain.com",
        "http://localhost:3000"
      ]
    }
    ```
  - [ ] Đã test CORS hoạt động đúng với frontend

### 4. ✅ Bảo Mật Secrets

- [ ] **Git Configuration**
  - [ ] Đã kiểm tra `.gitignore` có bỏ qua `appsettings.Production.json`
  - [ ] Đã đảm bảo không commit secrets vào Git
  - [ ] Đã sử dụng environment variables hoặc secret management cho production

- [ ] **Production Secrets**
  - [ ] Đã cấu hình secrets trên hosting platform (Render, Azure, AWS, etc.)
  - [ ] Đã test đọc được secrets từ environment variables

### 5. ✅ Database

- [ ] **Database Schema**
  - [ ] Đã chạy migrations để đảm bảo có các cột:
    - `Users.GoogleId` (nullable string)
    - `Users.FacebookId` (nullable string)
  - [ ] Đã kiểm tra database có thể lưu được GoogleId và FacebookId

### 6. ✅ Frontend Integration

- [ ] **Frontend Configuration**
  - [ ] Đã cấu hình Google Client ID trong frontend
  - [ ] Đã cấu hình Facebook App ID trong frontend
  - [ ] Đã test flow đăng nhập:
    1. User click đăng nhập Google/Facebook
    2. Redirect đến Google/Facebook để authorize
    3. Redirect về frontend với token
    4. Frontend gửi token lên backend API `/api/auth/google` hoặc `/api/auth/facebook`
    5. Backend trả về JWT token
    6. Frontend lưu JWT token và redirect đến trang chủ

---

## 🔧 Cấu Hình Chi Tiết

### 1. Environment Variables (Khuyến nghị cho Production)

Thay vì lưu secrets trong `appsettings.json`, sử dụng environment variables:

#### Trên Render.com:
1. Vào **Environment** tab trong service settings
2. Thêm các variables:
   ```
   Google__ClientId=873979098760-9cbdcjnrspc4o0sfekq809c0iiqujtu7.apps.googleusercontent.com
   Google__ClientSecret=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
   Facebook__AppId=842051432020279
   Facebook__AppSecret=19ab64f4f84998a78db86db1a2fbc2e8
   Cors__AllowedOrigins__0=https://your-frontend-domain.com
   ```

#### Trên Azure App Service:
1. Vào **Configuration** → **Application settings**
2. Thêm các settings với format `Google:ClientId`, `Facebook:AppId`, etc.

#### Trên AWS (EC2/ECS):
```bash
export Google__ClientId="873979098760-9cbdcjnrspc4o0sfekq809c0iiqujtu7.apps.googleusercontent.com"
export Google__ClientSecret="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"
export Facebook__AppId="842051432020279"
export Facebook__AppSecret="19ab64f4f84998a78db86db1a2fbc2e8"
```

### 2. Cập Nhật appsettings.json cho Production

Nếu không dùng environment variables, tạo file `appsettings.Production.json`:

```json
{
  "Google": {
    "ClientId": "873979098760-9cbdcjnrspc4o0sfekq809c0iiqujtu7.apps.googleusercontent.com",
    "ClientSecret": "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"
  },
  "Facebook": {
    "AppId": "842051432020279",
    "AppSecret": "19ab64f4f84998a78db86db1a2fbc2e8"
  },
  "Cors": {
    "AllowedOrigins": [
      "https://your-frontend-domain.com"
    ]
  }
}
```

**⚠️ QUAN TRỌNG:** File này **KHÔNG** được commit vào Git!

### 3. Cập Nhật Redirect URIs

#### Google Cloud Console:
1. Vào **APIs & Services** → **Credentials**
2. Click vào OAuth 2.0 Client ID của bạn
3. Thêm vào **Authorized redirect URIs**:
   ```
   https://your-frontend-domain.com/login
   https://your-frontend-domain.com/auth/callback
   ```

#### Facebook Developers:
1. Vào **Facebook Login** → **Settings**
2. Thêm vào **Valid OAuth Redirect URIs**:
   ```
   https://your-frontend-domain.com/login
   https://your-frontend-domain.com/auth/callback
   ```

---

## 🧪 Testing Sau Khi Deploy

### 1. Test Google Login

1. Mở frontend production
2. Click "Đăng nhập bằng Google"
3. Chọn tài khoản Google
4. Kiểm tra:
   - [ ] Redirect về frontend thành công
   - [ ] Backend nhận được id_token
   - [ ] Backend trả về JWT token
   - [ ] User được tạo hoặc cập nhật trong database
   - [ ] JWT token hoạt động để gọi các API khác

### 2. Test Facebook Login

1. Mở frontend production
2. Click "Đăng nhập bằng Facebook"
3. Chọn tài khoản Facebook
4. Kiểm tra:
   - [ ] Redirect về frontend thành công
   - [ ] Backend nhận được access_token
   - [ ] Backend trả về JWT token
   - [ ] User được tạo hoặc cập nhật trong database
   - [ ] JWT token hoạt động để gọi các API khác

### 3. Test Edge Cases

- [ ] **User đăng nhập lần đầu**: User mới được tạo với GoogleId/FacebookId
- [ ] **User đăng nhập lại**: User hiện có được cập nhật thông tin
- [ ] **User đã có tài khoản (email/password)**: Liên kết GoogleId/FacebookId với tài khoản hiện có
- [ ] **Facebook không có email**: User được tạo với email tạm, IsEmailVerified = false

---

## 🐛 Troubleshooting

### Lỗi: "Google token không hợp lệ"

**Nguyên nhân:**
- Client ID không khớp
- Token đã hết hạn
- Redirect URI không khớp

**Giải pháp:**
1. Kiểm tra `Google:ClientId` trong backend có đúng không
2. Kiểm tra Redirect URI trong Google Console có khớp với frontend URL không
3. Kiểm tra logs backend để xem chi tiết lỗi

### Lỗi: "Facebook token không hợp lệ"

**Nguyên nhân:**
- App ID không khớp
- Token đã hết hạn
- Redirect URI không khớp
- App chưa được approve permissions

**Giải pháp:**
1. Kiểm tra `Facebook:AppId` trong backend có đúng không
2. Kiểm tra Redirect URI trong Facebook App có khớp với frontend URL không
3. Kiểm tra App đã request permissions `email` và `public_profile` chưa
4. Kiểm tra logs backend để xem chi tiết lỗi

### Lỗi: "CORS policy blocked"

**Nguyên nhân:**
- Frontend domain không có trong `Cors:AllowedOrigins`
- CORS chưa được cấu hình đúng

**Giải pháp:**
1. Kiểm tra `Cors:AllowedOrigins` trong appsettings.json có chứa frontend domain không
2. Đảm bảo format đúng: `https://your-frontend-domain.com` (không có trailing slash)
3. Restart backend sau khi cập nhật CORS config

### Lỗi: "Facebook user info missing Email"

**Nguyên nhân:**
- User không cấp quyền email cho Facebook App
- App chưa được approve permission `email`

**Giải pháp:**
1. User sẽ được tạo với email tạm: `fb_{facebookId}@facebook.temp`
2. User cần cập nhật email sau khi đăng nhập
3. Đảm bảo Facebook App đã request permission `email` và được approve

### Lỗi: Database không có cột GoogleId/FacebookId

**Nguyên nhân:**
- Chưa chạy migrations
- Database schema chưa được cập nhật

**Giải pháp:**
```bash
# Chạy migrations
dotnet ef database update

# Hoặc nếu deploy trên hosting platform, đảm bảo migrations được chạy tự động
```

---

## 📝 Logs và Monitoring

### Kiểm Tra Logs Backend

Các log quan trọng để debug:

```
[Information] Verifying Google token with URL: ...
[Information] Google tokeninfo response received: ...
[Information] Google token verified successfully for user: ...
[Warning] Google token verification failed: ...
```

```
[Information] Verifying Facebook token with URL: ...
[Information] Facebook Graph API response received: ...
[Information] Facebook token verified successfully for user: ...
[Warning] Facebook token verification failed: ...
```

### Monitoring

- [ ] Đã setup monitoring cho social login endpoints
- [ ] Đã setup alert khi có nhiều lỗi authentication
- [ ] Đã track số lượng user đăng nhập bằng Google/Facebook

---

## ✅ Final Checklist

Trước khi go-live, đảm bảo:

- [ ] Tất cả checklist trên đã hoàn thành
- [ ] Đã test đăng nhập Google thành công trên production
- [ ] Đã test đăng nhập Facebook thành công trên production
- [ ] Đã test các edge cases
- [ ] Đã kiểm tra logs không có lỗi
- [ ] Đã đảm bảo secrets không bị expose
- [ ] Đã cấu hình CORS đúng
- [ ] Đã cập nhật Redirect URIs trong Google Console và Facebook App
- [ ] Frontend đã được cập nhật với production URLs

---

## 📞 Hỗ Trợ

Nếu gặp vấn đề:

1. Kiểm tra logs backend để xem chi tiết lỗi
2. Kiểm tra browser console để xem lỗi frontend
3. Kiểm tra Google Cloud Console / Facebook Developers để xem cấu hình
4. Tham khảo file `HUONG_DAN_CAU_HINH_OAUTH.md` để cấu hình lại

---

**Version:** 1.0  
**Last Updated:** 2024-11-13

