# 💳 Payment API Documentation

## Tổng quan

Hệ thống hỗ trợ 2 hình thức thanh toán:

1. **COD (Cash on Delivery)** – thanh toán khi nhận hàng.
2. **BankTransfer** – chuyển khoản qua mã QR (VietQR).

Thanh toán được tách riêng khỏi quy trình tạo đơn hàng. Sau khi tạo đơn (`POST /api/orders`), khách hàng gọi API thanh toán để nhận hướng dẫn/QR code.

### ⚠️ Tính năng mới: Payment riêng cho mỗi Enterprise

**Quan trọng:** Hệ thống tự động tạo payment riêng cho mỗi Enterprise trong đơn hàng:
- Mỗi Enterprise có QR code và thông tin thanh toán riêng
- Amount được tính riêng cho từng Enterprise (tổng giá trị sản phẩm của Enterprise đó)
- Response trả về **mảng payments** (không phải một payment đơn lẻ)
- Mỗi Enterprise có thể cấu hình thông tin ngân hàng riêng trong bảng `Enterprises`

---

## 🔹 Các trạng thái thanh toán

| Trạng thái        | Ý nghĩa                                                           |
|-------------------|-------------------------------------------------------------------|
| `Pending`         | Đơn hàng mới, chưa khởi tạo thanh toán hoặc COD chờ giao          |
| `AwaitingTransfer`| Đang chờ khách hàng chuyển khoản                                  |
| `Paid`            | Đã thanh toán thành công                                          |
| `Cancelled`       | Giao dịch thanh toán bị hủy                                       |

---

## 🔹 API Endpoints

### 1. Khởi tạo thanh toán
**Customer** – tạo thanh toán cho đơn hàng của mình.

```
POST /api/payments
```

**Body:**
```json
{
  "orderId": 123,
  "method": "BankTransfer" // hoặc "COD"
}
```

**Response 201:** (Mảng payments - một cho mỗi Enterprise)

**Đơn hàng 1 Enterprise:**
```json
[
  {
    "id": 10,
    "orderId": 123,
    "enterpriseId": 1,
    "enterpriseName": "Doanh nghiệp A",
    "amount": 350000,
    "method": "BankTransfer",
    "status": "AwaitingTransfer",
    "reference": "BT-20241112091530-123-E1",
    "bankCode": "970415",
    "bankAccount": "123456789",
    "accountName": "OCOP GIA LAI",
    "qrCodeUrl": "https://img.vietqr.io/image/970415-123456789-compact.png?addInfo=BT-20241112091530-123-E1&amount=350000&accountName=OCOP%20GIA%20LAI&description=Thanh%20toan%20don%20hang%20OCOP%20-%20Doanh%20nghiep%20A",
    "createdAt": "2024-11-12T09:15:30Z",
    "paidAt": null
  }
]
```

**Đơn hàng nhiều Enterprise:**
```json
[
  {
    "id": 10,
    "orderId": 123,
    "enterpriseId": 1,
    "enterpriseName": "Doanh nghiệp A",
    "amount": 200000,
    "method": "BankTransfer",
    "status": "AwaitingTransfer",
    "reference": "BT-20241112091530-123-E1",
    "bankCode": "970415",
    "bankAccount": "111111111",
    "accountName": "Doanh nghiệp A",
    "qrCodeUrl": "https://img.vietqr.io/image/970415-111111111-compact.png?addInfo=BT-20241112091530-123-E1&amount=200000&accountName=Doanh%20nghiep%20A&description=Thanh%20toan%20don%20hang%20OCOP%20-%20Doanh%20nghiep%20A",
    "createdAt": "2024-11-12T09:15:30Z",
    "paidAt": null
  },
  {
    "id": 11,
    "orderId": 123,
    "enterpriseId": 2,
    "enterpriseName": "Doanh nghiệp B",
    "amount": 150000,
    "method": "BankTransfer",
    "status": "AwaitingTransfer",
    "reference": "BT-20241112091530-123-E2",
    "bankCode": "970422",
    "bankAccount": "222222222",
    "accountName": "Doanh nghiệp B",
    "qrCodeUrl": "https://img.vietqr.io/image/970422-222222222-compact.png?addInfo=BT-20241112091530-123-E2&amount=150000&accountName=Doanh%20nghiep%20B&description=Thanh%20toan%20don%20hang%20OCOP%20-%20Doanh%20nghiep%20B",
    "createdAt": "2024-11-12T09:15:30Z",
    "paidAt": null
  }
]
```

**Lưu ý:**
- Response là **mảng payments** (không phải object đơn lẻ)
- Mỗi payment có `enterpriseId` và `enterpriseName` riêng
- `amount` được tính riêng cho từng Enterprise (tổng giá trị sản phẩm của Enterprise đó)
- `reference` có format `BT-YYYYMMDDHHmmss-{orderId}-E{enterpriseId}` hoặc `COD-YYYYMMDDHHmmss-{orderId}-E{enterpriseId}`
- Nếu Enterprise có cấu hình ngân hàng riêng (`BankCode`, `BankAccount`, `BankAccountName`), hệ thống sẽ dùng thông tin đó
- Nếu Enterprise không có cấu hình, hệ thống sẽ dùng global settings từ `appsettings.json`
- Khi chọn `BankTransfer`, hệ thống tạo `reference` và trả về link QR theo cấu hình của từng Enterprise
- Khi chọn `COD`, trạng thái thanh toán của đơn chuyển về `Pending`

---

### 2. Xem chi tiết thanh toán
```
GET /api/payments/{id}
```

**Phân quyền:**
- Customer: chỉ xem thanh toán thuộc đơn của mình.
- EnterpriseAdmin/SystemAdmin: xem được tất cả (EnterpriseAdmin xem được đơn chứa sản phẩm của doanh nghiệp mình).

---

### 3. Danh sách thanh toán của đơn hàng
```
GET /api/payments/order/{orderId}
```

**Response:** danh sách `PaymentDto` sắp xếp mới nhất → cũ.

---

### 4. Cập nhật trạng thái thanh toán
**SystemAdmin & EnterpriseAdmin** – xác nhận thanh toán chuyển khoản hoặc hủy giao dịch.

```
POST /api/payments/{id}/status
```

**Body:**
```json
{
  "status": "Paid",   // hoặc "Cancelled"
  "notes": "Đã nhận chuyển khoản 350,000đ."
}
```

**Phân quyền:**
- **SystemAdmin**: Có thể xác nhận bất kỳ payment nào
- **EnterpriseAdmin**: Chỉ có thể xác nhận payment của Enterprise của mình

**Cập nhật Order.PaymentStatus:**
- Khi set `Paid`:
  - Cập nhật `payment.PaidAt`
  - Nếu **tất cả payments** của order đã Paid → `order.PaymentStatus = "Paid"`
  - Nếu **một số payments** đã Paid → `order.PaymentStatus = "PartiallyPaid"`
- Khi set `Cancelled`:
  - Nếu còn payment nào Pending/AwaitingTransfer → `order.PaymentStatus = "Pending"`
  - Nếu tất cả payments đều bị hủy → `order.PaymentStatus = "Cancelled"`

---

## 🔹 Model cập nhật

### Order
- `PaymentMethod`: `"COD"` hoặc `"BankTransfer"`
- `PaymentStatus`: `"Pending"`, `"AwaitingTransfer"`, `"Paid"`, `"Cancelled"`
- `PaymentReference`: mã tham chiếu (đặc biệt hữu ích khi chuyển khoản)
- `Payments`: danh sách giao dịch (lịch sử)

### Payment
```
Id, OrderId, EnterpriseId, EnterpriseName, Amount, Method, Status, Reference,
BankCode, BankAccount, AccountName,
QrCodeUrl, Notes, CreatedAt, PaidAt
```

**Quan hệ:**
- `Payment` có quan hệ với `Enterprise` (mỗi payment thuộc một Enterprise)
- `Enterprise` có thể có nhiều `Payments`

---

## 🔹 Cấu hình VietQR

### Global Settings (appsettings.json)
```json
"BankTransfer": {
  "BankCode": "970415",
  "AccountNumber": "123456789",
  "AccountName": "OCOP GIA LAI",
  "Template": "compact",
  "BaseUrl": "https://img.vietqr.io/image",
  "Description": "Thanh toan don hang OCOP"
}
```

### Enterprise Settings (Database)
Mỗi Enterprise có thể cấu hình thông tin ngân hàng riêng trong bảng `Enterprises`:
- `BankCode`: Mã ngân hàng (ví dụ: "970415" cho MB Bank)
- `BankAccount`: Số tài khoản ngân hàng
- `BankAccountName`: Tên chủ tài khoản

**Ưu tiên:**
1. Nếu Enterprise có cấu hình ngân hàng → Dùng thông tin của Enterprise
2. Nếu Enterprise không có cấu hình → Dùng global settings từ `appsettings.json`

**Cập nhật Enterprise settings:**
```sql
UPDATE "Enterprises"
SET 
  "BankCode" = '970415',
  "BankAccount" = '111111111',
  "BankAccountName" = 'Doanh nghiệp A'
WHERE "Id" = 1;
```

---

## 🔹 Luồng đề xuất

1. Customer tạo đơn (`POST /api/orders`), chọn phương thức thanh toán (default COD).
2. Customer gọi `POST /api/payments`:
   - Hệ thống tự động tạo payment riêng cho mỗi Enterprise trong đơn hàng
   - COD: nhận hướng dẫn thanh toán khi nhận hàng (mỗi Enterprise một payment).
   - BankTransfer: nhận QR và mã tham chiếu riêng cho mỗi Enterprise.
3. Sau khi nhận tiền, SystemAdmin/EnterpriseAdmin gọi `POST /api/payments/{id}/status` với `status = "Paid"`:
   - EnterpriseAdmin chỉ có thể xác nhận payment của Enterprise của mình
   - SystemAdmin có thể xác nhận bất kỳ payment nào
4. Hệ thống tự động cập nhật `Order.PaymentStatus`:
   - Nếu tất cả payments đã Paid → `"Paid"`
   - Nếu một số payments đã Paid → `"PartiallyPaid"`
5. Đơn hàng có thể cập nhật `Status = Completed` khi giao xong hàng.

---

## 🔹 Frontend Integration

### Hiển thị QR chuyển khoản
```javascript
if (payment.method === 'BankTransfer' && payment.qrCodeUrl) {
  const img = document.createElement('img');
  img.src = payment.qrCodeUrl;
  img.alt = 'Quét mã QR để thanh toán';
  container.appendChild(img);
}
```

### Hướng dẫn COD
```javascript
if (payment.method === 'COD') {
  showMessage('Vui lòng chuẩn bị số tiền tương ứng và thanh toán cho nhân viên giao hàng.');
}
```

---

## 🔹 Testing Checklist

### Test cơ bản:
- [x] Tạo đơn hàng với `paymentMethod = COD`, tạo thanh toán -> status `Pending`.
- [x] Tạo đơn hàng với `paymentMethod = BankTransfer`, tạo thanh toán -> nhận QR.
- [x] Update status -> `Paid`, kiểm tra `order.PaymentStatus` cập nhật.
- [x] Update status -> `Cancelled`, đảm bảo `order.PaymentStatus = Cancelled`.
- [x] Thử tạo thanh toán 2 lần với cùng phương thức: API trả về giao dịch hiện tại (không bị trùng).
- [x] Thử tạo thanh toán với phương thức khác: giao dịch cũ bị đánh dấu `Cancelled`.
- [x] Kiểm tra phân quyền các endpoint.

### Test tính năng mới - Payment riêng cho mỗi Enterprise:
- [x] Tạo đơn hàng có sản phẩm từ 1 Enterprise -> Nhận 1 payment trong mảng
- [x] Tạo đơn hàng có sản phẩm từ 2 Enterprise -> Nhận 2 payments trong mảng
- [x] Mỗi payment có `enterpriseId` và `enterpriseName` riêng
- [x] Mỗi payment có `amount` riêng (tổng giá trị sản phẩm của Enterprise đó)
- [x] Mỗi payment có `reference` riêng (có `-E{enterpriseId}`)
- [x] Mỗi payment có QR code riêng (nếu BankTransfer và Enterprise có cấu hình ngân hàng)
- [x] EnterpriseAdmin chỉ xác nhận được payment của Enterprise mình
- [x] Xác nhận 1 payment -> Order.PaymentStatus = "PartiallyPaid"
- [x] Xác nhận tất cả payments -> Order.PaymentStatus = "Paid"

---

## 📝 Ghi chú

- Mặc định `CreateOrder` sẽ set `PaymentMethod` và `PaymentStatus` ban đầu.
- Seed data không tạo giao dịch mẫu (chỉ tạo doanh nghiệp & sản phẩm). Khi cần test, tạo order và thanh toán thủ công.
- Nếu cần tích hợp cổng thanh toán sau này (VNPay, MoMo), có thể mở rộng Payment entity và controller.

---

**Version:** 2.0  
**Last Updated:** 2024-11-12

**Changelog:**
- v2.0: Thêm tính năng payment riêng cho mỗi Enterprise
  - Tự động tạo payment riêng cho mỗi Enterprise trong đơn hàng
  - Mỗi Enterprise có QR code và thông tin thanh toán riêng
  - Hỗ trợ cấu hình ngân hàng riêng cho từng Enterprise
  - EnterpriseAdmin chỉ có thể xác nhận payment của Enterprise mình
  - Order.PaymentStatus hỗ trợ trạng thái "PartiallyPaid"

