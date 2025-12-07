# 🚀 Hướng Dẫn Deploy lên Render Production

Tài liệu này hướng dẫn cách deploy ứng dụng ASP.NET Core lên Render với cấu hình tối ưu cho Production.

---

## ✅ Các Cải Tiến Đã Thực Hiện

### 1. Tắt reloadOnChange
- ✅ Tất cả `AddJsonFile` đều có `reloadOnChange: false`
- ✅ Tránh inotify limit trên Linux (gây lỗi status 139)

### 2. Chỉ Load Config Files Cần Thiết
- ✅ Production: Chỉ load `appsettings.json` và `appsettings.Production.json`
- ✅ Không load `appsettings.Development.json` hoặc `appsettings.Local.json`

### 3. Environment Variables
- ✅ Mặc định `ASPNETCORE_ENVIRONMENT = Production`
- ✅ Environment variables override file config (quan trọng cho Render)

### 4. Loại Bỏ FileSystemWatcher
- ✅ PhysicalFileProvider không tự động watch files
- ✅ StaticFileMiddleware không sử dụng Watch() nên không gây inotify limit

---

## 📋 Cấu Hình Render

### 1. Tạo Web Service trên Render

1. Đăng nhập vào [Render Dashboard](https://dashboard.render.com)
2. Click **New +** → **Web Service**
3. Connect repository GitHub của bạn
4. Cấu hình như sau:

**Name:** `gialai-ocop-api` (hoặc tên bạn muốn)

**Environment:** `Docker` hoặc `Nixpacks`

**Region:** Chọn region gần nhất (ví dụ: `Singapore`)

**Branch:** `main` (hoặc branch bạn muốn deploy)

**Root Directory:** (để trống nếu root là project root)

**Build Command:**
```bash
dotnet restore && dotnet build -c Release
```

**Start Command:**
```bash
dotnet GiaLaiOCOP.Api.dll --urls http://0.0.0.0:$PORT
```

**Health Check Path:** `/health`

---

### 2. Environment Variables trên Render

Vào **Environment** tab và thêm các variables sau:

#### Bắt Buộc:

```
ASPNETCORE_ENVIRONMENT=Production
```

#### Database Connection:

```
ConnectionStrings__DefaultConnection=Host=your-db-host;Port=5432;Database=your-db;Username=your-user;Password=your-password;SslMode=Require
```

#### JWT Configuration:

```
Jwt__Key=your-super-secret-jwt-key-min-32-characters
Jwt__Issuer=GiaLaiOCOP
Jwt__Audience=GiaLaiOCOPUsers
Jwt__TokenLifetimeMinutes=60
```

#### Google OAuth (nếu dùng):

```
Google__ClientId=your-google-client-id
Google__ClientSecret=your-google-client-secret
```

#### Facebook OAuth (nếu dùng):

```
Facebook__AppId=your-facebook-app-id
Facebook__AppSecret=your-facebook-app-secret
```

#### CORS Configuration:

```
Cors__AllowedOrigins__0=https://your-frontend-domain.com
Cors__AllowedOrigins__1=http://localhost:3000
```

#### Email Configuration (nếu dùng):

```
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__SmtpUsername=your-email@gmail.com
Email__SmtpPassword=your-app-password
Email__FromEmail=your-email@gmail.com
Email__FromName=GiaLai OCOP
```

#### Cloudinary (nếu dùng):

```
Cloudinary__CloudName=your-cloud-name
Cloudinary__ApiKey=your-api-key
Cloudinary__ApiSecret=your-api-secret
Cloudinary__DefaultFolder=GiaLaiOCOP/Images
```

---

### 3. Database trên Render

1. Tạo **PostgreSQL** database trên Render:
   - Click **New +** → **PostgreSQL**
   - Chọn plan phù hợp
   - Copy **Internal Database URL** hoặc **External Database URL**

2. Thêm Internal Database URL vào Environment Variables:
   ```
   ConnectionStrings__DefaultConnection=<Internal Database URL>
   ```

3. Chạy migrations sau khi deploy:
   ```bash
   # SSH vào service hoặc dùng Render Shell
   dotnet ef database update
   ```

---

### 4. Build Settings

Nếu dùng **Docker**, tạo file `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["GiaLaiOCOP.Api.csproj", "./"]
RUN dotnet restore "GiaLaiOCOP.Api.csproj"
COPY . .
RUN dotnet build "GiaLaiOCOP.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GiaLaiOCOP.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GiaLaiOCOP.Api.dll"]
```

Nếu dùng **Nixpacks**, Render sẽ tự động detect và build.

---

## 🔧 Kiểm Tra Sau Khi Deploy

### 1. Health Check

Truy cập: `https://your-service.onrender.com/health`

Kết quả mong đợi:
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy"
  }
}
```

### 2. API Endpoints

Test các endpoints:
- `GET /api/products` - Lấy danh sách sản phẩm
- `POST /api/auth/login` - Đăng nhập
- `GET /swagger` - Swagger UI (chỉ trong Development)

### 3. Logs

Kiểm tra logs trên Render Dashboard:
- Không có lỗi `inotify` hoặc `status 139`
- Không có lỗi `FileSystemWatcher`
- Application start thành công

---

## 🐛 Troubleshooting

### Lỗi: "status 139" hoặc "inotify limit"

**Nguyên nhân:** Config files có `reloadOnChange: true`

**Giải pháp:** 
- ✅ Đã được fix trong code - tất cả `AddJsonFile` đều có `reloadOnChange: false`
- Đảm bảo `ASPNETCORE_ENVIRONMENT=Production` được set

### Lỗi: "Cannot find appsettings.Development.json"

**Nguyên nhân:** Code đang cố load Development file trong Production

**Giải pháp:**
- ✅ Đã được fix - Production chỉ load `appsettings.json` và `appsettings.Production.json`
- Đảm bảo `ASPNETCORE_ENVIRONMENT=Production`

### Lỗi: Database connection failed

**Nguyên nhân:** Connection string không đúng hoặc database chưa sẵn sàng

**Giải pháp:**
1. Kiểm tra Internal Database URL trong Environment Variables
2. Đảm bảo database đã được tạo và running
3. Kiểm tra firewall rules (nếu dùng External URL)

### Lỗi: "JWT key is not configured"

**Nguyên nhân:** `Jwt__Key` chưa được set trong Environment Variables

**Giải pháp:**
- Thêm `Jwt__Key` vào Environment Variables trên Render
- Đảm bảo key có ít nhất 32 ký tự

### Lỗi: CORS policy blocked

**Nguyên nhân:** Frontend domain chưa được thêm vào `Cors__AllowedOrigins`

**Giải pháp:**
- Thêm frontend domain vào Environment Variables:
  ```
  Cors__AllowedOrigins__0=https://your-frontend-domain.com
  ```

---

## 📝 Checklist Deploy

Trước khi deploy, đảm bảo:

- [ ] Đã set `ASPNETCORE_ENVIRONMENT=Production` trong Environment Variables
- [ ] Đã thêm tất cả secrets vào Environment Variables (không dùng appsettings.json)
- [ ] Đã tạo PostgreSQL database trên Render
- [ ] Đã thêm Database Connection String vào Environment Variables
- [ ] Đã cấu hình CORS với frontend domain
- [ ] Đã test build thành công local với `dotnet build -c Release`
- [ ] Đã commit và push code lên GitHub
- [ ] Đã cấu hình Health Check Path: `/health`

Sau khi deploy:

- [ ] Health check endpoint trả về `Healthy`
- [ ] API endpoints hoạt động đúng
- [ ] Không có lỗi trong logs
- [ ] Database migrations đã được chạy
- [ ] Swagger UI không hiển thị (vì Production mode)

---

## 🔒 Bảo Mật

### Best Practices:

1. **Không commit secrets vào Git**
   - Sử dụng Environment Variables trên Render
   - File `appsettings.json` chỉ chứa config mặc định (không có secrets)

2. **Sử dụng Internal Database URL**
   - Render cung cấp Internal URL cho database
   - An toàn hơn External URL (không cần firewall rules)

3. **Rotate Secrets định kỳ**
   - Đổi JWT Key, OAuth secrets định kỳ
   - Update Environment Variables trên Render

4. **Enable HTTPS**
   - Render tự động cung cấp HTTPS
   - Đảm bảo CORS chỉ cho phép HTTPS origins

---

## 📚 Tài Liệu Tham Khảo

- [Render Documentation](https://render.com/docs)
- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Deploy ASP.NET Core to Render](https://render.com/docs/deploy-aspnet-core)

---

**Version:** 1.0  
**Last Updated:** 2024-11-13

