# ⚠️ LƯU Ý QUAN TRỌNG: Redirect URI cho OAuth

## 🔴 Redirect URI PHẢI LÀ FRONTEND URL, KHÔNG PHẢI BACKEND URL!

### ❌ SAI:
```
https://gialai-ocop-be.onrender.com/auth/facebook/callback
https://gialai-ocop-be.onrender.com/auth/google/callback
```

### ✅ ĐÚNG:
```
http://localhost:3000/login                    (Development)
https://gialai-ocop-fe.vercel.app/login        (Production - nếu frontend deploy trên Vercel)
https://your-frontend-domain.com/login         (Production)
```

---

## 📋 Flow OAuth Authorization Code

### Cách hoạt động:

1. **User click "Đăng nhập với Google/Facebook"** trên frontend
2. **Frontend redirect** user đến Google/Facebook để authorize
3. **Google/Facebook redirect** user về **FRONTEND** với authorization code
   - URL: `https://your-frontend.com/login?code=xxxxx`
4. **Frontend nhận code** và gửi lên **Backend API**
   - POST `/api/auth/google` hoặc `/api/auth/facebook`
   - Body: `{ "code": "xxxxx", "redirectUri": "https://your-frontend.com/login" }`
5. **Backend đổi code lấy token** và lấy thông tin user
6. **Backend trả JWT** về frontend
7. **Frontend lưu JWT** và redirect user đến trang chính

---

## 🔧 Cấu Hình Redirect URI

### Google Console:
1. Vào: https://console.cloud.google.com/apis/credentials
2. Click vào OAuth 2.0 Client ID của bạn
3. **Authorized redirect URIs**: Thêm frontend URLs
   ```
   http://localhost:3000/login
   https://your-frontend-domain.com/login
   ```

### Facebook App:
1. Vào: https://developers.facebook.com/apps/
2. Chọn app → **Facebook Login** → **Settings**
3. **Valid OAuth Redirect URIs**: Thêm frontend URLs
   ```
   http://localhost:3000/login
   https://your-frontend-domain.com/login
   ```

---

## 🎯 Làm thế nào để biết Frontend URL của bạn?

### Development:
- Thường là: `http://localhost:3000` hoặc `http://localhost:3001`
- Redirect URI: `http://localhost:3000/login`

### Production:
- Kiểm tra nơi bạn deploy frontend:
  - **Vercel**: `https://your-app.vercel.app`
  - **Netlify**: `https://your-app.netlify.app`
  - **Custom domain**: `https://yourdomain.com`
- Redirect URI: `https://your-frontend-domain.com/login`

---

## ❓ Tại sao không dùng Backend URL?

1. **Backend không có callback endpoint**: Backend chỉ có API endpoint `/api/auth/google` và `/api/auth/facebook` để nhận code từ frontend
2. **Security**: Authorization code không nên được expose trực tiếp trên backend URL
3. **User Experience**: User sẽ bị redirect về backend thay vì frontend, gây confusion
4. **OAuth Best Practice**: Authorization code flow yêu cầu redirect về client application (frontend)

---

## ✅ Checklist

- [ ] Redirect URI là frontend URL (không phải backend)
- [ ] Redirect URI khớp chính xác (bao gồm protocol, domain, port, path)
- [ ] Đã cấu hình cả Development và Production URLs
- [ ] Đã test redirect URI hoạt động đúng


