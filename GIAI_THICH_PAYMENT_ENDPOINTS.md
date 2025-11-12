# 📖 Giải thích chi tiết Payment Endpoints

## 1️⃣ GET: api/payments/order/{orderId}

### Mục đích:
Lấy **danh sách tất cả payments** của một đơn hàng cụ thể.

### Tại sao cần endpoint này?
- Một đơn hàng có thể có **nhiều payments** (mỗi Enterprise một payment)
- Customer/EnterpriseAdmin cần xem tất cả payments của đơn hàng để theo dõi thanh toán

### Cách hoạt động:

```
GET /api/payments/order/5
Authorization: Bearer {token}
```

**Ví dụ:**
- Đơn hàng ID = 5 có sản phẩm từ 2 Enterprise:
  - Enterprise A (ID: 1) → Payment 1
  - Enterprise B (ID: 2) → Payment 2

**Response:**
```json
[
  {
    "id": 1,
    "orderId": 5,
    "enterpriseId": 1,
    "enterpriseName": "Doanh nghiệp A",
    "amount": 200000,
    "method": "BankTransfer",
    "status": "AwaitingTransfer",
    "reference": "BT-20241112101250-5-E1",
    "bankCode": "970415",
    "bankAccount": "111111111",
    "accountName": "Doanh nghiệp A",
    "qrCodeUrl": "https://img.vietqr.io/image/970415-111111111-compact.png?...",
    "notes": null,
    "createdAt": "2024-11-12T10:12:50Z",
    "paidAt": null
  },
  {
    "id": 2,
    "orderId": 5,
    "enterpriseId": 2,
    "enterpriseName": "Doanh nghiệp B",
    "amount": 150000,
    "method": "BankTransfer",
    "status": "AwaitingTransfer",
    "reference": "BT-20241112101250-5-E2",
    "bankCode": "970422",
    "bankAccount": "222222222",
    "accountName": "Doanh nghiệp B",
    "qrCodeUrl": "https://img.vietqr.io/image/970422-222222222-compact.png?...",
    "notes": null,
    "createdAt": "2024-11-12T10:12:50Z",
    "paidAt": null
  }
]
```

### Phân quyền:
- ✅ **Customer**: Xem payments của đơn hàng của mình
- ✅ **EnterpriseAdmin**: Xem payments của đơn hàng có sản phẩm từ Enterprise mình
- ✅ **SystemAdmin**: Xem tất cả payments

### Khi nào dùng?
1. **Customer** muốn xem tất cả QR codes cần thanh toán
2. **EnterpriseAdmin** muốn xem payment của Enterprise mình trong đơn hàng
3. Kiểm tra trạng thái thanh toán của tất cả Enterprise trong đơn hàng

---

## 2️⃣ POST: api/payments/{id}/status

### Mục đích:
**Xác nhận thanh toán** hoặc **hủy payment** sau khi đã nhận/không nhận được tiền.

### Tại sao cần endpoint này?
- Sau khi Customer chuyển khoản, **EnterpriseAdmin cần xác nhận** đã nhận được tiền
- Có thể hủy payment nếu có lỗi hoặc khách hàng không thanh toán

### Cách hoạt động:

```
POST /api/payments/1/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "Paid",
  "notes": "Đã nhận chuyển khoản 200,000đ từ khách hàng vào lúc 10:30"
}
```

### Request Body:
```json
{
  "status": "Paid",        // hoặc "Cancelled"
  "notes": "Ghi chú..."    // (tùy chọn)
}
```

### Response:
- **204 No Content** - Thành công (không có body)

### Phân quyền:
- ✅ **SystemAdmin**: Có thể xác nhận bất kỳ payment nào
- ✅ **EnterpriseAdmin**: Chỉ có thể xác nhận payment của Enterprise mình
- ❌ **Customer**: Không có quyền (chỉ có thể tạo payment, không thể xác nhận)

### Logic xử lý:

#### Khi set status = "Paid":
1. Cập nhật `payment.PaidAt = DateTime.UtcNow`
2. Cập nhật `payment.Status = "Paid"`
3. **Kiểm tra tất cả payments của Order:**
   - Nếu **TẤT CẢ** payments đã Paid → `order.PaymentStatus = "Paid"`
   - Nếu **MỘT SỐ** payments đã Paid → `order.PaymentStatus = "PartiallyPaid"`

**Ví dụ:**
- Đơn hàng có 2 payments:
  - Payment 1 (Enterprise A): `AwaitingTransfer`
  - Payment 2 (Enterprise B): `AwaitingTransfer`
  
- EnterpriseAdmin A xác nhận Payment 1 = `Paid`:
  - Payment 1: `Paid`
  - Payment 2: `AwaitingTransfer`
  - **Order.PaymentStatus = "PartiallyPaid"** ✅

- EnterpriseAdmin B xác nhận Payment 2 = `Paid`:
  - Payment 1: `Paid`
  - Payment 2: `Paid`
  - **Order.PaymentStatus = "Paid"** ✅

#### Khi set status = "Cancelled":
1. Xóa `payment.PaidAt = null`
2. Cập nhật `payment.Status = "Cancelled"`
3. **Kiểm tra payments còn lại:**
   - Nếu còn payment nào `Pending` hoặc `AwaitingTransfer` → `order.PaymentStatus = "Pending"`
   - Nếu tất cả payments đều bị hủy → `order.PaymentStatus = "Cancelled"`

### Khi nào dùng?

#### 1. Xác nhận đã nhận tiền (Paid):
```json
POST /api/payments/1/status
{
  "status": "Paid",
  "notes": "Đã nhận chuyển khoản 200,000đ. Mã tham chiếu: BT-20241112101250-5-E1"
}
```

**Khi nào:**
- EnterpriseAdmin đã kiểm tra tài khoản và thấy có tiền vào
- Xác nhận khách hàng đã thanh toán thành công

#### 2. Hủy payment (Cancelled):
```json
POST /api/payments/1/status
{
  "status": "Cancelled",
  "notes": "Khách hàng không thanh toán sau 7 ngày. Tự động hủy."
}
```

**Khi nào:**
- Khách hàng không thanh toán sau thời gian quy định
- Có lỗi trong quá trình thanh toán
- Khách hàng yêu cầu hủy và tạo payment mới

---

## 🔄 Luồng hoạt động thực tế:

### Scenario 1: Đơn hàng 1 Enterprise

1. **Customer tạo đơn hàng:**
   ```
   POST /api/orders
   → Order ID = 5, Status = "Pending"
   ```

2. **Customer tạo payment:**
   ```
   POST /api/payments
   {
     "orderId": 5,
     "method": "BankTransfer"
   }
   → Payment ID = 1, Status = "AwaitingTransfer"
   → Order.PaymentStatus = "AwaitingTransfer"
   ```

3. **Customer xem payments cần thanh toán:**
   ```
   GET /api/payments/order/5
   → Trả về Payment 1 với QR code
   ```

4. **Customer quét QR và chuyển khoản**

5. **EnterpriseAdmin xác nhận đã nhận tiền:**
   ```
   POST /api/payments/1/status
   {
     "status": "Paid",
     "notes": "Đã nhận 200,000đ"
   }
   → Payment 1: Status = "Paid", PaidAt = "2024-11-12T10:30:00Z"
   → Order.PaymentStatus = "Paid"
   ```

### Scenario 2: Đơn hàng nhiều Enterprise

1. **Customer tạo đơn hàng:**
   - Sản phẩm từ Enterprise A (200,000đ)
   - Sản phẩm từ Enterprise B (150,000đ)
   - Tổng: 350,000đ

2. **Customer tạo payment:**
   ```
   POST /api/payments
   {
     "orderId": 5,
     "method": "BankTransfer"
   }
   → Payment 1 (Enterprise A): 200,000đ, Status = "AwaitingTransfer"
   → Payment 2 (Enterprise B): 150,000đ, Status = "AwaitingTransfer"
   → Order.PaymentStatus = "AwaitingTransfer"
   ```

3. **Customer xem tất cả payments:**
   ```
   GET /api/payments/order/5
   → Trả về 2 payments với 2 QR codes khác nhau
   ```

4. **Customer thanh toán:**
   - Chuyển 200,000đ cho Enterprise A (QR code 1)
   - Chuyển 150,000đ cho Enterprise B (QR code 2)

5. **EnterpriseAdmin A xác nhận:**
   ```
   POST /api/payments/1/status
   {
     "status": "Paid"
   }
   → Payment 1: Status = "Paid"
   → Order.PaymentStatus = "PartiallyPaid" (vì Payment 2 chưa Paid)
   ```

6. **EnterpriseAdmin B xác nhận:**
   ```
   POST /api/payments/2/status
   {
     "status": "Paid"
   }
   → Payment 2: Status = "Paid"
   → Order.PaymentStatus = "Paid" (tất cả đã thanh toán)
   ```

---

## 📊 So sánh 2 Endpoints:

| Tính năng | GET /api/payments/order/{orderId} | POST /api/payments/{id}/status |
|-----------|-----------------------------------|--------------------------------|
| **Mục đích** | Xem danh sách payments | Xác nhận/hủy payment |
| **Method** | GET | POST |
| **Input** | orderId (trong URL) | paymentId (trong URL) + body |
| **Output** | Mảng payments | 204 No Content |
| **Ai dùng** | Customer, EnterpriseAdmin, SystemAdmin | Chỉ EnterpriseAdmin, SystemAdmin |
| **Khi nào** | Xem QR codes, kiểm tra trạng thái | Sau khi nhận/không nhận tiền |

---

## ❓ Câu hỏi thường gặp:

### Q1: Tại sao cần GET /api/payments/order/{orderId}?
**A:** Vì một đơn hàng có thể có nhiều payments (mỗi Enterprise một payment). Customer cần xem tất cả để biết phải thanh toán cho những Enterprise nào.

### Q2: Tại sao Customer không thể xác nhận payment?
**A:** Vì chỉ EnterpriseAdmin mới biết chắc chắn đã nhận được tiền trong tài khoản. Customer chỉ có thể tạo payment và chuyển khoản.

### Q3: Khi nào Order.PaymentStatus = "PartiallyPaid"?
**A:** Khi đơn hàng có nhiều payments và chỉ một số payments đã được xác nhận `Paid`, còn một số vẫn `AwaitingTransfer` hoặc `Pending`.

### Q4: EnterpriseAdmin có thể xác nhận payment của Enterprise khác không?
**A:** Không. EnterpriseAdmin chỉ có thể xác nhận payment của Enterprise mình. SystemAdmin mới có thể xác nhận tất cả.

### Q5: Có thể xác nhận payment nhiều lần không?
**A:** Có thể, nhưng không cần thiết. Nếu payment đã `Paid`, việc xác nhận lại sẽ không thay đổi gì.

---

**Hy vọng giải thích này giúp bạn hiểu rõ hơn! 🎉**

