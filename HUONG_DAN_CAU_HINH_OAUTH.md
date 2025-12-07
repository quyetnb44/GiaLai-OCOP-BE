# Hướng Dẫn Cấu Hình OAuth (Google & Facebook)

## 📋 Mục Lục
1. [Cấu hình Google OAuth](#1-cấu-hình-google-oauth)
2. [Cấu hình Facebook OAuth](#2-cấu-hình-facebook-oauth)
3. [Cập nhật appsettings.json](#3-cập-nhật-appsettingsjson)
4. [Bảo mật thông tin nhạy cảm](#4-bảo-mật-thông-tin-nhạy-cảm)

---

## 1. Cấu Hình Google OAuth

### Bước 1: Truy cập Google Cloud Console
1. Đi tới: https://console.cloud.google.com/
2. Đăng nhập bằng tài khoản Google của bạn
3. Chọn project của bạn (hoặc tạo project mới)

### Bước 2: Tạo OAuth 2.0 Credentials
1. Vào **APIs & Services** → **Credentials**
2. Nếu chưa có OAuth consent screen, tạo mới:
   - Chọn **User Type**: External (hoặc Internal nếu dùng Google Workspace)
   - Điền thông tin ứng dụng:
     - App name: `GiaLai OCOP`
     - User support email: Email của bạn
     - Developer contact: Email của bạn
   - Thêm scopes: `email`, `profile`, `openid`
   - Thêm test users (nếu cần)

3. Tạo OAuth 2.0 Client ID:
   - Click **Create Credentials** → **OAuth client ID**
   - Application type: **Web application**
   - Name: `GiaLai OCOP Web Client`
   - **Authorized JavaScript origins**:
     ```
     http://localhost:3000
     https://yourdomain.com
     ```
   - **Authorized redirect URIs**: 
     
     ⚠️ **QUAN TRỌNG**: Redirect URI phải trỏ về **FRONTEND** (nơi user được redirect sau khi authorize), **KHÔNG phải backend**!
     
     ```
     http://localhost:3000/login          (Development)
     https://your-frontend-domain.com/login  (Production)
     ```
     
     **Ví dụ cụ thể:**
     - ✅ Đúng: `https://gialai-ocop-fe.vercel.app/login` (frontend URL)
     - ❌ Sai: `https://gialai-ocop-be.onrender.com/auth/google/callback` (backend URL)
     
     **Lý do:**
     - Google sẽ redirect user về frontend với authorization code
     - Frontend nhận code và gửi lên backend API `/api/auth/google`
     - Backend không có endpoint callback, chỉ có API endpoint để nhận code
   - Click **Create**

### Bước 3: Lấy Client ID và Client Secret
1. Sau khi tạo, bạn sẽ thấy popup hiển thị:
   - **Client ID**: `881859864614-4l0gkv983cimnhgf29iblt436u578fa2.apps.googleusercontent.com` (đã có)
   - **Client Secret**: Copy giá trị này (chỉ hiển thị 1 lần!)

2. Nếu đã mất Client Secret:
   - Vào **Credentials** → Click vào OAuth client ID của bạn
   - Click **Reset Secret** để tạo secret mới
   - **Lưu ý**: Secret cũ sẽ không còn hoạt động

---

## 2. Cấu Hình Facebook OAuth

### Bước 1: Truy cập Facebook Developers
1. Đi tới: https://developers.facebook.com/
2. Đăng nhập bằng tài khoản Facebook của bạn
3. Click **My Apps** → **Create App**

### Bước 2: Tạo Facebook App
1. Chọn loại app: **Consumer** hoặc **Business**
2. Điền thông tin:
   - App Name: `GiaLai OCOP`
   - App Contact Email: Email của bạn
   - Click **Create App**

### Bước 3: Thêm Facebook Login Product
1. Trong Dashboard, tìm **Add Product**
2. Chọn **Facebook Login** → **Set Up**
3. Chọn **Web** platform

### Bước 4: Cấu hình Facebook Login
1. Vào **Facebook Login** → **Settings**
2. **Valid OAuth Redirect URIs**: 
   
   ⚠️ **QUAN TRỌNG**: Redirect URI phải trỏ về **FRONTEND** (nơi user được redirect sau khi authorize), **KHÔNG phải backend**!
   
   ```
   http://localhost:3000/login          (Development)
   https://your-frontend-domain.com/login  (Production)
   ```
   
   **Ví dụ cụ thể:**
   - ✅ Đúng: `https://gialai-ocop-fe.vercel.app/login` (frontend URL)
   - ❌ Sai: `https://gialai-ocop-be.onrender.com/auth/facebook/callback` (backend URL)
   
   **Lý do:**
   - Facebook sẽ redirect user về frontend với authorization code
   - Frontend nhận code và gửi lên backend API `/api/auth/facebook`
   - Backend không có endpoint callback, chỉ có API endpoint để nhận code
   
3. Click **Save Changes**

### Bước 5: Lấy App ID và App Secret
1. Vào **Settings** → **Basic**
2. Bạn sẽ thấy:
   - **App ID**: Copy giá trị này
   - **App Secret**: Click **Show** và copy (cần xác thực password)

### Bước 6: Cấu hình App Domains (Quan trọng!)
1. Vào **Settings** → **Basic**
2. **App Domains**: Thêm domain của bạn
   ```
   localhost
   yourdomain.com
   ```
3. **Privacy Policy URL**: URL chính sách bảo mật (bắt buộc)
4. **Terms of Service URL**: URL điều khoản sử dụng (tùy chọn)
5. Click **Save Changes**

### Bước 7: Chuyển App sang Production Mode (Khi deploy)
1. Vào **App Review** → **Permissions and Features**
2. Request các permissions cần thiết: `email`, `public_profile`
3. Sau khi được approve, chuyển app sang **Live Mode**

---

## 3. Cập Nhật appsettings.json

### Cách 1: Cập nhật trực tiếp (Chỉ dùng cho Development)

Mở file `appsettings.json` và cập nhật:

```json
{
  "Google": {
    "ClientId": "881859864614-4l0gkv983cimnhgf29iblt436u578fa2.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-xxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
  },
  "Facebook": {
    "AppId": "1234567890123456",
    "AppSecret": "abcdefghijklmnopqrstuvwxyz123456"
  }
}
```

**⚠️ LƯU Ý QUAN TRỌNG:**
- **KHÔNG** commit file `appsettings.json` có chứa secrets vào Git!
- Sử dụng User Secrets hoặc Environment Variables cho Production

### Cách 2: Sử dụng User Secrets (Khuyến nghị cho Development)

1. Mở Terminal/PowerShell trong thư mục project
2. Chạy lệnh:

```bash
# Google Client Secret
dotnet user-secrets set "Google:ClientSecret" "GOCSPX-xxxxxxxxxxxxxxxxxxxxxxxxxxxxx"

# Facebook App ID
dotnet user-secrets set "Facebook:AppId" "1234567890123456"

# Facebook App Secret
dotnet user-secrets set "Facebook:AppSecret" "abcdefghijklmnopqrstuvwxyz123456"
```

3. Xác minh đã lưu:

```bash
dotnet user-secrets list
```

### Cách 3: Sử dụng Environment Variables (Khuyến nghị cho Production)

#### Trên Windows (PowerShell):
```powershell
$env:Google__ClientSecret = "GOCSPX-xxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
$env:Facebook__AppId = "1234567890123456"
$env:Facebook__AppSecret = "abcdefghijklmnopqrstuvwxyz123456"
```

#### Trên Linux/Mac:
```bash
export Google__ClientSecret="GOCSPX-xxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
export Facebook__AppId="1234567890123456"
export Facebook__AppSecret="abcdefghijklmnopqrstuvwxyz123456"
```

#### Trên Azure App Service:
1. Vào **Configuration** → **Application settings**
2. Thêm các settings:
   - `Google__ClientSecret`: `GOCSPX-xxxxxxxxxxxxxxxxxxxxxxxxxxxxx`
   - `Facebook__AppId`: `1234567890123456`
   - `Facebook__AppSecret`: `abcdefghijklmnopqrstuvwxyz123456`

#### Trên Docker:
Thêm vào `docker-compose.yml`:
```yaml
environment:
  - Google__ClientSecret=GOCSPX-xxxxxxxxxxxxxxxxxxxxxxxxxxxxx
  - Facebook__AppId=1234567890123456
  - Facebook__AppSecret=abcdefghijklmnopqrstuvwxyz123456
```

---

## 4. Bảo Mật Thông Tin Nhạy Cảm

### Kiểm tra .gitignore

Đảm bảo file `.gitignore` có các dòng sau:

```
# User secrets
**/secrets.json
**/appsettings.*.json
!appsettings.json
!appsettings.Development.json

# User-specific files
*.user
*.suo
```

### Tạo appsettings.Production.json (Không commit)

Tạo file `appsettings.Production.json` với cấu trúc:

```json
{
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  },
  "Facebook": {
    "AppId": "YOUR_FACEBOOK_APP_ID",
    "AppSecret": "YOUR_FACEBOOK_APP_SECRET"
  }
}
```

**⚠️ QUAN TRỌNG:** File này **KHÔNG** được commit vào Git!

### Sử dụng Azure Key Vault (Cho Production)

1. Tạo Azure Key Vault
2. Lưu secrets vào Key Vault
3. Cấu hình trong `Program.cs`:

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

---

## 5. Kiểm Tra Cấu Hình

### Test Google OAuth

1. Restart backend server
2. Mở browser console và test đăng nhập Google
3. Kiểm tra logs backend để xem có lỗi không

### Test Facebook OAuth

1. Restart backend server
2. Mở browser console và test đăng nhập Facebook
3. Kiểm tra logs backend để xem có lỗi không

### Debug Logs

Nếu gặp lỗi, kiểm tra logs:

```csharp
// Trong SocialAuthService.cs, các log sẽ hiển thị:
_logger.LogInformation("Exchanging Google authorization code for token...");
_logger.LogInformation("Google token exchange successful");
_logger.LogError(ex, "Error exchanging Google code");
```

---

## 6. Troubleshooting

### Lỗi: "Invalid client secret"
- Kiểm tra lại Client Secret đã copy đúng chưa
- Đảm bảo không có khoảng trắng thừa
- Thử reset Client Secret và cập nhật lại

### Lỗi: "Redirect URI mismatch"
- Kiểm tra Redirect URI trong Google Console/Facebook App phải khớp chính xác
- Bao gồm cả `http://` và `https://`
- Bao gồm cả port number (nếu có)

### Lỗi: "App not in development mode"
- Facebook App phải ở chế độ Development để test
- Thêm test users vào Facebook App
- Request permissions cần thiết

### Lỗi: "Invalid authorization code"
- Code chỉ có hiệu lực trong thời gian ngắn (vài phút)
- Code chỉ dùng được 1 lần
- Đảm bảo redirect URI khớp chính xác

---

## 7. Checklist

- [ ] Đã tạo Google OAuth Client ID và Client Secret
- [ ] Đã cấu hình Authorized Redirect URIs trong Google Console
- [ ] Đã tạo Facebook App và lấy App ID, App Secret
- [ ] Đã cấu hình Valid OAuth Redirect URIs trong Facebook App
- [ ] Đã cập nhật appsettings.json hoặc User Secrets
- [ ] Đã kiểm tra .gitignore để không commit secrets
- [ ] Đã test đăng nhập Google thành công
- [ ] Đã test đăng nhập Facebook thành công

---

## 📞 Hỗ Trợ

Nếu gặp vấn đề, kiểm tra:
1. Logs backend để xem chi tiết lỗi
2. Browser console để xem lỗi frontend
3. Google Cloud Console / Facebook Developers để xem cấu hình

