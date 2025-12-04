# 🏦 Hướng Dẫn Quản Lý Thông Tin Ngân Hàng Cho EnterpriseAdmin

Tài liệu hướng dẫn chi tiết về chức năng quản lý thông tin ngân hàng và tạo QR code thanh toán cho EnterpriseAdmin.

---

## 📋 Tổng Quan

Chức năng này cho phép **EnterpriseAdmin** cấu hình thông tin ngân hàng của doanh nghiệp mình, hệ thống sẽ tự động tạo QR code theo chuẩn **VietQR/EMVCo** và lưu vào database. QR code này được sử dụng khi khách hàng thanh toán đơn hàng.

### Đặc Điểm

- ✅ **QR code được lưu một lần** - Chỉ chứa thông tin tài khoản, không có amount
- ✅ **QR code động khi thanh toán** - Tạo QR code mới với amount và description cho mỗi giao dịch
- ✅ **Chuẩn VietQR/EMVCo** - Có thể quét bằng mọi app ngân hàng
- ✅ **Mỗi Enterprise có QR riêng** - Mỗi doanh nghiệp quản lý thông tin ngân hàng độc lập

---

## 🔧 1. EnterpriseAdmin Cấu Hình Thông Tin Ngân Hàng

### Endpoint

```
POST /api/enterprise-bank-info
```

### Request Body

```json
{
  "bankName": "Ngân hàng Quân đội",
  "bankAccount": "987654321",
  "accountName": "DOANH NGHIEP ABC",
  "bankCode": "970415",
  "template": "compact"
}
```

### Các Trường

| Trường | Bắt Buộc | Mô Tả | Ví Dụ |
|--------|----------|-------|-------|
| `bankName` | ✅ | Tên ngân hàng | "Ngân hàng Quân đội" |
| `bankAccount` | ✅ | Số tài khoản | "987654321" |
| `accountName` | ✅ | Tên chủ tài khoản | "DOANH NGHIEP ABC" |
| `bankCode` | ✅ | Mã ngân hàng theo Napas | "970415" (MB Bank) |
| `template` | ❌ | Template QR | "compact" hoặc "print" (mặc định: "compact") |

### Response (Thành Công)

```json
{
  "id": 1,
  "enterpriseId": 5,
  "bankName": "Ngân hàng Quân đội",
  "bankAccount": "987654321",
  "accountName": "DOANH NGHIEP ABC",
  "bankCode": "970415",
  "template": "compact",
  "qrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAA...", // QR code base64 (chỉ thông tin tài khoản)
  "createdAt": "2024-11-13T10:30:00Z",
  "updatedAt": null
}
```

### Lưu Ý

- ✅ Nếu Enterprise đã có thông tin ngân hàng, endpoint này sẽ **cập nhật** thông tin hiện có
- ✅ QR code sẽ được **tạo lại tự động** khi cập nhật thông tin
- ✅ QR code base64 trong response chỉ chứa thông tin tài khoản (không có amount)

---

## 🔄 2. Cập Nhật Thông Tin Ngân Hàng

### Endpoint

```
PUT /api/enterprise-bank-info
```

### Request Body

```json
{
  "bankName": "Ngân hàng Quân đội",
  "bankAccount": "987654321",
  "accountName": "DOANH NGHIEP ABC UPDATED",
  "bankCode": "970415",
  "template": "print"
}
```

**Lưu ý:** Tất cả các trường đều **optional**, chỉ cần gửi các trường muốn cập nhật.

### Response

Tương tự như POST, nhưng `updatedAt` sẽ có giá trị.

---

## 👁️ 3. Xem Thông Tin Ngân Hàng Của Mình

### Endpoint

```
GET /api/enterprise-bank-info/me
```

### Response

```json
{
  "id": 1,
  "enterpriseId": 5,
  "bankName": "Ngân hàng Quân đội",
  "bankAccount": "987654321",
  "accountName": "DOANH NGHIEP ABC",
  "bankCode": "970415",
  "template": "compact",
  "qrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAA...",
  "createdAt": "2024-11-13T10:30:00Z",
  "updatedAt": null
}
```

---

## 💳 4. Lấy QR Code Thanh Toán (Khi Khách Hàng Đặt Hàng)

### Endpoint

```
GET /api/payments/{paymentId}/qr-code
```

### Response

```json
{
  "qrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAA...", // QR code với amount và description
  "description": "Thanh toan don hang #123",
  "amount": 500000,
  "enterpriseBankName": "Ngân hàng Quân đội",
  "enterpriseAccountNumber": "987654321",
  "accountName": "DOANH NGHIEP ABC"
}
```

### Đặc Điểm

- ✅ QR code này **khác** với QR code trong `EnterpriseBankInfo`
- ✅ QR code này chứa **amount** và **description** cụ thể cho giao dịch
- ✅ QR code được tạo **động** mỗi lần gọi endpoint
- ✅ Format: `"Thanh toan don hang #{orderId}"`

---

## 📱 5. Frontend Flow

### Bước 1: EnterpriseAdmin Cấu Hình Thông Tin Ngân Hàng

```javascript
// EnterpriseAdmin nhập thông tin ngân hàng
async function saveBankInfo(bankInfo) {
  const response = await fetch('/api/enterprise-bank-info', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      bankName: bankInfo.bankName,
      bankAccount: bankInfo.bankAccount,
      accountName: bankInfo.accountName,
      bankCode: bankInfo.bankCode,
      template: 'compact'
    })
  });

  const data = await response.json();
  // QR code base64 đã được tạo và lưu
  console.log('QR Code Base64:', data.qrCodeBase64);
}
```

### Bước 2: Khách Hàng Tạo Đơn Hàng

```javascript
// Khách hàng tạo đơn hàng với paymentMethod = "BankTransfer"
const order = await createOrder({
  items: [...],
  paymentMethod: 'BankTransfer'
});

// Backend tự động tạo Payment cho mỗi Enterprise
// Response chứa danh sách payments
```

### Bước 3: Lấy QR Code Thanh Toán

```javascript
// Với mỗi payment, lấy QR code thanh toán
async function getPaymentQrCode(paymentId) {
  const response = await fetch(`/api/payments/${paymentId}/qr-code`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  const qrData = await response.json();
  
  // Hiển thị QR code
  displayQrCode(qrData.qrCodeBase64, {
    amount: qrData.amount,
    description: qrData.description,
    bankName: qrData.enterpriseBankName,
    accountNumber: qrData.enterpriseAccountNumber
  });
}
```

### Bước 4: Hiển Thị QR Code

```jsx
function PaymentQrCodeDisplay({ paymentId }) {
  const [qrData, setQrData] = useState(null);

  useEffect(() => {
    fetch(`/api/payments/${paymentId}/qr-code`)
      .then(res => res.json())
      .then(data => setQrData(data));
  }, [paymentId]);

  if (!qrData) return <div>Loading...</div>;

  return (
    <div className="payment-qr-code">
      <h3>Thanh toán cho: {qrData.enterpriseBankName}</h3>
      <p>Số tiền: {qrData.amount.toLocaleString('vi-VN')} VNĐ</p>
      <p>Nội dung: {qrData.description}</p>
      <p>Số tài khoản: {qrData.enterpriseAccountNumber}</p>
      <p>Chủ tài khoản: {qrData.accountName}</p>
      
      {/* Hiển thị QR code */}
      <img 
        src={`data:image/png;base64,${qrData.qrCodeBase64}`} 
        alt="QR Code Thanh Toán"
        style={{ width: '300px', height: '300px' }}
      />
      
      <p>Quét QR code để thanh toán</p>
    </div>
  );
}
```

---

## 🔍 6. Logic Ưu Tiên Khi Tạo Payment

Khi khách hàng tạo đơn hàng với `paymentMethod = "BankTransfer"`, hệ thống sẽ kiểm tra theo thứ tự:

```
1. EnterpriseBankInfo có tồn tại?
   ├─ CÓ → Dùng thông tin từ EnterpriseBankInfo ✅
   └─ KHÔNG → Chuyển sang bước 2

2. Enterprise.BankCode có tồn tại? (tương thích với code cũ)
   ├─ CÓ → Dùng thông tin từ Enterprise ✅
   └─ KHÔNG → Chuyển sang bước 3

3. Global Settings có tồn tại?
   ├─ CÓ → Dùng Global Settings ✅
   └─ KHÔNG → Throw Exception ❌
```

---

## 📊 7. So Sánh QR Code

### QR Code Trong EnterpriseBankInfo

- **Mục đích:** Lưu trữ thông tin tài khoản
- **Nội dung:** Chỉ chứa thông tin tài khoản (bankCode, bankAccount, accountName)
- **Khi nào tạo:** Khi EnterpriseAdmin cấu hình thông tin ngân hàng
- **Tần suất:** Tạo một lần, cập nhật khi thông tin thay đổi
- **Sử dụng:** Không dùng trực tiếp cho thanh toán

### QR Code Thanh Toán (Payment QR Code)

- **Mục đích:** Hiển thị cho khách hàng để thanh toán
- **Nội dung:** Thông tin tài khoản + amount + description
- **Khi nào tạo:** Khi gọi `GET /api/payments/{id}/qr-code`
- **Tần suất:** Tạo động mỗi lần gọi endpoint
- **Sử dụng:** Hiển thị cho khách hàng quét và thanh toán

---

## 🧪 8. Testing

### Test Cấu Hình Thông Tin Ngân Hàng

```bash
# 1. EnterpriseAdmin đăng nhập và lấy token
TOKEN="your-enterprise-admin-token"

# 2. Tạo/cập nhật thông tin ngân hàng
curl -X POST https://localhost:5001/api/enterprise-bank-info \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "bankName": "Ngân hàng Quân đội",
    "bankAccount": "987654321",
    "accountName": "DOANH NGHIEP ABC",
    "bankCode": "970415",
    "template": "compact"
  }'

# 3. Xem thông tin đã lưu
curl -X GET https://localhost:5001/api/enterprise-bank-info/me \
  -H "Authorization: Bearer $TOKEN"
```

### Test Lấy QR Code Thanh Toán

```bash
# 1. Khách hàng tạo đơn hàng (sẽ tự động tạo Payment)
# 2. Lấy paymentId từ response
PAYMENT_ID=1

# 3. Lấy QR code thanh toán
curl -X GET https://localhost:5001/api/payments/$PAYMENT_ID/qr-code \
  -H "Authorization: Bearer $TOKEN"

# Response sẽ chứa qrCodeBase64 với amount và description
```

---

## ⚠️ 9. Lưu Ý Quan Trọng

### Bảo Mật

- ✅ Chỉ **EnterpriseAdmin** mới có thể cấu hình thông tin ngân hàng của doanh nghiệp mình
- ✅ Khách hàng chỉ có thể xem QR code thanh toán của đơn hàng của mình
- ✅ QR code base64 không chứa thông tin nhạy cảm (chỉ thông tin công khai)

### Validation

- ✅ Tất cả các trường bắt buộc phải được điền đầy đủ khi tạo mới
- ✅ Mã ngân hàng phải đúng theo chuẩn Napas
- ✅ Số tài khoản phải hợp lệ

### Performance

- ✅ QR code trong `EnterpriseBankInfo` được lưu sẵn, không tạo lại mỗi lần
- ✅ QR code thanh toán được tạo động nhưng nhanh (sử dụng QRCoder library)
- ✅ Có thể cache QR code thanh toán nếu cần (tùy chọn)

---

## 📚 10. Danh Sách Mã Ngân Hàng

| Mã | Tên Ngân Hàng |
|----|---------------|
| 970415 | MB Bank (Ngân hàng Quân đội) |
| 970422 | VietinBank |
| 970436 | Vietcombank |
| 970441 | BIDV |
| 970448 | Agribank |
| 970454 | ACB (Asia Commercial Bank) |
| 970458 | Techcombank |
| 970461 | VPBank |
| 970466 | TPBank |
| 970467 | HDBank |

**Xem danh sách đầy đủ:** [VietQR Bank List](https://www.vietqr.io/bank-list)

---

## 🆘 11. Troubleshooting

### Lỗi: "Enterprise chưa cấu hình thông tin ngân hàng"

**Nguyên nhân:** EnterpriseAdmin chưa cấu hình thông tin ngân hàng.

**Giải pháp:** EnterpriseAdmin cần gọi `POST /api/enterprise-bank-info` để cấu hình.

### QR Code Không Quét Được

**Nguyên nhân:** 
- EMVCo string không đúng format
- CRC không đúng

**Giải pháp:**
- Kiểm tra lại thông tin ngân hàng (bankCode, bankAccount, accountName)
- Đảm bảo sử dụng mã ngân hàng đúng theo chuẩn Napas
- Test với app ngân hàng khác nhau

### Lỗi: "Lỗi khi tạo QR code"

**Nguyên nhân:** 
- QRCoder library lỗi
- Thông tin đầu vào không hợp lệ

**Giải pháp:**
- Kiểm tra log để xem chi tiết lỗi
- Đảm bảo tất cả thông tin đều hợp lệ
- Thử lại sau vài giây

---

## 📝 12. Checklist

### Cho EnterpriseAdmin

- [ ] Đã cấu hình thông tin ngân hàng qua `POST /api/enterprise-bank-info`
- [ ] Đã kiểm tra QR code base64 được tạo thành công
- [ ] Đã test QR code có thể quét được bằng app ngân hàng
- [ ] Đã cập nhật thông tin nếu có thay đổi

### Cho Frontend Developer

- [ ] Đã tích hợp form cấu hình thông tin ngân hàng cho EnterpriseAdmin
- [ ] Đã tích hợp hiển thị QR code thanh toán cho khách hàng
- [ ] Đã test flow từ tạo đơn hàng đến hiển thị QR code
- [ ] Đã xử lý error cases (chưa cấu hình, lỗi tạo QR, etc.)

---

**Version:** 1.0  
**Last Updated:** 2024-11-13  
**Author:** GiaLai OCOP Team

