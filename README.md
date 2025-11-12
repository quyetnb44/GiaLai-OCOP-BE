# 🏢 GiaLai OCOP Backend API

Backend API cho hệ thống quản lý sản phẩm OCOP (One Commune One Product) tỉnh Gia Lai. Hệ thống hỗ trợ quản lý doanh nghiệp, sản phẩm, đơn hàng, thanh toán và bản đồ doanh nghiệp.

## 📋 Mục lục

- [Tính năng chính](#-tính-năng-chính)
- [Yêu cầu hệ thống](#-yêu-cầu-hệ-thống)
- [Cài đặt](#-cài-đặt)
- [Cấu hình](#-cấu-hình)
- [Chạy ứng dụng](#-chạy-ứng-dụng)
- [Cấu trúc dự án](#-cấu-trúc-dự-án)
- [API Endpoints](#-api-endpoints)
- [Authentication](#-authentication)
- [Testing](#-testing)
- [Deployment](#-deployment)
- [Tài liệu tham khảo](#-tài-liệu-tham-khảo)

---

## ✨ Tính năng chính

### 🔐 Authentication & Authorization
- Đăng ký/Đăng nhập với JWT
- Phân quyền: Customer, EnterpriseAdmin, SystemAdmin
- Bảo mật mật khẩu với BCrypt

### 🛒 Quản lý đơn hàng
- Tạo đơn hàng (Customer)
- Quản lý trạng thái đơn hàng (EnterpriseAdmin)
- Hủy đơn hàng (Customer)
- Xem lịch sử đơn hàng

### 💳 Hệ thống thanh toán
- **COD (Cash on Delivery)** - Thanh toán khi nhận hàng
- **BankTransfer** - Chuyển khoản qua QR code (VietQR)
- Payment riêng cho mỗi Enterprise trong đơn hàng
- Tự động tạo QR code cho từng Enterprise
- Xác nhận thanh toán (EnterpriseAdmin/SystemAdmin)

### 📍 Map API
- Tìm kiếm doanh nghiệp theo từ khóa
- Tìm theo khu vực bản đồ (Bounding Box)
- Tìm theo tọa độ và bán kính
- Lọc doanh nghiệp theo nhiều điều kiện
- Tính khoảng cách tự động
- Google Maps directions integration

### 🏭 Quản lý doanh nghiệp
- Đăng ký doanh nghiệp (Enterprise Application)
- Quản lý thông tin doanh nghiệp
- Cấu hình thông tin ngân hàng riêng cho từng Enterprise
- OCOP rating (3-5 sao)

### 📦 Quản lý sản phẩm
- CRUD sản phẩm
- Quản lý tồn kho
- Đánh giá sản phẩm (Reviews)
- Tìm kiếm và lọc sản phẩm

---

## 🛠 Yêu cầu hệ thống

### Bắt buộc
- **.NET SDK 9.0** hoặc cao hơn
- **PostgreSQL 12+** hoặc database tương thích
- **Git** để clone repository

### Tùy chọn (cho development)
- **Docker** và **Docker Compose** (cho containerization)
- **Postman** hoặc **Swagger UI** (để test API)
- **Visual Studio 2022** hoặc **VS Code** với C# extension

---

## 📦 Cài đặt

### 1. Clone repository

```bash
git clone <repository-url>
cd GiaLai-OCOP-BE
```

### 2. Cài đặt .NET SDK

Kiểm tra phiên bản .NET:
```bash
dotnet --version
```

Nếu chưa có, tải về từ: https://dotnet.microsoft.com/download

### 3. Cài đặt PostgreSQL

**Windows:**
- Tải PostgreSQL từ: https://www.postgresql.org/download/windows/
- Hoặc sử dụng Docker: `docker run -e POSTGRES_PASSWORD=password -p 5432:5432 postgres`

**macOS:**
```bash
brew install postgresql
brew services start postgresql
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt-get update
sudo apt-get install postgresql postgresql-contrib
```

### 4. Restore dependencies

```bash
dotnet restore
```

---

## ⚙️ Cấu hình

### 1. Cấu hình Database

Tạo file `appsettings.Development.json` (hoặc chỉnh sửa `appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=GiaLaiOCOP;Username=postgres;Password=your_password;SSL Mode=Prefer"
  },
  "Jwt": {
    "Key": "your-super-secret-key-min-32-characters-long",
    "Issuer": "GiaLaiOCOP",
    "Audience": "GiaLaiOCOPUsers",
    "TokenLifetimeMinutes": 60
  },
  "BankTransfer": {
    "BankCode": "970415",
    "AccountNumber": "123456789",
    "AccountName": "OCOP GIA LAI",
    "Template": "compact",
    "BaseUrl": "https://img.vietqr.io/image",
    "Description": "Thanh toan don hang OCOP"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 2. Tạo Database

```bash
# Tạo database PostgreSQL
createdb GiaLaiOCOP

# Hoặc sử dụng psql
psql -U postgres
CREATE DATABASE GiaLaiOCOP;
\q
```

### 3. Chạy Migrations

```bash
# Tạo migration (nếu cần)
dotnet ef migrations add InitialCreate

# Áp dụng migrations vào database
dotnet ef database update
```

**Lưu ý:** Nếu chưa cài đặt EF Core Tools:
```bash
dotnet tool install --global dotnet-ef
```

---

## 🚀 Chạy ứng dụng

### Development Mode

```bash
# Chạy ứng dụng
dotnet run

# Hoặc với hot reload
dotnet watch run
```

Ứng dụng sẽ chạy tại: `https://localhost:5001` hoặc `http://localhost:5000`

### Swagger UI

Mở trình duyệt và truy cập:
- **Swagger UI:** `https://localhost:5001/swagger`
- **Swagger JSON:** `https://localhost:5001/swagger/v1/swagger.json`

### Docker

```bash
# Build Docker image
docker build -t gialai-ocop-api .

# Chạy container
docker run -p 5000:80 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=GiaLaiOCOP;Username=postgres;Password=password" \
  gialai-ocop-api
```

### Docker Compose (nếu có)

```bash
docker-compose up -d
```

---

## 📁 Cấu trúc dự án

```
GiaLai-OCOP-BE/
├── Controllers/          # API Controllers
│   ├── AuthController.cs
│   ├── OrdersController.cs
│   ├── PaymentsController.cs
│   ├── ProductsController.cs
│   ├── EnterprisesController.cs
│   ├── MapController.cs
│   └── ...
├── Data/                 # Database context và migrations
│   ├── AppDbContext.cs
│   ├── MapSeedData.cs
│   └── Migrations/
├── Dtos/                 # Data Transfer Objects
│   ├── OrderDto.cs
│   ├── PaymentDto.cs
│   ├── ProductDto.cs
│   └── ...
├── Models/               # Entity models
│   ├── User.cs
│   ├── Order.cs
│   ├── Payment.cs
│   ├── Product.cs
│   └── ...
├── Services/            # Business logic services
│   ├── TokenService.cs
│   └── ...
├── Options/             # Configuration options
│   └── BankTransferSettings.cs
├── Program.cs           # Application entry point
├── appsettings.json     # Configuration file
├── Dockerfile           # Docker configuration
└── README.md           # This file
```

---

## 🔌 API Endpoints

### Authentication

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/auth/register` | Đăng ký tài khoản mới | Public |
| POST | `/api/auth/login` | Đăng nhập, nhận JWT token | Public |

### Products

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/products` | Danh sách sản phẩm | Public |
| GET | `/api/products/{id}` | Chi tiết sản phẩm | Public |
| POST | `/api/products` | Tạo sản phẩm mới | EnterpriseAdmin |
| PUT | `/api/products/{id}` | Cập nhật sản phẩm | EnterpriseAdmin |
| DELETE | `/api/products/{id}` | Xóa sản phẩm | EnterpriseAdmin |

### Orders

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/orders` | Danh sách đơn hàng | Customer/EnterpriseAdmin/SystemAdmin |
| GET | `/api/orders/{id}` | Chi tiết đơn hàng | Customer/EnterpriseAdmin/SystemAdmin |
| POST | `/api/orders` | Tạo đơn hàng mới | Customer |
| PUT | `/api/orders/{id}/status` | Cập nhật trạng thái đơn hàng | Customer/EnterpriseAdmin/SystemAdmin |
| DELETE | `/api/orders/{id}` | Xóa đơn hàng | Customer/EnterpriseAdmin |

### Payments

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/payments` | Tạo thanh toán cho đơn hàng | Customer |
| GET | `/api/payments/{id}` | Chi tiết thanh toán | Customer/EnterpriseAdmin/SystemAdmin |
| GET | `/api/payments/order/{orderId}` | Danh sách payments của đơn hàng | Customer/EnterpriseAdmin/SystemAdmin |
| POST | `/api/payments/{id}/status` | Xác nhận thanh toán | EnterpriseAdmin/SystemAdmin |

### Map

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/map/search` | Tìm kiếm doanh nghiệp | Public |
| GET | `/api/map/bounding-box` | Tìm theo khu vực bản đồ | Public |
| GET | `/api/map/nearby` | Tìm theo tọa độ và bán kính | Public |
| GET | `/api/map/filter` | Lọc doanh nghiệp | Public |
| GET | `/api/map/enterprises/{id}` | Chi tiết doanh nghiệp | Public |
| GET | `/api/map/enterprises/{id}/products` | Sản phẩm của doanh nghiệp | Public |
| GET | `/api/map/filter-options` | Options cho filter | Public |

### Enterprises

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/enterprises` | Danh sách doanh nghiệp | Public/EnterpriseAdmin |
| GET | `/api/enterprises/{id}` | Chi tiết doanh nghiệp | Public/EnterpriseAdmin |
| POST | `/api/enterprises` | Tạo doanh nghiệp mới | SystemAdmin |
| PUT | `/api/enterprises/{id}` | Cập nhật doanh nghiệp | EnterpriseAdmin/SystemAdmin |

### Users

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/users` | Danh sách users | SystemAdmin |
| GET | `/api/users/{id}` | Chi tiết user | SystemAdmin/User (chính mình) |

**Lưu ý:** Xem chi tiết API trong Swagger UI hoặc các file documentation:
- `PAYMENT_API_DOCUMENTATION.md`
- `MAP_API_DOCUMENTATION.md`
- `ENTERPRISE_ADMIN_ORDER_MANAGEMENT.md`

---

## 🔐 Authentication

### 1. Đăng ký

```http
POST /api/auth/register
Content-Type: application/json

{
  "name": "Nguyễn Văn A",
  "email": "user@example.com",
  "password": "password123"
}
```

### 2. Đăng nhập

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": "2024-11-12T10:30:00Z"
}
```

### 3. Sử dụng Token

Thêm token vào header của mọi request:

```http
Authorization: Bearer {token}
```

**Ví dụ với curl:**
```bash
curl -H "Authorization: Bearer {token}" \
     https://localhost:5001/api/orders
```

**Ví dụ với JavaScript:**
```javascript
fetch('https://localhost:5001/api/orders', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
})
```

### 4. Roles

- **Customer:** Khách hàng, có thể đặt hàng và xem đơn hàng của mình
- **EnterpriseAdmin:** Quản trị doanh nghiệp, quản lý sản phẩm và đơn hàng của doanh nghiệp mình
- **SystemAdmin:** Quản trị hệ thống, toàn quyền

---

## 🧪 Testing

### Test với Swagger UI

1. Mở `https://localhost:5001/swagger`
2. Click "Authorize" và nhập token (nếu cần)
3. Test các endpoints

### Test với Postman

1. Import collection từ Swagger JSON
2. Set environment variables:
   - `base_url`: `https://localhost:5001`
   - `token`: JWT token từ login

### Test với curl

```bash
# Đăng nhập
TOKEN=$(curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password123"}' \
  | jq -r '.token')

# Sử dụng token
curl -H "Authorization: Bearer $TOKEN" \
     https://localhost:5001/api/orders
```

### Unit Tests (Chưa có)

Dự án hiện chưa có unit tests. Khuyến nghị thêm test project:
```bash
dotnet new xunit -n GiaLaiOCOP.Api.Tests
dotnet add reference ../GiaLaiOCOP.Api/GiaLaiOCOP.Api.csproj
```

---

## 🚢 Deployment

### Production Checklist

- [ ] Thay đổi JWT Key thành secret mạnh
- [ ] Cấu hình CORS cho production domains
- [ ] Sử dụng environment variables cho secrets
- [ ] Bật HTTPS
- [ ] Cấu hình logging
- [ ] Setup database backup
- [ ] Thêm health checks
- [ ] Setup monitoring

### Environment Variables

Sử dụng environment variables cho production:

```bash
export ConnectionStrings__DefaultConnection="Host=..."
export Jwt__Key="your-production-secret-key"
export Jwt__Issuer="GiaLaiOCOP"
export Jwt__Audience="GiaLaiOCOPUsers"
```

### Docker Production

```bash
# Build
docker build -t gialai-ocop-api:latest .

# Run với environment variables
docker run -d \
  -p 80:80 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Jwt__Key="..." \
  --name gialai-ocop-api \
  gialai-ocop-api:latest
```

### Azure App Service

1. Tạo App Service
2. Deploy từ Git hoặc Docker
3. Cấu hình Connection Strings trong App Settings
4. Enable HTTPS

### AWS / GCP

Tương tự, deploy lên EC2, ECS, hoặc Cloud Run với Docker.

---

## 📚 Tài liệu tham khảo

### Documentation Files

- `PAYMENT_API_DOCUMENTATION.md` - Chi tiết Payment API
- `MAP_API_DOCUMENTATION.md` - Chi tiết Map API
- `ENTERPRISE_ADMIN_ORDER_MANAGEMENT.md` - Quản lý đơn hàng cho EnterpriseAdmin
- `GIAI_THICH_PAYMENT_ENDPOINTS.md` - Giải thích Payment Endpoints
- `PHAN_TICH_LOGIC_DU_AN.md` - Phân tích logic dự án
- `XAC_NHAN_LUONG_DON_HANG.md` - Xác nhận luồng đơn hàng
- `BAO_CAO_THIEU_SOT.md` - Báo cáo những gì còn thiếu

### External Resources

- [.NET 9.0 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [JWT Authentication](https://jwt.io/)
- [VietQR Documentation](https://vietqr.io/)

---

## 🤝 Đóng góp

1. Fork repository
2. Tạo feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Tạo Pull Request

---

## 📝 License

Dự án này thuộc về tỉnh Gia Lai.

---

## 👥 Liên hệ

Nếu có câu hỏi hoặc vấn đề, vui lòng tạo issue trên repository.

---

## 🎯 Roadmap

- [ ] Thêm Unit Tests
- [ ] Error Handling Middleware
- [ ] API Versioning
- [ ] Rate Limiting
- [ ] Health Checks
- [ ] Background Jobs
- [ ] Email Notifications
- [ ] File Upload API

---

**Version:** 1.0  
**Last Updated:** 2024-11-12

