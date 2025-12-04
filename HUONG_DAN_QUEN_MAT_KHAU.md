# 🔐 Hướng Dẫn Chức Năng Quên Mật Khẩu

Tài liệu hướng dẫn sử dụng chức năng quên mật khẩu và đặt lại mật khẩu trong GiaLai OCOP Backend API.

---

## 📋 Tổng Quan

Hệ thống hỗ trợ 2 chức năng liên quan đến mật khẩu:

1. **Đổi mật khẩu** (`PUT /api/auth/change-password`) - Yêu cầu đăng nhập và mật khẩu hiện tại
2. **Quên mật khẩu** (`POST /api/auth/forgot-password`) - Gửi OTP đến email
3. **Đặt lại mật khẩu** (`POST /api/auth/reset-password`) - Xác thực OTP và đặt mật khẩu mới

---

## 🔹 1. Quên Mật Khẩu - Gửi OTP

### Endpoint
```
POST /api/auth/forgot-password
```

### Request Body
```json
{
  "email": "user@example.com"
}
```

### Response (Thành công)
```json
{
  "message": "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi mã OTP đến email của bạn. Vui lòng kiểm tra hộp thư."
}
```

### Response (Development mode - Email service chưa cấu hình)
```json
{
  "message": "⚠️ Không thể gửi email. (Development mode - OTP: 123456)",
  "warning": "Email service chưa được cấu hình. Vui lòng cấu hình Email settings trong appsettings.json",
  "otpCode": "123456"
}
```

### Lưu ý
- ✅ **Security Best Practice**: API không tiết lộ email có tồn tại hay không (tránh email enumeration attack)
- ✅ Rate limiting: Chỉ cho phép gửi OTP 1 lần mỗi phút
- ✅ OTP có hiệu lực trong 10 phút
- ✅ OTP cũ sẽ tự động bị xóa khi hết hạn hoặc đã sử dụng

### Error Responses

**400 Bad Request** - Quá nhiều request
```json
{
  "message": "Vui lòng đợi 1 phút trước khi yêu cầu mã OTP mới."
}
```

**400 Bad Request** - Tài khoản bị vô hiệu hóa
```json
{
  "message": "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên."
}
```

**500 Internal Server Error** - Email service lỗi (Production)
```json
{
  "message": "Không thể gửi email. Vui lòng thử lại sau.",
  "error": "Email service configuration error"
}
```

---

## 🔹 2. Đặt Lại Mật Khẩu - Xác Thực OTP

### Endpoint
```
POST /api/auth/reset-password
```

### Request Body
```json
{
  "email": "user@example.com",
  "otpCode": "123456",
  "newPassword": "NewPassword123",
  "confirmNewPassword": "NewPassword123"
}
```

### Response (Thành công)
```json
{
  "message": "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập với mật khẩu mới.",
  "success": true
}
```

### Yêu Cầu Mật Khẩu Mới
- ✅ Độ dài: 6-100 ký tự
- ✅ Phải chứa ít nhất:
  - 1 chữ hoa (A-Z)
  - 1 chữ thường (a-z)
  - 1 số (0-9)
- ✅ Phải khác mật khẩu hiện tại

### Error Responses

**400 Bad Request** - OTP không hợp lệ
```json
{
  "message": "Mã OTP không hợp lệ hoặc đã hết hạn."
}
```

**400 Bad Request** - Mật khẩu không đúng format
```json
{
  "message": "Mật khẩu mới phải chứa ít nhất một chữ hoa, một chữ thường và một số"
}
```

**400 Bad Request** - Mật khẩu xác nhận không khớp
```json
{
  "message": "Mật khẩu xác nhận không khớp với mật khẩu mới"
}
```

**400 Bad Request** - Mật khẩu mới giống mật khẩu cũ
```json
{
  "message": "Mật khẩu mới phải khác mật khẩu hiện tại"
}
```

**400 Bad Request** - Tài khoản bị vô hiệu hóa
```json
{
  "message": "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên."
}
```

---

## 📱 Ví Dụ Sử Dụng (Frontend)

### JavaScript/TypeScript

```javascript
// Bước 1: Gửi yêu cầu quên mật khẩu
async function forgotPassword(email) {
  try {
    const response = await fetch('https://your-api.com/api/auth/forgot-password', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ email })
    });

    const data = await response.json();
    
    if (response.ok) {
      alert(data.message);
      // Hiển thị form nhập OTP
      showOtpForm();
    } else {
      alert(data.message || 'Đã xảy ra lỗi');
    }
  } catch (error) {
    console.error('Error:', error);
    alert('Đã xảy ra lỗi. Vui lòng thử lại sau.');
  }
}

// Bước 2: Đặt lại mật khẩu với OTP
async function resetPassword(email, otpCode, newPassword, confirmPassword) {
  try {
    const response = await fetch('https://your-api.com/api/auth/reset-password', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        email,
        otpCode,
        newPassword,
        confirmNewPassword: confirmPassword
      })
    });

    const data = await response.json();
    
    if (response.ok) {
      alert(data.message);
      // Redirect đến trang đăng nhập
      window.location.href = '/login';
    } else {
      alert(data.message || 'Đã xảy ra lỗi');
    }
  } catch (error) {
    console.error('Error:', error);
    alert('Đã xảy ra lỗi. Vui lòng thử lại sau.');
  }
}
```

### React Example

```jsx
import React, { useState } from 'react';
import axios from 'axios';

const ForgotPasswordPage = () => {
  const [step, setStep] = useState(1); // 1: Email, 2: OTP + New Password
  const [email, setEmail] = useState('');
  const [otpCode, setOtpCode] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState('');

  const handleForgotPassword = async (e) => {
    e.preventDefault();
    setLoading(true);
    setMessage('');

    try {
      const response = await axios.post('/api/auth/forgot-password', { email });
      setMessage(response.data.message);
      setStep(2); // Chuyển sang bước nhập OTP
    } catch (error) {
      setMessage(error.response?.data?.message || 'Đã xảy ra lỗi');
    } finally {
      setLoading(false);
    }
  };

  const handleResetPassword = async (e) => {
    e.preventDefault();
    setLoading(true);
    setMessage('');

    try {
      const response = await axios.post('/api/auth/reset-password', {
        email,
        otpCode,
        newPassword,
        confirmNewPassword: confirmPassword
      });
      setMessage(response.data.message);
      // Redirect sau 2 giây
      setTimeout(() => {
        window.location.href = '/login';
      }, 2000);
    } catch (error) {
      setMessage(error.response?.data?.message || 'Đã xảy ra lỗi');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="forgot-password-container">
      <h1>Quên Mật Khẩu</h1>

      {step === 1 && (
        <form onSubmit={handleForgotPassword}>
          <div>
            <label>Email</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              disabled={loading}
            />
          </div>
          <button type="submit" disabled={loading}>
            {loading ? 'Đang gửi...' : 'Gửi mã OTP'}
          </button>
          {message && <p className="message">{message}</p>}
        </form>
      )}

      {step === 2 && (
        <form onSubmit={handleResetPassword}>
          <div>
            <label>Mã OTP</label>
            <input
              type="text"
              value={otpCode}
              onChange={(e) => setOtpCode(e.target.value)}
              maxLength={6}
              required
              disabled={loading}
            />
          </div>
          <div>
            <label>Mật khẩu mới</label>
            <input
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
              disabled={loading}
            />
          </div>
          <div>
            <label>Xác nhận mật khẩu mới</label>
            <input
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              required
              disabled={loading}
            />
          </div>
          <button type="submit" disabled={loading}>
            {loading ? 'Đang xử lý...' : 'Đặt lại mật khẩu'}
          </button>
          {message && <p className="message">{message}</p>}
        </form>
      )}
    </div>
  );
};

export default ForgotPasswordPage;
```

---

## 🔒 Bảo Mật

### Security Best Practices Đã Áp Dụng

1. ✅ **Email Enumeration Protection**: API không tiết lộ email có tồn tại hay không
2. ✅ **Rate Limiting**: Chỉ cho phép gửi OTP 1 lần mỗi phút
3. ✅ **OTP Expiration**: OTP chỉ có hiệu lực trong 10 phút
4. ✅ **OTP One-Time Use**: Mỗi OTP chỉ được sử dụng 1 lần
5. ✅ **Password Strength**: Yêu cầu mật khẩu mạnh (chữ hoa, chữ thường, số)
6. ✅ **Password History**: Kiểm tra mật khẩu mới phải khác mật khẩu cũ
7. ✅ **Account Status Check**: Kiểm tra tài khoản có bị vô hiệu hóa không

---

## 🧪 Testing

### Test với Swagger UI

1. Mở `https://localhost:5001/swagger`
2. Tìm endpoint `POST /api/auth/forgot-password`
3. Nhập email và gửi request
4. Kiểm tra email hoặc response (trong development mode)
5. Sử dụng OTP để test `POST /api/auth/reset-password`

### Test với curl

```bash
# Bước 1: Gửi yêu cầu quên mật khẩu
curl -X POST https://localhost:5001/api/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com"}'

# Bước 2: Đặt lại mật khẩu (thay OTP_CODE bằng mã OTP thực tế)
curl -X POST https://localhost:5001/api/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{
    "email":"user@example.com",
    "otpCode":"123456",
    "newPassword":"NewPassword123",
    "confirmNewPassword":"NewPassword123"
  }'
```

---

## 📊 Flow Diagram

```
┌─────────────┐
│   User      │
│  Quên MK    │
└──────┬──────┘
       │
       ▼
┌─────────────────────────┐
│ POST /forgot-password   │
│ { email }               │
└──────┬──────────────────┘
       │
       ▼
┌─────────────────────────┐
│ 1. Kiểm tra email       │
│ 2. Tạo OTP              │
│ 3. Gửi email            │
└──────┬──────────────────┘
       │
       ▼
┌─────────────────────────┐
│  User nhận OTP          │
│  qua email              │
└──────┬──────────────────┘
       │
       ▼
┌─────────────────────────┐
│ POST /reset-password    │
│ { email, otpCode,       │
│   newPassword,          │
│   confirmPassword }     │
└──────┬──────────────────┘
       │
       ▼
┌─────────────────────────┐
│ 1. Xác thực OTP         │
│ 2. Validate password    │
│ 3. Hash & Update        │
│ 4. Invalidate OTP        │
└──────┬──────────────────┘
       │
       ▼
┌─────────────────────────┐
│  Success!               │
│  User đăng nhập với     │
│  mật khẩu mới           │
└─────────────────────────┘
```

---

## ⚠️ Lưu Ý

1. **Email Service**: Đảm bảo đã cấu hình Email settings trong `appsettings.json`
2. **OTP Expiration**: OTP chỉ có hiệu lực 10 phút, sau đó phải yêu cầu OTP mới
3. **Rate Limiting**: Chỉ có thể yêu cầu OTP 1 lần mỗi phút
4. **Password Requirements**: Mật khẩu mới phải đáp ứng yêu cầu về độ mạnh
5. **Development Mode**: Trong development, OTP có thể được trả về trong response nếu email service chưa cấu hình

---

## 📚 API Endpoints Summary

| Method | Endpoint | Mô tả | Auth Required |
|--------|----------|-------|---------------|
| POST | `/api/auth/forgot-password` | Gửi OTP đến email | ❌ No |
| POST | `/api/auth/reset-password` | Đặt lại mật khẩu với OTP | ❌ No |
| PUT | `/api/auth/change-password` | Đổi mật khẩu (cần mật khẩu cũ) | ✅ Yes |

---

**Version:** 1.0  
**Last Updated:** 2024-11-13  
**Author:** GiaLai OCOP Team

