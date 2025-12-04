# 📱 Hướng Dẫn Quy Trình Tạo QR Code Thanh Toán

Tài liệu giải thích chi tiết quy trình tạo QR code cho thanh toán chuyển khoản (Bank Transfer) trong hệ thống GiaLai OCOP.

---

## 📋 Tổng Quan

Hệ thống sử dụng **VietQR API** để tự động tạo QR code cho thanh toán chuyển khoản. QR code được tạo riêng cho từng **Enterprise** trong một đơn hàng, cho phép khách hàng thanh toán trực tiếp cho từng doanh nghiệp.

---

## 🔄 Quy Trình Tạo QR Code

### Bước 1: Khách Hàng Tạo Đơn Hàng

Khi khách hàng tạo đơn hàng với phương thức thanh toán `BankTransfer`, hệ thống sẽ:

1. **Phân chia đơn hàng theo Enterprise**: Mỗi sản phẩm trong đơn thuộc về một Enterprise
2. **Tính tổng tiền cho từng Enterprise**: Tính tổng giá trị sản phẩm của mỗi Enterprise
3. **Tạo Payment riêng cho mỗi Enterprise**: Mỗi Enterprise sẽ có một Payment record riêng

### Bước 2: Xác Định Thông Tin Ngân Hàng

Hệ thống sẽ ưu tiên lấy thông tin ngân hàng theo thứ tự:

#### Ưu Tiên 1: Thông Tin Từ Enterprise (Nếu Có)

```csharp
if (!string.IsNullOrWhiteSpace(enterprise.BankCode) &&
    !string.IsNullOrWhiteSpace(enterprise.BankAccount) &&
    !string.IsNullOrWhiteSpace(enterprise.BankAccountName))
{
    // Sử dụng thông tin từ Enterprise
    bankCode = enterprise.BankCode;
    bankAccount = enterprise.BankAccount;
    accountName = enterprise.BankAccountName;
}
```

**Lợi ích:**
- Mỗi Enterprise có thể có tài khoản ngân hàng riêng
- Tiền sẽ được chuyển trực tiếp vào tài khoản của Enterprise
- Linh hoạt trong quản lý tài chính

#### Ưu Tiên 2: Thông Tin Từ Global Settings (Fallback)

Nếu Enterprise chưa cấu hình thông tin ngân hàng, hệ thống sẽ sử dụng cấu hình global từ `appsettings.json`:

```json
{
  "BankTransfer": {
    "BankCode": "970415",
    "AccountNumber": "123456789",
    "AccountName": "OCOP GIA LAI",
    "Template": "compact",
    "BaseUrl": "https://img.vietqr.io/image",
    "Description": "Thanh toan don hang OCOP"
  }
}
```

**Lưu ý:** Nếu cả Enterprise và Global Settings đều không có thông tin, hệ thống sẽ throw exception.

### Bước 3: Tạo Reference Code

Hệ thống tự động tạo một mã tham chiếu (Reference) duy nhất cho mỗi Payment:

```csharp
private string GenerateReference(int orderId, int enterpriseId, string method)
{
    var prefix = method == "BankTransfer" ? "BT" : "COD";
    return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{orderId}-E{enterpriseId}";
}
```

**Format:** `BT-YYYYMMDDHHMMSS-{OrderId}-E{EnterpriseId}`

**Ví dụ:** `BT-20241113143025-123-E5`
- `BT`: BankTransfer prefix
- `20241113143025`: Timestamp (2024-11-13 14:30:25)
- `123`: Order ID
- `E5`: Enterprise ID = 5

**Mục đích:**
- Theo dõi từng giao dịch thanh toán
- Dễ dàng đối soát với ngân hàng
- Tránh trùng lặp

### Bước 4: Xây Dựng URL QR Code

Hệ thống sử dụng VietQR API để tạo URL QR code:

```csharp
private string BuildVietQrUrl(decimal amount, string reference, BankTransferSettings settings)
{
    var baseUrl = "https://img.vietqr.io/image";
    var template = settings.Template ?? "compact";
    var addInfo = Uri.EscapeDataString(reference);
    var accountName = Uri.EscapeDataString(settings.AccountName);
    var description = Uri.EscapeDataString(settings.Description ?? reference);
    var amountString = amount > 0 ? $"&amount={(int)amount}" : string.Empty;

    return $"{baseUrl}/{settings.BankCode}-{settings.AccountNumber}-{template}.png?addInfo={addInfo}{amountString}&accountName={accountName}&description={description}";
}
```

**Cấu trúc URL:**
```
https://img.vietqr.io/image/{BankCode}-{AccountNumber}-{Template}.png
  ?addInfo={Reference}
  &amount={Amount}
  &accountName={AccountName}
  &description={Description}
```

**Ví dụ URL thực tế:**
```
https://img.vietqr.io/image/970415-123456789-compact.png
  ?addInfo=BT-20241113143025-123-E5
  &amount=500000
  &accountName=OCOP%20GIA%20LAI
  &description=Thanh%20toan%20don%20hang%20OCOP%20-%20Enterprise%20Name
```

### Bước 5: Lưu Thông Tin Payment

Sau khi tạo QR code URL, hệ thống lưu thông tin vào database:

```csharp
var payment = new Payment
{
    OrderId = order.Id,
    EnterpriseId = enterprise.Id,
    Amount = amount,
    Method = "BankTransfer",
    Status = "AwaitingTransfer", // Chờ chuyển khoản
    Reference = reference,
    BankCode = bankCode,
    BankAccount = bankAccount,
    AccountName = accountName,
    QrCodeUrl = qrUrl, // ✅ URL QR code được lưu ở đây
    CreatedAt = DateTime.UtcNow
};
```

---

## 📊 Sơ Đồ Quy Trình

```
┌─────────────────────────────────┐
│  Customer tạo Order             │
│  PaymentMethod = "BankTransfer"  │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  Phân chia OrderItems            │
│  theo Enterprise                 │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  Với mỗi Enterprise:             │
│  1. Tính tổng Amount             │
│  2. Lấy thông tin ngân hàng      │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  Xác định Bank Info:             │
│  ✅ Enterprise.BankCode?        │
│  ✅ Enterprise.BankAccount?      │
│  ✅ Enterprise.BankAccountName?   │
│                                  │
│  Nếu không có →                  │
│  Dùng Global Settings            │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  Tạo Reference Code:             │
│  BT-{Timestamp}-{OrderId}-E{EId}│
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  Build VietQR URL:                │
│  https://img.vietqr.io/image/    │
│  {BankCode}-{Account}-compact.png│
│  ?addInfo={Reference}            │
│  &amount={Amount}                 │
│  &accountName={Name}             │
│  &description={Desc}             │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  Lưu Payment vào Database:       │
│  - QrCodeUrl = {URL}             │
│  - Status = "AwaitingTransfer"   │
│  - Reference = {Reference}        │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│  Trả về PaymentDto cho Frontend  │
│  với QrCodeUrl                   │
└─────────────────────────────────┘
```

---

## 🔧 Cấu Hình

### 1. Cấu Hình Global (appsettings.json)

```json
{
  "BankTransfer": {
    "BankCode": "970415",                    // Mã ngân hàng (970415 = MB Bank)
    "AccountNumber": "123456789",            // Số tài khoản
    "AccountName": "OCOP GIA LAI",           // Tên chủ tài khoản
    "Template": "compact",                    // Template QR: "compact" hoặc "print"
    "BaseUrl": "https://img.vietqr.io/image", // Base URL của VietQR API
    "Description": "Thanh toan don hang OCOP" // Mô tả mặc định
  }
}
```

### 2. Cấu Hình Cho Từng Enterprise

Mỗi Enterprise có thể cấu hình thông tin ngân hàng riêng trong database:

```sql
UPDATE "Enterprises"
SET 
  "BankCode" = '970415',
  "BankAccount" = '987654321',
  "BankAccountName" = 'DOANH NGHIEP ABC'
WHERE "Id" = 5;
```

Hoặc qua API:
```http
PUT /api/enterprises/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "bankCode": "970415",
  "bankAccount": "987654321",
  "bankAccountName": "DOANH NGHIEP ABC"
}
```

---

## 📱 Sử Dụng QR Code

### Frontend Flow

1. **Khách hàng tạo đơn hàng** với `paymentMethod = "BankTransfer"`

2. **Backend trả về danh sách Payments** với QR code URL:

```json
{
  "payments": [
    {
      "id": 1,
      "enterpriseId": 5,
      "enterpriseName": "Doanh Nghiệp A",
      "amount": 500000,
      "method": "BankTransfer",
      "status": "AwaitingTransfer",
      "qrCodeUrl": "https://img.vietqr.io/image/970415-123456789-compact.png?...",
      "reference": "BT-20241113143025-123-E5"
    },
    {
      "id": 2,
      "enterpriseId": 6,
      "enterpriseName": "Doanh Nghiệp B",
      "amount": 300000,
      "method": "BankTransfer",
      "status": "AwaitingTransfer",
      "qrCodeUrl": "https://img.vietqr.io/image/970415-123456789-compact.png?...",
      "reference": "BT-20241113143025-123-E6"
    }
  ]
}
```

3. **Frontend hiển thị QR code** cho từng Payment:

```jsx
{payments.map(payment => (
  <div key={payment.id}>
    <h3>{payment.enterpriseName}</h3>
    <p>Số tiền: {payment.amount.toLocaleString('vi-VN')} VNĐ</p>
    <p>Mã tham chiếu: {payment.reference}</p>
    <img src={payment.qrCodeUrl} alt="QR Code" />
    <p>Quét QR code để thanh toán</p>
  </div>
))}
```

4. **Khách hàng quét QR code** bằng app ngân hàng và chuyển khoản

5. **EnterpriseAdmin xác nhận thanh toán** khi nhận được tiền:

```http
POST /api/payments/{paymentId}/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "Paid"
}
```

---

## 🔍 Chi Tiết Kỹ Thuật

### 1. VietQR API Format

VietQR sử dụng chuẩn **VietQR Standard** (tiêu chuẩn QR code thanh toán của Ngân hàng Nhà nước Việt Nam).

**Các thông tin trong QR code:**
- **BankCode**: Mã ngân hàng (ví dụ: 970415 = MB Bank)
- **AccountNumber**: Số tài khoản
- **Amount**: Số tiền (tùy chọn, có thể để trống)
- **AddInfo**: Nội dung chuyển khoản (Reference code)
- **AccountName**: Tên chủ tài khoản
- **Description**: Mô tả giao dịch

### 2. URL Encoding

Tất cả các tham số trong URL đều được encode bằng `Uri.EscapeDataString()`:

```csharp
var addInfo = Uri.EscapeDataString(reference);
// "BT-20241113143025-123-E5" → "BT-20241113143025-123-E5"

var accountName = Uri.EscapeDataString(settings.AccountName);
// "OCOP GIA LAI" → "OCOP%20GIA%20LAI"

var description = Uri.EscapeDataString(settings.Description);
// "Thanh toan don hang OCOP - Enterprise Name" 
// → "Thanh%20toan%20don%20hang%20OCOP%20-%20Enterprise%20Name"
```

### 3. Template Options

VietQR hỗ trợ 2 template:

- **`compact`**: QR code nhỏ gọn, phù hợp hiển thị trên màn hình
- **`print`**: QR code lớn hơn, phù hợp in ấn

### 4. Amount Format

Số tiền được chuyển đổi sang integer (VND):

```csharp
var amountString = amount > 0 ? $"&amount={(int)amount}" : string.Empty;
// 500000.50 → "&amount=500000"
```

---

## ⚠️ Lưu Ý Quan Trọng

### 1. Bảo Mật

- ✅ QR code URL chỉ chứa thông tin công khai (không có thông tin nhạy cảm)
- ✅ Reference code là duy nhất, không thể đoán trước
- ✅ Không lưu mật khẩu hoặc thông tin bảo mật trong QR code

### 2. Xử Lý Lỗi

Nếu Enterprise chưa cấu hình thông tin ngân hàng và Global Settings cũng chưa có:

```csharp
throw new InvalidOperationException(
    $"Cấu hình BankTransfer cho Enterprise {enterprise.Name} (ID: {enterprise.Id}) chưa được thiết lập đầy đủ."
);
```

**Giải pháp:**
- Cấu hình thông tin ngân hàng cho Enterprise
- Hoặc cấu hình Global Settings trong `appsettings.json`

### 3. COD vs BankTransfer

- **COD**: Không tạo QR code (`QrCodeUrl = null`)
- **BankTransfer**: Tự động tạo QR code và lưu vào `QrCodeUrl`

### 4. Multiple Payments

Một đơn hàng có thể có nhiều Payment (mỗi Enterprise một Payment):

```json
{
  "orderId": 123,
  "payments": [
    { "enterpriseId": 5, "amount": 500000, "qrCodeUrl": "..." },
    { "enterpriseId": 6, "amount": 300000, "qrCodeUrl": "..." }
  ]
}
```

Khách hàng cần thanh toán cho **tất cả** các Payment để đơn hàng được xác nhận.

---

## 🧪 Testing

### Test với Swagger

1. Tạo đơn hàng với `paymentMethod = "BankTransfer"`
2. Kiểm tra response có chứa `qrCodeUrl`
3. Mở URL QR code trong browser để xem QR code
4. Quét QR code bằng app ngân hàng để kiểm tra thông tin

### Test với curl

```bash
# Tạo payment
curl -X POST https://localhost:5001/api/payments \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": 123,
    "paymentMethod": "BankTransfer"
  }'

# Response sẽ chứa qrCodeUrl
{
  "payments": [
    {
      "id": 1,
      "qrCodeUrl": "https://img.vietqr.io/image/...",
      ...
    }
  ]
}
```

---

## 📚 Tài Liệu Tham Khảo

- [VietQR Documentation](https://vietqr.io/)
- [VietQR Standard](https://www.vietqr.io/standard)
- [Danh sách mã ngân hàng](https://www.vietqr.io/bank-list)

---

## 💡 Best Practices

1. **Luôn kiểm tra QrCodeUrl trước khi hiển thị:**
   ```javascript
   if (payment.qrCodeUrl) {
     // Hiển thị QR code
   } else {
     // Hiển thị thông báo lỗi
   }
   ```

2. **Hiển thị Reference code cho khách hàng:**
   - Giúp khách hàng ghi chú khi chuyển khoản
   - Dễ dàng đối soát sau này

3. **Cập nhật Status sau khi thanh toán:**
   - Chuyển từ `AwaitingTransfer` → `Paid`
   - Ghi lại thời gian thanh toán (`PaidAt`)

4. **Validate Amount:**
   - Đảm bảo số tiền > 0
   - Kiểm tra format số tiền (VND)

---

**Version:** 1.0  
**Last Updated:** 2024-11-13  
**Author:** GiaLai OCOP Team

