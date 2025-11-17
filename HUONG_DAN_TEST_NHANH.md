# ⚡ Hướng Dẫn Test Nhanh - GiaLai OCOP Backend

Hướng dẫn nhanh để chạy và debug tests.

---

## 🚀 Chạy Tests (3 bước)

### 1. Chạy tất cả tests
```bash
dotnet test Tests/GiaLaiOCOP.Api.Tests.csproj
```

### 2. Xem kết quả
```
✅ Passed: 35
❌ Failed: 0
⏭️  Skipped: 0
```

### 3. Nếu có test fail, xem chi tiết
```bash
dotnet test Tests/GiaLaiOCOP.Api.Tests.csproj --verbosity normal
```

---

## 🔍 Debug Test Fail

### Bước 1: Xác định test nào fail
```bash
dotnet test Tests/GiaLaiOCOP.Api.Tests.csproj --verbosity minimal
```

### Bước 2: Chạy test cụ thể
```bash
dotnet test Tests/GiaLaiOCOP.Api.Tests.csproj --filter "FullyQualifiedName~TênTestFail"
```

### Bước 3: Xem output chi tiết
```bash
dotnet test Tests/GiaLaiOCOP.Api.Tests.csproj --filter "FullyQualifiedName~TênTestFail" --verbosity detailed
```

---

## 📊 Xem Coverage

### Cách 1: Terminal (nhanh)
```bash
dotnet test Tests/GiaLaiOCOP.Api.Tests.csproj /p:CollectCoverage=true
```

### Cách 2: HTML Report (chi tiết)
```bash
# 1. Generate coverage
dotnet test Tests/GiaLaiOCOP.Api.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# 2. Generate HTML (cần cài tool)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"Tests/coverage.opencover.xml" -targetdir:"Tests/coverage-report" -reporttypes:Html

# 3. Mở file: Tests/coverage-report/index.html
```

---

## 🎯 Các Lệnh Thường Dùng

### Chạy Unit Tests
```bash
dotnet test Tests/GiaLaiOCOP.Api.Tests.csproj --filter "FullyQualifiedName~Tests" --filter "FullyQualifiedName!~Integration"
```

### Chạy Integration Tests
```bash
dotnet test Tests/GiaLaiOCOP.Api.Tests.csproj --filter "FullyQualifiedName~Integration"
```

### Chạy Tests trong một class
```bash
dotnet test Tests/GiaLaiOCOP.Api.Tests.csproj --filter "FullyQualifiedName~RatingServiceTests"
```

### Chạy một test cụ thể
```bash
dotnet test Tests/GiaLaiOCOP.Api.Tests.csproj --filter "FullyQualifiedName~Login_WithValidCredentials_ShouldReturnToken"
```

---

## 🐛 Troubleshooting

### Lỗi: "Database provider conflict"
**Giải pháp:** Đã được sửa tự động. Nếu vẫn gặp, chạy:
```bash
dotnet clean
dotnet build
dotnet test
```

### Lỗi: "Test không tìm thấy"
**Giải pháp:** Đảm bảo đang ở thư mục gốc:
```bash
cd D:\GiaLai-OCOP-BE
dotnet test Tests/GiaLaiOCOP.Api.Tests.csproj
```

### Test pass khi chạy riêng nhưng fail khi chạy tất cả
**Nguyên nhân:** Database isolation issue
**Giải pháp:** Đã được sửa. Mỗi test class có database riêng.

---

## ✅ Checklist Trước Khi Commit

- [ ] Tất cả tests pass: `dotnet test`
- [ ] Không có test fail
- [ ] Coverage không giảm (nếu có)
- [ ] Code được format đúng

---

## 📚 Tài Liệu Đầy Đủ

Xem file `HUONG_DAN_TESTING.md` để biết chi tiết về:
- Cách viết tests mới
- Test templates
- Best practices
- Cấu trúc tests

---

**Last Updated:** 2024-11-17

