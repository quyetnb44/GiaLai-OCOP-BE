# 📱 Hướng Dẫn Frontend - Tích Hợp Social Login

Tài liệu hướng dẫn chi tiết cho Frontend developers về cách tích hợp đăng nhập Google và Facebook với Backend API.

---

## 📋 Tổng Quan

Backend đã cung cấp 2 API endpoints:
- `POST /api/auth/google` - Đăng nhập/đăng ký bằng Google
- `POST /api/auth/facebook` - Đăng nhập/đăng ký bằng Facebook

**Response format:**
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
    "avatarUrl": "https://...",
    ...
  }
}
```

---

## 🔵 Tích Hợp Google Login

### Bước 1: Setup Google OAuth 2.0

1. **Tạo Google Cloud Project:**
   - Truy cập: https://console.cloud.google.com/
   - Tạo project mới hoặc chọn project có sẵn

2. **Tạo OAuth 2.0 Credentials:**
   - Vào **APIs & Services** → **Credentials**
   - Click **Create Credentials** → **OAuth client ID**
   - Application type: **Web application**
   - Authorized JavaScript origins: Thêm domain của bạn
     ```
     http://localhost:3000 (Development)
     https://yourdomain.com (Production)
     ```
   - Authorized redirect URIs: Thêm callback URL (nếu cần)
   - Copy **Client ID** (dạng: `123456789-abc.apps.googleusercontent.com`)

### Bước 2: Cài đặt Google Sign-In SDK

#### Option 1: Web (HTML/JavaScript)

**Thêm script vào `<head>`:**
```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

**HTML:**
```html
<div id="google-signin-button"></div>
```

**JavaScript:**
```javascript
// Initialize Google Sign-In
window.onload = function() {
  google.accounts.id.initialize({
    client_id: 'YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com',
    callback: handleGoogleSignIn
  });
  
  // Render button
  google.accounts.id.renderButton(
    document.getElementById("google-signin-button"),
    { 
      theme: "outline", 
      size: "large",
      text: "signin_with",
      width: 300
    }
  );
};

// Handle response
function handleGoogleSignIn(response) {
  const idToken = response.credential;
  
  // Gửi idToken lên backend
  loginWithGoogle(idToken);
}
```

#### Option 2: React

**Cài đặt package:**
```bash
npm install @react-oauth/google
```

**Setup trong App.js:**
```jsx
import { GoogleOAuthProvider } from '@react-oauth/google';
import { GoogleLogin } from '@react-oauth/google';

function App() {
  return (
    <GoogleOAuthProvider clientId="YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com">
      <GoogleLogin
        onSuccess={(credentialResponse) => {
          const idToken = credentialResponse.credential;
          loginWithGoogle(idToken);
        }}
        onError={() => {
          console.log('Login Failed');
        }}
        useOneTap
      />
    </GoogleOAuthProvider>
  );
}
```

#### Option 3: React Native

**Cài đặt package:**
```bash
npm install @react-native-google-signin/google-signin
```

**Setup:**
```javascript
import { GoogleSignin } from '@react-native-google-signin/google-signin';

// Configure
GoogleSignin.configure({
  webClientId: 'YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com', // From Firebase Console
  offlineAccess: true,
});

// Login function
const signInWithGoogle = async () => {
  try {
    await GoogleSignin.hasPlayServices();
    const { idToken } = await GoogleSignin.signIn();
    
    // Gửi idToken lên backend
    await loginWithGoogle(idToken);
  } catch (error) {
    console.error('Google Sign-In Error:', error);
  }
};
```

### Bước 3: Gọi Backend API

```javascript
async function loginWithGoogle(idToken) {
  try {
    const response = await fetch('https://your-api.com/api/auth/google', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        idToken: idToken
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Đăng nhập thất bại');
    }

    const data = await response.json();
    
    // Lưu token vào localStorage/sessionStorage
    localStorage.setItem('token', data.token);
    localStorage.setItem('user', JSON.stringify(data.user));
    
    // Redirect hoặc update UI
    window.location.href = '/dashboard';
    
  } catch (error) {
    console.error('Login error:', error);
    alert(error.message);
  }
}
```

---

## 🔵 Tích Hợp Facebook Login

### Bước 1: Setup Facebook App

1. **Tạo Facebook App:**
   - Truy cập: https://developers.facebook.com/
   - Click **My Apps** → **Create App**
   - Chọn **Consumer** hoặc **Business**
   - Điền App Name, Contact Email

2. **Thêm Facebook Login:**
   - Vào **Add Product** → Chọn **Facebook Login**
   - Chọn **Web** hoặc **iOS/Android** tùy platform

3. **Cấu hình Settings:**
   - Vào **Settings** → **Basic**
   - Copy **App ID** và **App Secret**
   - Thêm **Valid OAuth Redirect URIs**:
     ```
     http://localhost:3000 (Development)
     https://yourdomain.com (Production)
     ```

### Bước 2: Cài đặt Facebook SDK

#### Option 1: Web (HTML/JavaScript)

**Thêm script vào `<head>`:**
```html
<script async defer crossorigin="anonymous" 
  src="https://connect.facebook.net/en_US/sdk.js"></script>
```

**JavaScript:**
```javascript
// Initialize Facebook SDK
window.fbAsyncInit = function() {
  FB.init({
    appId: 'YOUR_FACEBOOK_APP_ID',
    cookie: true,
    xfbml: true,
    version: 'v18.0'
  });
};

// Load SDK
(function(d, s, id) {
  var js, fjs = d.getElementsByTagName(s)[0];
  if (d.getElementById(id)) return;
  js = d.createElement(s); js.id = id;
  js.src = "https://connect.facebook.net/en_US/sdk.js";
  fjs.parentNode.insertBefore(js, fjs);
}(document, 'script', 'facebook-jssdk'));

// Login function
function loginWithFacebook() {
  FB.login(function(response) {
    if (response.authResponse) {
      const accessToken = response.authResponse.accessToken;
      
      // Gửi accessToken lên backend
      loginWithFacebookAPI(accessToken);
    } else {
      console.log('User cancelled login or did not fully authorize.');
    }
  }, { 
    scope: 'email,public_profile' // Quyền cần thiết
  });
}
```

#### Option 2: React

**Cài đặt package:**
```bash
npm install react-facebook-login
```

**Component:**
```jsx
import FacebookLogin from 'react-facebook-login';

function FacebookLoginButton() {
  const responseFacebook = (response) => {
    if (response.accessToken) {
      loginWithFacebookAPI(response.accessToken);
    }
  };

  return (
    <FacebookLogin
      appId="YOUR_FACEBOOK_APP_ID"
      autoLoad={false}
      fields="name,email,picture"
      callback={responseFacebook}
      scope="email,public_profile"
      cssClass="facebook-login-button"
      icon="fa-facebook"
    />
  );
}
```

#### Option 3: React Native

**Cài đặt package:**
```bash
npm install react-native-fbsdk-next
```

**Setup:**
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
    await loginWithFacebookAPI(accessToken);
  } catch (error) {
    console.error('Facebook Login Error:', error);
  }
};
```

### Bước 3: Gọi Backend API

```javascript
async function loginWithFacebookAPI(accessToken) {
  try {
    const response = await fetch('https://your-api.com/api/auth/facebook', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        accessToken: accessToken
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Đăng nhập thất bại');
    }

    const data = await response.json();
    
    // Lưu token vào localStorage/sessionStorage
    localStorage.setItem('token', data.token);
    localStorage.setItem('user', JSON.stringify(data.user));
    
    // Redirect hoặc update UI
    window.location.href = '/dashboard';
    
  } catch (error) {
    console.error('Login error:', error);
    alert(error.message);
  }
}
```

---

## 🎨 UI/UX Best Practices

### 1. Loading State

```javascript
const [isLoading, setIsLoading] = useState(false);

async function loginWithGoogle(idToken) {
  setIsLoading(true);
  try {
    // ... API call
  } finally {
    setIsLoading(false);
  }
}
```

### 2. Error Handling

```javascript
async function loginWithGoogle(idToken) {
  try {
    const response = await fetch('...');
    
    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Token không hợp lệ. Vui lòng thử lại.');
      }
      throw new Error('Đăng nhập thất bại. Vui lòng thử lại sau.');
    }
    
    // Success
  } catch (error) {
    // Hiển thị error message cho user
    showErrorToast(error.message);
  }
}
```

### 3. Token Management

```javascript
// Lưu token
function saveAuthData(data) {
  localStorage.setItem('token', data.token);
  localStorage.setItem('tokenExpires', data.expires);
  localStorage.setItem('user', JSON.stringify(data.user));
}

// Kiểm tra token còn hợp lệ
function isTokenValid() {
  const expires = localStorage.getItem('tokenExpires');
  if (!expires) return false;
  
  return new Date(expires) > new Date();
}

// Lấy token cho API calls
function getAuthToken() {
  if (!isTokenValid()) {
    // Token hết hạn, yêu cầu đăng nhập lại
    logout();
    return null;
  }
  return localStorage.getItem('token');
}

// Logout
function logout() {
  localStorage.removeItem('token');
  localStorage.removeItem('tokenExpires');
  localStorage.removeItem('user');
  window.location.href = '/login';
}
```

### 4. Axios Interceptor (Nếu dùng Axios)

```javascript
import axios from 'axios';

// Request interceptor - thêm token vào header
axios.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor - xử lý token hết hạn
axios.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token hết hạn hoặc không hợp lệ
      logout();
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);
```

---

## 📱 Ví Dụ Hoàn Chỉnh (React)

### GoogleLoginButton.jsx

```jsx
import React, { useState } from 'react';
import { GoogleOAuthProvider, GoogleLogin } from '@react-oauth/google';
import axios from 'axios';

const GoogleLoginButton = () => {
  const [isLoading, setIsLoading] = useState(false);
  const GOOGLE_CLIENT_ID = process.env.REACT_APP_GOOGLE_CLIENT_ID;

  const handleGoogleSuccess = async (credentialResponse) => {
    setIsLoading(true);
    
    try {
      const response = await axios.post('https://your-api.com/api/auth/google', {
        idToken: credentialResponse.credential
      });

      // Lưu token và user info
      localStorage.setItem('token', response.data.token);
      localStorage.setItem('user', JSON.stringify(response.data.user));
      
      // Redirect
      window.location.href = '/dashboard';
      
    } catch (error) {
      console.error('Google login error:', error);
      alert(error.response?.data?.message || 'Đăng nhập thất bại');
    } finally {
      setIsLoading(false);
    }
  };

  const handleGoogleError = () => {
    console.log('Google login failed');
    alert('Đăng nhập Google thất bại');
  };

  return (
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
      <GoogleLogin
        onSuccess={handleGoogleSuccess}
        onError={handleGoogleError}
        disabled={isLoading}
        text="signin_with"
        shape="rectangular"
        theme="outline"
        size="large"
      />
      {isLoading && <p>Đang xử lý...</p>}
    </GoogleOAuthProvider>
  );
};

export default GoogleLoginButton;
```

### FacebookLoginButton.jsx

```jsx
import React, { useState } from 'react';
import FacebookLogin from 'react-facebook-login';
import axios from 'axios';

const FacebookLoginButton = () => {
  const [isLoading, setIsLoading] = useState(false);
  const FACEBOOK_APP_ID = process.env.REACT_APP_FACEBOOK_APP_ID;

  const handleFacebookResponse = async (response) => {
    if (!response.accessToken) {
      return;
    }

    setIsLoading(true);
    
    try {
      const apiResponse = await axios.post('https://your-api.com/api/auth/facebook', {
        accessToken: response.accessToken
      });

      // Lưu token và user info
      localStorage.setItem('token', apiResponse.data.token);
      localStorage.setItem('user', JSON.stringify(apiResponse.data.user));
      
      // Redirect
      window.location.href = '/dashboard';
      
    } catch (error) {
      console.error('Facebook login error:', error);
      alert(error.response?.data?.message || 'Đăng nhập thất bại');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <FacebookLogin
      appId={FACEBOOK_APP_ID}
      autoLoad={false}
      fields="name,email,picture"
      callback={handleFacebookResponse}
      scope="email,public_profile"
      cssClass="facebook-login-button"
      icon="fa-facebook"
      textButton="Đăng nhập với Facebook"
      disabled={isLoading}
    />
  );
};

export default FacebookLoginButton;
```

### LoginPage.jsx

```jsx
import React from 'react';
import GoogleLoginButton from './GoogleLoginButton';
import FacebookLoginButton from './FacebookLoginButton';

const LoginPage = () => {
  return (
    <div className="login-container">
      <h1>Đăng nhập</h1>
      
      <div className="social-login-buttons">
        <GoogleLoginButton />
        <FacebookLoginButton />
      </div>
      
      <div className="divider">
        <span>HOẶC</span>
      </div>
      
      {/* Form đăng nhập thông thường */}
      <form>
        {/* Email/Password form */}
      </form>
    </div>
  );
};

export default LoginPage;
```

---

## 🔒 Security Best Practices

### 1. Environment Variables

**Không commit credentials vào code!**

Tạo file `.env`:
```env
REACT_APP_GOOGLE_CLIENT_ID=123456789-abc.apps.googleusercontent.com
REACT_APP_FACEBOOK_APP_ID=1234567890123456
REACT_APP_API_URL=https://your-api.com
```

### 2. HTTPS trong Production

- Luôn sử dụng HTTPS trong production
- Google và Facebook yêu cầu HTTPS cho OAuth

### 3. Token Storage

- **Web:** Sử dụng `localStorage` hoặc `sessionStorage`
- **React Native:** Sử dụng `AsyncStorage` hoặc `SecureStore`

### 4. Token Expiration

```javascript
// Kiểm tra token hết hạn trước mỗi API call
function checkTokenExpiration() {
  const expires = localStorage.getItem('tokenExpires');
  if (expires && new Date(expires) < new Date()) {
    // Token hết hạn, yêu cầu đăng nhập lại
    logout();
    return false;
  }
  return true;
}
```

---

## 🐛 Troubleshooting

### Lỗi: "Google token không hợp lệ"

**Nguyên nhân:**
- Token đã hết hạn (id_token có thời hạn ~1 giờ)
- Client ID không đúng
- Domain chưa được authorize trong Google Console

**Giải pháp:**
- Lấy token mới từ Google
- Kiểm tra Client ID trong Google Console
- Thêm domain vào Authorized JavaScript origins

### Lỗi: "Facebook token không hợp lệ"

**Nguyên nhân:**
- Access token đã hết hạn
- App ID không đúng
- Chưa request đúng permissions (email, public_profile)

**Giải pháp:**
- Lấy token mới từ Facebook
- Kiểm tra App ID trong Facebook Developers
- Đảm bảo scope có `email` và `public_profile`

### CORS Error

**Nguyên nhân:**
- Backend chưa cấu hình CORS cho domain của frontend

**Giải pháp:**
- Yêu cầu backend thêm domain vào CORS allowed origins
- Hoặc sử dụng proxy trong development

---

## 📋 Checklist

### Trước khi deploy:

- [ ] Đã tạo Google OAuth Client ID
- [ ] Đã tạo Facebook App ID
- [ ] Đã thêm domain vào Google Authorized origins
- [ ] Đã thêm domain vào Facebook Valid OAuth Redirect URIs
- [ ] Đã test login flow trên development
- [ ] Đã test login flow trên staging
- [ ] Đã cấu hình environment variables
- [ ] Đã implement error handling
- [ ] Đã implement loading states
- [ ] Đã test token expiration handling
- [ ] Đã test logout flow

---

## 📚 Tài Liệu Tham Khảo

- [Google Sign-In Documentation](https://developers.google.com/identity/sign-in/web)
- [Facebook Login Documentation](https://developers.facebook.com/docs/facebook-login/)
- [React Google OAuth](https://www.npmjs.com/package/@react-oauth/google)
- [React Facebook Login](https://www.npmjs.com/package/react-facebook-login)
- [React Native Google Sign-In](https://github.com/react-native-google-signin/google-signin)
- [React Native Facebook SDK](https://github.com/thebergamo/react-native-fbsdk-next)

---

## 💡 Tips

1. **Test trên nhiều browsers:** Chrome, Firefox, Safari, Edge
2. **Test trên mobile:** iOS Safari, Android Chrome
3. **Test với nhiều accounts:** Gmail, Facebook accounts khác nhau
4. **Monitor errors:** Sử dụng error tracking (Sentry, etc.)
5. **User feedback:** Hiển thị message rõ ràng khi có lỗi

---

**Version:** 1.0  
**Last Updated:** 2024-11-13


