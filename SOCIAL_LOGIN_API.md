# 🔐 Social Login API Documentation

Tài liệu hướng dẫn sử dụng API đăng nhập bằng Google và Facebook.

---

## 📋 Tổng quan

Backend hỗ trợ 2 API endpoints để đăng nhập/đăng ký bằng tài khoản Google hoặc Facebook:

- `POST /api/auth/google` - Đăng nhập bằng Google id_token
- `POST /api/auth/facebook` - Đăng nhập bằng Facebook access_token

Cả 2 API đều sẽ:
1. Xác thực token với Google/Facebook
2. Tạo user mới nếu chưa tồn tại
3. Cập nhật thông tin user nếu đã tồn tại
4. Trả về JWT token và thông tin user

---

## 🔵 POST /api/auth/google

### Request

**Endpoint:** `POST /api/auth/google`

**Headers:**
```
Content-Type: application/json
```

**Body:**
```json
{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6Ij..."
}
```

### Response

**Success (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": "2024-11-13T10:30:00Z",
  "message": "Đăng nhập bằng Google thành công.",
  "user": {
    "id": 1,
    "name": "Nguyễn Văn A",
    "email": "user@gmail.com",
    "role": "Customer",
    "isEmailVerified": true,
    "isActive": true,
    "avatarUrl": "https://lh3.googleusercontent.com/...",
    "createdAt": "2024-11-12T08:00:00Z",
    ...
  }
}
```

**Error (401 Unauthorized):**
```json
{
  "message": "Google token không hợp lệ hoặc đã hết hạn."
}
```

### Cách lấy Google id_token từ Frontend

**JavaScript (Google Sign-In):**
```javascript
// 1. Load Google Sign-In library
<script src="https://accounts.google.com/gsi/client" async defer></script>

// 2. Initialize Google Sign-In
window.onload = function () {
  google.accounts.id.initialize({
    client_id: 'YOUR_GOOGLE_CLIENT_ID',
    callback: handleCredentialResponse
  });
  
  google.accounts.id.renderButton(
    document.getElementById("buttonDiv"),
    { theme: "outline", size: "large" }
  );
};

// 3. Handle response
function handleCredentialResponse(response) {
  // response.credential chính là id_token
  const idToken = response.credential;
  
  // Gửi id_token lên backend
  fetch('https://your-api.com/api/auth/google', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ idToken: idToken })
  })
  .then(res => res.json())
  .then(data => {
    // Lưu JWT token
    localStorage.setItem('token', data.token);
    // Lưu thông tin user
    localStorage.setItem('user', JSON.stringify(data.user));
  });
}
```

**React Native (react-native-google-signin):**
```javascript
import { GoogleSignin } from '@react-native-google-signin/google-signin';

GoogleSignin.configure({
  webClientId: 'YOUR_GOOGLE_CLIENT_ID', // From Firebase Console
});

const signIn = async () => {
  try {
    await GoogleSignin.hasPlayServices();
    const { idToken } = await GoogleSignin.signIn();
    
    // Gửi idToken lên backend
    const response = await fetch('https://your-api.com/api/auth/google', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ idToken })
    });
    
    const data = await response.json();
    // Lưu token và user info
  } catch (error) {
    console.error(error);
  }
};
```

---

## 🔵 POST /api/auth/facebook

### Request

**Endpoint:** `POST /api/auth/facebook`

**Headers:**
```
Content-Type: application/json
```

**Body:**
```json
{
  "accessToken": "EAABwzLix...your_access_token..."
}
```

### Response

**Success (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": "2024-11-13T10:30:00Z",
  "message": "Đăng nhập bằng Facebook thành công.",
  "user": {
    "id": 1,
    "name": "Nguyễn Văn A",
    "email": "user@facebook.com",
    "role": "Customer",
    "isEmailVerified": true,
    "isActive": true,
    "avatarUrl": "https://graph.facebook.com/...",
    "createdAt": "2024-11-12T08:00:00Z",
    ...
  }
}
```

**Error (401 Unauthorized):**
```json
{
  "message": "Facebook token không hợp lệ hoặc đã hết hạn."
}
```

### Cách lấy Facebook access_token từ Frontend

**JavaScript (Facebook SDK):**
```javascript
// 1. Load Facebook SDK
<script async defer crossorigin="anonymous" 
  src="https://connect.facebook.net/en_US/sdk.js"></script>

// 2. Initialize Facebook SDK
window.fbAsyncInit = function() {
  FB.init({
    appId: 'YOUR_FACEBOOK_APP_ID',
    cookie: true,
    xfbml: true,
    version: 'v18.0'
  });
};

// 3. Login với Facebook
function loginWithFacebook() {
  FB.login(function(response) {
    if (response.authResponse) {
      const accessToken = response.authResponse.accessToken;
      
      // Gửi accessToken lên backend
      fetch('https://your-api.com/api/auth/facebook', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ accessToken: accessToken })
      })
      .then(res => res.json())
      .then(data => {
        // Lưu JWT token
        localStorage.setItem('token', data.token);
        // Lưu thông tin user
        localStorage.setItem('user', JSON.stringify(data.user));
      });
    }
  }, { scope: 'email,public_profile' });
}
```

**React Native (react-native-fbsdk-next):**
```javascript
import { LoginManager, AccessToken } from 'react-native-fbsdk-next';

const loginWithFacebook = async () => {
  try {
    const result = await LoginManager.logInWithPermissions(['public_profile', 'email']);
    
    if (result.isCancelled) {
      console.log('Login cancelled');
      return;
    }
    
    const data = await AccessToken.getCurrentAccessToken();
    const accessToken = data.accessToken;
    
    // Gửi accessToken lên backend
    const response = await fetch('https://your-api.com/api/auth/facebook', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ accessToken })
    });
    
    const responseData = await response.json();
    // Lưu token và user info
  } catch (error) {
    console.error(error);
  }
};
```

---

## 🔧 Cấu hình Backend (Tùy chọn)

Nếu muốn xác thực Google Client ID trên backend, thêm vào `appsettings.json`:

```json
{
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com"
  }
}
```

**Lưu ý:** Nếu không cấu hình, backend vẫn hoạt động nhưng không kiểm tra audience của token.

---

## 📝 Logic xử lý

### Khi user đăng nhập lần đầu:
1. Backend xác thực token với Google/Facebook
2. Tạo user mới với:
   - `GoogleId` hoặc `FacebookId` được lưu
   - `IsEmailVerified = true` (vì đã được Google/Facebook xác thực)
   - `Role = "Customer"` (mặc định)
   - `Password` được tạo ngẫu nhiên (không cần dùng)
3. Trả về JWT token và thông tin user

### Khi user đăng nhập lại:
1. Backend xác thực token
2. Tìm user theo `GoogleId`/`FacebookId` hoặc `Email`
3. Cập nhật thông tin nếu có thay đổi (avatar, tên)
4. Trả về JWT token và thông tin user

### Khi user đã có tài khoản (đăng ký bằng email/password):
- Nếu user đăng nhập bằng Google/Facebook với email đã tồn tại:
  - Backend sẽ liên kết `GoogleId`/`FacebookId` với tài khoản hiện có
  - User có thể đăng nhập bằng cả 2 cách (email/password hoặc social login)

---

## 🚀 Deployment trên Render

### Environment Variables

Không cần thêm environment variables đặc biệt cho social login. Backend sẽ tự động:
- Gọi Google tokeninfo API để xác thực
- Gọi Facebook Graph API để xác thực

### CORS Configuration

Đảm bảo CORS cho phép domain của frontend:

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

---

## ⚠️ Lưu ý

1. **Google id_token có thời hạn:** Token chỉ có hiệu lực trong khoảng 1 giờ. Frontend cần lấy token mới nếu token cũ hết hạn.

2. **Facebook access_token:** Có thể có thời hạn ngắn hoặc dài tùy loại token. Frontend nên xử lý refresh token nếu cần.

3. **Email bắt buộc:** Cả Google và Facebook đều yêu cầu user cấp quyền email. Nếu user không cấp quyền, đăng nhập sẽ thất bại.

4. **Bảo mật:** 
   - Luôn sử dụng HTTPS trong production
   - Không lưu Google/Facebook tokens trên client quá lâu
   - Validate token trên backend (đã được implement)

---

## 🧪 Testing

### Test với Postman/curl

**Google:**
```bash
curl -X POST https://your-api.com/api/auth/google \
  -H "Content-Type: application/json" \
  -d '{"idToken": "YOUR_GOOGLE_ID_TOKEN"}'
```

**Facebook:**
```bash
curl -X POST https://your-api.com/api/auth/facebook \
  -H "Content-Type: application/json" \
  -d '{"accessToken": "YOUR_FACEBOOK_ACCESS_TOKEN"}'
```

---

## 📚 Tài liệu tham khảo

- [Google Sign-In Documentation](https://developers.google.com/identity/sign-in/web)
- [Facebook Login Documentation](https://developers.facebook.com/docs/facebook-login/)
- [Google Token Info API](https://developers.google.com/identity/sign-in/web/backend-auth#verify-the-integrity-of-the-id-token)
- [Facebook Graph API](https://developers.facebook.com/docs/graph-api)

---

**Version:** 1.0  
**Last Updated:** 2024-11-13


