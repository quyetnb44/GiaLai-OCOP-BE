# 🏢 GiaLai OCOP - Backend API

Backend API cho hệ thống quản lý sản phẩm OCOP Gia Lai.

## 🚀 Quick Start

### Chạy Backend Local

**Cách 1: Dùng Script (Khuyến nghị)**

```bash
# Windows
run-local.bat

# Hoặc PowerShell
.\run-local.bat
```

**Cách 2: Manual**

```bash
cd C:\Users\Admin\Desktop\SEP490\GiaLai-OCOP-BE
dotnet restore
dotnet run
```

### Kiểm Tra Backend

Sau khi chạy, truy cập:

- 📚 **Swagger UI:** http://localhost:5003/swagger
- ✅ **Health Check:** http://localhost:5003/health
- 🌐 **API Base:** http://localhost:5003/api

---

## 📋 Tech Stack

- **.NET Core 9.0** - Framework
- **PostgreSQL** - Database (Supabase)
- **Entity Framework Core** - ORM
- **JWT** - Authentication
- **Swagger** - API Documentation
- **Cloudinary** - Image storage

---

## 🔧 Configuration

### CORS

Backend đã được cấu hình để cho phép frontend local:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",      // Frontend local
      "https://gialai-ocop-frontend-2.onrender.com"  // Production
    ]
  }
}
```

### Database

Hiện đang sử dụng **Supabase PostgreSQL** (cloud):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=5432;..."
  }
}
```

### Environment Variables

Các biến môi trường quan trọng trong `appsettings.json`:

- `Jwt:Key` - JWT secret key
- `Google:ClientId` - Google OAuth Client ID
- `Facebook:AppId` - Facebook App ID
- `Email:SmtpUsername` - Email SMTP credentials
- `Cloudinary:CloudName` - Cloudinary config

---

## 📁 Project Structure

```
GiaLai-OCOP-BE/
├── Controllers/          # API Controllers
├── Data/                 # DbContext và Migrations
├── Models/               # Entity models
├── Services/             # Business logic
├── Dtos/                 # Data Transfer Objects
├── Middleware/           # Custom middleware
├── Options/              # Configuration options
├── Scripts/              # Database scripts
├── uploads/              # Static files
├── appsettings.json      # Configuration
├── Program.cs            # Application entry point
└── run-local.bat         # Quick start script
```

---

## 🔐 Authentication

Backend sử dụng **JWT Bearer Token** cho authentication.

### Endpoints

- `POST /api/auth/login` - Đăng nhập
- `POST /api/auth/register` - Đăng ký
- `POST /api/auth/google` - Google OAuth
- `POST /api/auth/facebook` - Facebook OAuth

### Test Account

```
Email: admin@system.com
Password: 123456
Role: SystemAdmin
```

---

## 📚 API Documentation

Sau khi chạy backend, truy cập Swagger UI để xem đầy đủ API documentation:

👉 http://localhost:5003/swagger

### Main Endpoints

- `/api/products` - Quản lý sản phẩm
- `/api/categories` - Quản lý danh mục
- `/api/enterprises` - Quản lý doanh nghiệp
- `/api/orders` - Quản lý đơn hàng
- `/api/users` - Quản lý người dùng
- `/api/ratings` - Đánh giá sản phẩm
- `/api/cart` - Giỏ hàng
- `/api/wallet` - Ví điện tử

---

## 🐛 Troubleshooting

### Port Already in Use

```bash
# Kiểm tra process đang dùng port 5003
netstat -ano | findstr :5003

# Kill process
taskkill /PID <PID> /F
```

### CORS Errors

Kiểm tra console log khi backend khởi động:

```
🔹 CORS Allowed Origins: http://localhost:3000, ...
```

Nếu không thấy, kiểm tra `appsettings.json` → `Cors:AllowedOrigins`

### Database Connection Errors

Backend đang dùng Supabase PostgreSQL (cloud), không cần PostgreSQL local.

---

## 📖 Documentation

- [LOCAL_SETUP.md](./LOCAL_SETUP.md) - Hướng dẫn setup chi tiết
- [Swagger UI](http://localhost:5003/swagger) - API documentation

---

## 🚀 Deployment

Backend hiện đang deploy trên **Render**:

- Production URL: https://gialai-ocop-be.onrender.com
- Swagger: https://gialai-ocop-be.onrender.com/swagger

---

## 📝 Notes

- Backend tự động tạo SystemAdmin account khi khởi động lần đầu
- CORS đã được cấu hình cho localhost:3000 (frontend local)
- Database migrations tự động chạy khi khởi động
- Static files được serve từ `/uploads` directory

---

**Developed with ❤️ for GiaLai OCOP**
