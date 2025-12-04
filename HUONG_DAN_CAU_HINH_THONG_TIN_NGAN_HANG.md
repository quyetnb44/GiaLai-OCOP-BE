# 🏦 Hướng Dẫn Cấu Hình Thông Tin Ngân Hàng Cho QR Code

Tài liệu hướng dẫn chi tiết về cách cấu hình thông tin ngân hàng để tạo QR code thanh toán trong hệ thống GiaLai OCOP.

---

## 📋 Tổng Quan

Hệ thống hỗ trợ **2 cấp độ cấu hình** thông tin ngân hàng:

1. **Global Settings** - Cấu hình chung cho tất cả Enterprise (trong `appsettings.json`)
2. **Enterprise Settings** - Cấu hình riêng cho từng Enterprise (trong Database)

**Ưu tiên:** Enterprise Settings > Global Settings

---

## 🔧 Cấu Hình 1: Global Settings (Cấu Hình Chung)

### Vị Trí File

- **Production:** `appsettings.json`
- **Development:** `appsettings.Development.json`

### Cấu Trúc

```json
{
  "BankTransfer": {
    "BankCode": "970415",                    // Mã ngân hàng
    "AccountNumber": "123456789",            // Số tài khoản
    "AccountName": "OCOP GIA LAI",           // Tên chủ tài khoản
    "Template": "compact",                    // Template QR: "compact" hoặc "print"
    "BaseUrl": "https://img.vietqr.io/image", // Base URL của VietQR API
    "Description": "Thanh toan don hang OCOP" // Mô tả mặc định
  }
}
```

### Cách Cấu Hình

1. **Mở file `appsettings.json`** (hoặc `appsettings.Development.json`)

2. **Tìm section `BankTransfer`** và cập nhật:

```json
{
  "BankTransfer": {
    "BankCode": "970415",                    // ⬅️ Thay bằng mã ngân hàng thực tế
    "AccountNumber": "123456789",            // ⬅️ Thay bằng số tài khoản thực tế
    "AccountName": "OCOP GIA LAI",           // ⬅️ Thay bằng tên chủ tài khoản
    "Template": "compact",                    // "compact" hoặc "print"
    "BaseUrl": "https://img.vietqr.io/image", // Không cần thay đổi
    "Description": "Thanh toan don hang OCOP" // Có thể tùy chỉnh
  }
}
```

3. **Lưu file và restart ứng dụng**

### Danh Sách Mã Ngân Hàng Phổ Biến

| Mã | Tên Ngân Hàng |
|----|---------------|
| 970415 | MB Bank (Military Bank) |
| 970422 | VietinBank |
| 970436 | Vietcombank |
| 970441 | BIDV |
| 970448 | Agribank |
| 970454 | ACB (Asia Commercial Bank) |
| 970458 | Techcombank |
| 970461 | VPBank |
| 970466 | TPBank |
| 970467 | HDBank |

**Lưu ý:** Xem danh sách đầy đủ tại [VietQR Bank List](https://www.vietqr.io/bank-list)

### Khi Nào Dùng Global Settings?

- ✅ Tất cả Enterprise dùng chung một tài khoản ngân hàng
- ✅ Enterprise chưa cấu hình thông tin ngân hàng riêng
- ✅ Cấu hình mặc định cho hệ thống

---

## 🏢 Cấu Hình 2: Enterprise Settings (Cấu Hình Riêng)

### Vị Trí

Thông tin được lưu trong **Database**, bảng `Enterprises` với các cột:
- `BankCode` (string, nullable)
- `BankAccount` (string, nullable)
- `BankAccountName` (string, nullable)

### Cách Cấu Hình

#### Cách 1: Qua SQL (Trực Tiếp)

```sql
UPDATE "Enterprises"
SET 
  "BankCode" = '970415',
  "BankAccount" = '987654321',
  "BankAccountName" = 'DOANH NGHIEP ABC'
WHERE "Id" = 5;
```

#### Cách 2: Qua API (Hiện Chưa Có Endpoint)

**⚠️ Lưu ý:** Hiện tại `UpdateEnterpriseDto` chưa có các trường `BankCode`, `BankAccount`, `BankAccountName`, nên chưa thể cập nhật qua API.

**Giải pháp tạm thời:** Sử dụng SQL hoặc thêm các trường vào DTO.

#### Cách 3: Thêm Vào DTO (Đề Xuất)

Cần thêm vào `UpdateEnterpriseDto.cs`:

```csharp
// Thông tin ngân hàng
[MaxLength(10)]
public string? BankCode { get; set; }

[MaxLength(50)]
public string? BankAccount { get; set; }

[MaxLength(255)]
public string? BankAccountName { get; set; }
```

Và cập nhật logic trong `EnterprisesController.cs`:

```csharp
// Trong UpdateMyEnterprise hoặc UpdateEnterprise
if (dto.BankCode != null)
    enterprise.BankCode = dto.BankCode;
if (dto.BankAccount != null)
    enterprise.BankAccount = dto.BankAccount;
if (dto.BankAccountName != null)
    enterprise.BankAccountName = dto.BankAccountName;
```

### Khi Nào Dùng Enterprise Settings?

- ✅ Mỗi Enterprise có tài khoản ngân hàng riêng
- ✅ Tiền cần chuyển trực tiếp vào tài khoản của từng Enterprise
- ✅ Quản lý tài chính độc lập cho mỗi Enterprise

---

## 🔄 Logic Ưu Tiên

Khi tạo QR code, hệ thống sẽ kiểm tra theo thứ tự:

```
1. Enterprise có thông tin ngân hàng riêng?
   ├─ CÓ → Dùng thông tin của Enterprise
   └─ KHÔNG → Chuyển sang bước 2

2. Global Settings có thông tin?
   ├─ CÓ → Dùng Global Settings
   └─ KHÔNG → Throw Exception (Lỗi)
```

### Code Logic

```csharp
if (!string.IsNullOrWhiteSpace(enterprise.BankCode) &&
    !string.IsNullOrWhiteSpace(enterprise.BankAccount) &&
    !string.IsNullOrWhiteSpace(enterprise.BankAccountName))
{
    // ✅ Dùng thông tin của Enterprise
    bankCode = enterprise.BankCode;
    bankAccount = enterprise.BankAccount;
    accountName = enterprise.BankAccountName;
}
else
{
    // ✅ Dùng Global Settings
    var settings = _bankOptions.Value;
    bankCode = settings.BankCode;
    bankAccount = settings.AccountNumber;
    accountName = settings.AccountName;
}
```

---

## 📊 Ví Dụ Thực Tế

### Scenario 1: Tất Cả Dùng Chung

**Cấu hình:**
- Global Settings: `970415-123456789-OCOP GIA LAI`
- Enterprise A: Chưa cấu hình
- Enterprise B: Chưa cấu hình

**Kết quả:**
- Enterprise A → QR code: `970415-123456789`
- Enterprise B → QR code: `970415-123456789`

### Scenario 2: Mỗi Enterprise Có Tài Khoản Riêng

**Cấu hình:**
- Global Settings: `970415-123456789-OCOP GIA LAI` (fallback)
- Enterprise A: `970415-987654321-DOANH NGHIEP A`
- Enterprise B: `970422-555555555-DOANH NGHIEP B`

**Kết quả:**
- Enterprise A → QR code: `970415-987654321` (dùng riêng)
- Enterprise B → QR code: `970422-555555555` (dùng riêng)

### Scenario 3: Hỗn Hợp

**Cấu hình:**
- Global Settings: `970415-123456789-OCOP GIA LAI`
- Enterprise A: `970415-987654321-DOANH NGHIEP A` (có riêng)
- Enterprise B: Chưa cấu hình

**Kết quả:**
- Enterprise A → QR code: `970415-987654321` (dùng riêng)
- Enterprise B → QR code: `970415-123456789` (dùng Global)

---

## ✅ Kiểm Tra Cấu Hình

### 1. Kiểm Tra Global Settings

```bash
# Xem file appsettings.json
cat appsettings.json | grep -A 7 "BankTransfer"
```

Hoặc mở file và kiểm tra section `BankTransfer`.

### 2. Kiểm Tra Enterprise Settings

```sql
SELECT 
  "Id",
  "Name",
  "BankCode",
  "BankAccount",
  "BankAccountName"
FROM "Enterprises"
WHERE "Id" = 5;
```

### 3. Test Tạo Payment

Tạo một đơn hàng với `paymentMethod = "BankTransfer"` và kiểm tra QR code URL trong response:

```json
{
  "payments": [
    {
      "enterpriseId": 5,
      "bankCode": "970415",
      "bankAccount": "987654321",
      "accountName": "DOANH NGHIEP A",
      "qrCodeUrl": "https://img.vietqr.io/image/970415-987654321-compact.png?..."
    }
  ]
}
```

---

## ⚠️ Lưu Ý Quan Trọng

### 1. Bảo Mật

- ⚠️ **KHÔNG commit** file `appsettings.json` có thông tin ngân hàng thực vào Git
- ✅ Sử dụng `appsettings.Development.json` cho development (đã có trong `.gitignore`)
- ✅ Sử dụng **Environment Variables** hoặc **Azure Key Vault** cho production

### 2. Validation

Hệ thống sẽ throw exception nếu:
- Enterprise chưa cấu hình thông tin ngân hàng
- Global Settings cũng chưa có thông tin

**Error message:**
```
Cấu hình BankTransfer cho Enterprise {Name} (ID: {Id}) chưa được thiết lập đầy đủ.
```

### 3. Cập Nhật

- ✅ Sau khi cập nhật Global Settings → **Restart ứng dụng**
- ✅ Sau khi cập nhật Enterprise Settings → **Không cần restart** (lưu trực tiếp vào DB)

---

## 🔧 Cấu Hình Production

### Sử Dụng Environment Variables

```bash
# Linux/Mac
export BankTransfer__BankCode="970415"
export BankTransfer__AccountNumber="123456789"
export BankTransfer__AccountName="OCOP GIA LAI"

# Windows PowerShell
$env:BankTransfer__BankCode="970415"
$env:BankTransfer__AccountNumber="123456789"
$env:BankTransfer__AccountName="OCOP GIA LAI"
```

### Sử Dụng Azure App Settings

Trong Azure Portal → App Service → Configuration → Application settings:

```
BankTransfer:BankCode = 970415
BankTransfer:AccountNumber = 123456789
BankTransfer:AccountName = OCOP GIA LAI
```

---

## 📝 Checklist Cấu Hình

### Trước Khi Deploy

- [ ] Đã cấu hình Global Settings trong `appsettings.json`
- [ ] Đã kiểm tra mã ngân hàng đúng
- [ ] Đã kiểm tra số tài khoản đúng
- [ ] Đã kiểm tra tên chủ tài khoản đúng
- [ ] Đã cấu hình thông tin ngân hàng cho các Enterprise cần thiết
- [ ] Đã test tạo payment và kiểm tra QR code
- [ ] Đã đảm bảo không commit thông tin nhạy cảm vào Git

---

## 🆘 Troubleshooting

### Lỗi: "Cấu hình BankTransfer chưa được thiết lập đầy đủ"

**Nguyên nhân:**
- Enterprise chưa cấu hình thông tin ngân hàng
- Global Settings cũng chưa có

**Giải pháp:**
1. Cấu hình Global Settings trong `appsettings.json`
2. Hoặc cấu hình thông tin ngân hàng cho Enterprise cụ thể

### QR Code Không Hiển Thị

**Nguyên nhân:**
- `QrCodeUrl` = `null` (có thể do COD hoặc lỗi tạo URL)

**Giải pháp:**
1. Kiểm tra `paymentMethod` = `"BankTransfer"`
2. Kiểm tra thông tin ngân hàng đã được cấu hình
3. Kiểm tra log để xem có exception không

### QR Code Hiển Thị Sai Thông Tin

**Nguyên nhân:**
- Cấu hình sai mã ngân hàng hoặc số tài khoản

**Giải pháp:**
1. Kiểm tra lại Global Settings hoặc Enterprise Settings
2. Restart ứng dụng nếu cập nhật Global Settings
3. Test lại tạo payment

---

## 📚 Tài Liệu Tham Khảo

- [VietQR Documentation](https://vietqr.io/)
- [Danh sách mã ngân hàng](https://www.vietqr.io/bank-list)
- [VietQR Standard](https://www.vietqr.io/standard)

---

**Version:** 1.0  
**Last Updated:** 2024-11-13  
**Author:** GiaLai OCOP Team

