# 📋 TỔNG QUAN DỰ ÁN GiaLai OCOP - Backend API

## 📌 Thông Tin Chung

| Thuộc tính | Giá trị |
|------------|---------|
| **Tên dự án** | GiaLai OCOP Backend API |
| **Phiên bản .NET** | .NET 9.0 |
| **Loại dự án** | ASP.NET Core Web API |
| **Database** | PostgreSQL (Supabase) |
| **ORM** | Entity Framework Core 9.0 |
| **Authentication** | JWT Bearer Token |
| **Image Storage** | Cloudinary |
| **Email Service** | SendGrid |
| **Deployment** | Render |

---

## 🎯 Mục Đích Dự Án

Hệ thống quản lý sản phẩm OCOP (Chương trình Mỗi xã Một sản phẩm) tỉnh Gia Lai, bao gồm:
- Quản lý doanh nghiệp và sản phẩm OCOP
- Hệ thống đặt hàng và thanh toán
- Ví điện tử cho người dùng
- Bản đồ doanh nghiệp OCOP
- Đăng ký doanh nghiệp OCOP mới

---

## 👥 Các Vai Trò Người Dùng (Roles)

| Role | Mô tả | Quyền hạn chính |
|------|-------|-----------------|
| **SystemAdmin** | Quản trị viên hệ thống | Toàn quyền quản lý hệ thống |
| **EnterpriseAdmin** | Quản trị viên doanh nghiệp | Quản lý sản phẩm, đơn hàng của doanh nghiệp |
| **Customer** | Khách hàng | Đặt hàng, đánh giá sản phẩm |

---

## 🏗️ Kiến Trúc Dự Án

```
GiaLai-OCOP-BE/
├── Controllers/              # 30 API Controllers
├── Data/                     # DbContext và Migrations
│   ├── AppDbContext.cs       # Database context chính
│   ├── MapSeedData.cs        # Seed data cho bản đồ
│   └── Migrations/           # EF Core migrations
├── Models/                   # 28 Entity models
├── Dtos/                     # 77 Data Transfer Objects
├── Services/                 # 20 Services (Business Logic)
├── Middleware/               # Custom middleware
│   └── GlobalExceptionHandlerMiddleware.cs
├── Options/                  # Configuration options
│   ├── BankTransferSettings.cs
│   └── CloudinarySettings.cs
├── Scripts/                  # Database scripts
├── Tests/                    # Unit & Integration tests
├── uploads/                  # Static files (images, documents)
├── Program.cs                # Application entry point
├── appsettings.json          # Configuration
└── Dockerfile                # Docker configuration
```

---

## 📊 Database Models (28 Entities)

### Core Entities

| Model | Mô tả |
|-------|-------|
| `User` | Người dùng (Customer, EnterpriseAdmin, SystemAdmin) |
| `Enterprise` | Doanh nghiệp OCOP |
| `Product` | Sản phẩm OCOP |
| `Category` | Danh mục sản phẩm |
| `Order` | Đơn hàng |
| `OrderItem` | Chi tiết đơn hàng |
| `Payment` | Thanh toán |

### User & Authentication

| Model | Mô tả |
|-------|-------|
| `User` | Thông tin người dùng |
| `EmailVerification` | Xác thực email OTP |
| `ShippingAddress` | Địa chỉ giao hàng |

### Enterprise & Product

| Model | Mô tả |
|-------|-------|
| `Enterprise` | Doanh nghiệp OCOP |
| `EnterpriseApplication` | Đơn đăng ký doanh nghiệp OCOP |
| `EnterpriseBankInfo` | Thông tin ngân hàng doanh nghiệp |
| `EnterpriseSettings` | Cài đặt doanh nghiệp |
| `Product` | Sản phẩm |
| `Category` | Danh mục |
| `Image` | Hình ảnh |
| `Review` | Đánh giá sản phẩm |
| `Producer` | Nhà sản xuất |

### Order & Payment

| Model | Mô tả |
|-------|-------|
| `Order` | Đơn hàng |
| `OrderItem` | Chi tiết đơn hàng |
| `OrderEnterpriseStatus` | Trạng thái đơn hàng theo doanh nghiệp |
| `Payment` | Thanh toán |
| `Transaction` | Giao dịch |

### Wallet System

| Model | Mô tả |
|-------|-------|
| `Wallet` | Ví điện tử |
| `WalletTransaction` | Giao dịch ví |
| `WalletRequest` | Yêu cầu nạp/rút tiền |
| `BankAccount` | Tài khoản ngân hàng người dùng |

### Location & Map

| Model | Mô tả |
|-------|-------|
| `Location` | Địa điểm |
| `Province` | Tỉnh/Thành phố |
| `District` | Quận/Huyện |
| `Ward` | Phường/Xã |

### Other

| Model | Mô tả |
|-------|-------|
| `Notification` | Thông báo |
| `InventoryHistory` | Lịch sử kho |

---

## 🔧 Services (20 Services)

### Authentication & User

| Service | Interface | Mô tả |
|---------|-----------|-------|
| `EmailService` | `IEmailService` | Gửi email OTP |
| `SocialAuthService` | `ISocialAuthService` | Đăng nhập Google/Facebook |
| `TokenService` | `ITokenService` | Tạo JWT token |

### Payment & Wallet

| Service | Interface | Mô tả |
|---------|-----------|-------|
| `WalletService` | `IWalletService` | Quản lý ví điện tử |
| `WalletRequestService` | `IWalletRequestService` | Xử lý yêu cầu nạp/rút tiền |
| `VietQrService` | `IVietQrService` | Tạo QR code VietQR |
| `VietQRPaymentService` | `IVietQRPaymentService` | Thanh toán VietQR |
| `BankAccountService` | `IBankAccountService` | Quản lý tài khoản ngân hàng |

### Product & Rating

| Service | Interface | Mô tả |
|---------|-----------|-------|
| `RatingService` | `IRatingService` | Cập nhật đánh giá sản phẩm |
| `CloudinaryService` | `ICloudinaryService` | Upload hình ảnh Cloudinary |

### Location

| Service | Interface | Mô tả |
|---------|-----------|-------|
| `GpsAddressService` | `IGpsAddressService` | Tra cứu địa chỉ từ GPS |

---

## 🔐 Authentication & Authorization

### JWT Configuration

```json
{
  "Jwt": {
    "Key": "SECRET_KEY",
    "Issuer": "GiaLaiOCOP",
    "Audience": "GiaLaiOCOPUsers",
    "TokenLifetimeMinutes": 60
  }
}
```

### Claims trong JWT Token

| Claim | Mô tả |
|-------|-------|
| `sub` | Email người dùng |
| `ClaimTypes.NameIdentifier` | User ID |
| `ClaimTypes.Name` | Tên người dùng |
| `ClaimTypes.Role` | Vai trò (Customer, EnterpriseAdmin, SystemAdmin) |

---

## 💳 Phương Thức Thanh Toán

| Phương thức | Mô tả |
|-------------|-------|
| **COD** | Thanh toán khi nhận hàng |
| **BankTransfer** | Chuyển khoản ngân hàng (VietQR) |
| **Wallet** | Thanh toán bằng ví điện tử |

### Trạng Thái Thanh Toán

| Status | Mô tả |
|--------|-------|
| `Pending` | Chờ thanh toán |
| `AwaitingTransfer` | Chờ chuyển khoản |
| `BankTransferConfirmed` | Đã xác nhận chuyển khoản |
| `BankTransferRejected` | Từ chối chuyển khoản |
| `Paid` | Đã thanh toán |
| `PartiallyPaid` | Thanh toán một phần |
| `Cancelled` | Đã hủy |

---

## 📦 Trạng Thái Đơn Hàng

| Status | Mô tả |
|--------|-------|
| `Pending` | Chờ xử lý |
| `Processing` | Đang xử lý |
| `Shipped` | Đang giao hàng |
| `PendingCompletion` | Chờ xác nhận hoàn thành |
| `Completed` | Hoàn thành |
| `Cancelled` | Đã hủy |

---

## 🏷️ Trạng Thái Sản Phẩm

| Status | Mô tả |
|--------|-------|
| `PendingApproval` | Chờ duyệt |
| `Approved` | Đã duyệt |
| `Rejected` | Bị từ chối |

---

## 📁 Third-Party Integrations

### Cloudinary (Image Storage)

```json
{
  "Cloudinary": {
    "CloudName": "xxx",
    "ApiKey": "xxx",
    "ApiSecret": "xxx",
    "DefaultFolder": "GiaLaiOCOP/Images"
  }
}
```

### SendGrid (Email)

```json
{
  "Email": {
    "SendGridApiKey": "xxx",
    "FromEmail": "xxx@gmail.com",
    "FromName": "GiaLai OCOP"
  }
}
```

### Google OAuth

```json
{
  "Google": {
    "ClientId": "xxx.apps.googleusercontent.com",
    "ClientSecret": "xxx"
  }
}
```

### Facebook OAuth

```json
{
  "Facebook": {
    "AppId": "xxx",
    "AppSecret": "xxx"
  }
}
```

### VietQR (Bank Transfer)

```json
{
  "BankTransfer": {
    "BankCode": "970422",
    "AccountNumber": "xxx",
    "AccountName": "xxx",
    "Template": "compact",
    "BaseUrl": "https://img.vietqr.io/image"
  }
}
```

---

## 🗺️ API Endpoints Overview

| Controller | Base Route | Số Endpoints |
|------------|------------|--------------|
| AuthController | `/api/auth` | 13 |
| ProductsController | `/api/products` | 6 |
| OrdersController | `/api/orders` | 9 |
| UsersController | `/api/users` | 8 |
| EnterprisesController | `/api/enterprises` | 7 |
| CategoriesController | `/api/categories` | 5 |
| WalletController | `/api/wallet` | 9 |
| PaymentsController | `/api/payments` | 5 |
| NotificationsController | `/api/notifications` | 4 |
| ReviewsController | `/api/reviews` | 5 |
| MapController | `/api/map` | 7 |
| LocationsController | `/api/locations` | 6 |
| AddressController | `/api/address` | 3 |
| ShippingAddressesController | `/api/shipping-addresses` | 6 |
| FileUploadController | `/api/fileupload` | 3 |
| ProfileController | `/api/profile` | 4 |
| InventoryController | `/api/inventory` | 2 |
| ReportsController | `/api/reports` | 3 |
| WalletRequestController | `/api/walletrequest` | 5 |
| BankAccountController | `/api/bankaccount` | 6 |
| EnterpriseApplicationsController | `/api/enterpriseapplications` | 4 |
| EnterpriseBankInfoController | `/api/enterprise-bank-info` | 4 |
| **Tổng cộng** | | **~120 endpoints** |

---

## 🚀 Deployment

### Production (Render)

- **URL**: https://gialai-ocop-be.onrender.com
- **Swagger**: https://gialai-ocop-be.onrender.com/swagger
- **Health Check**: https://gialai-ocop-be.onrender.com/health

### Local Development

```bash
# Chạy backend local
dotnet run

# Hoặc dùng script
run-local.bat
```

- **Swagger UI**: http://localhost:5003/swagger
- **Health Check**: http://localhost:5003/health
- **API Base**: http://localhost:5003/api

---

## 🔒 CORS Configuration

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:3001",
      "https://gialai-ocop-frontend-2.onrender.com"
    ]
  }
}
```

---

## 📝 Test Account

| Role | Email | Password |
|------|-------|----------|
| SystemAdmin | admin@system.com | 123456 |

---

## 📦 NuGet Packages

| Package | Version | Mục đích |
|---------|---------|----------|
| `BCrypt.Net-Next` | 4.0.3 | Hash password |
| `CloudinaryDotNet` | 1.27.0 | Upload ảnh |
| `SendGrid` | 9.29.3 | Gửi email |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.0.9 | JWT Auth |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 9.0.4 | PostgreSQL |
| `QRCoder` | 1.7.0 | Tạo QR code |
| `Swashbuckle.AspNetCore` | 9.0.5 | Swagger |
| `Microsoft.EntityFrameworkCore.Tools` | 9.0.9 | EF Core tools |

---

## 🧪 Testing

```
Tests/
├── Controllers/           # Controller unit tests
├── Services/              # Service unit tests
├── Integration/           # Integration tests
└── Helpers/               # Test helpers
```

---

## 📄 Tài Liệu Bổ Sung

- [API_ENDPOINTS.md](./API_ENDPOINTS.md) - Chi tiết tất cả API endpoints
- [customer-purchase-flow.md](./customer-purchase-flow.md) - Luồng mua hàng
- [system-architecture.svg](./system-architecture.svg) - Kiến trúc hệ thống
- [package-diagram-backend.svg](./package-diagram-backend.svg) - Package diagram

---

**Developed with ❤️ for GiaLai OCOP**

