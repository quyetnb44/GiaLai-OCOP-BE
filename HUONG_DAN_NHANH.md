# 🚀 Hướng Dẫn Nhanh: Cấu Hình OAuth Secrets

## ⚡ Cách Nhanh Nhất (Sử dụng User Secrets)

### 1. Lấy Google Client Secret
1. Vào: https://console.cloud.google.com/apis/credentials
2. Click vào OAuth 2.0 Client ID của bạn
3. Copy **Client Secret** (hoặc click **Reset Secret** nếu đã mất)

### 2. Lấy Facebook App ID và App Secret
1. Vào: https://developers.facebook.com/apps/
2. Chọn app của bạn → **Settings** → **Basic**
3. Copy **App ID** và **App Secret** (click Show để xem secret)

### 3. Cấu Hình User Secrets (Khuyến nghị)

Mở Terminal/PowerShell trong thư mục `D:\GiaLai-OCOP-BE` và chạy:

```powershell
# Google
dotnet user-secrets set "Google:ClientSecret" "GOCSPX-dán-client-secret-ở-đây"

# Facebook
dotnet user-secrets set "Facebook:AppId" "dán-app-id-ở-đây"
dotnet user-secrets set "Facebook:AppSecret" "dán-app-secret-ở-đây"
```

### 4. Kiểm Tra

```powershell
dotnet user-secrets list
```

Bạn sẽ thấy:
```
Google:ClientSecret = GOCSPX-...
Facebook:AppId = 123456789...
Facebook:AppSecret = abcdef...
```

### 5. Restart Backend và Test

Restart backend server và test đăng nhập Google/Facebook.

---

## 📝 Lưu Ý Quan Trọng

1. **KHÔNG** commit secrets vào Git
2. User Secrets chỉ hoạt động trong Development
3. Production: Sử dụng Environment Variables hoặc Azure Key Vault

---

## 🔗 Xem Hướng Dẫn Chi Tiết

Xem file `HUONG_DAN_CAU_HINH_OAUTH.md` để biết:
- Cách tạo OAuth credentials từ đầu
- Cấu hình redirect URIs
- Troubleshooting các lỗi thường gặp


