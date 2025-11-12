# 🏢 Quản lý đơn hàng cho EnterpriseAdmin

## Tổng quan

EnterpriseAdmin có thể xem và xử lý đơn hàng có sản phẩm từ Enterprise của mình.

---

## 🔹 Quyền truy cập

### 1. **Xem danh sách đơn hàng**
- **Endpoint:** `GET /api/orders`
- **Quyền:** EnterpriseAdmin chỉ xem được đơn hàng có sản phẩm từ Enterprise của mình
- **Lọc:** Tự động lọc theo `EnterpriseId` của EnterpriseAdmin

### 2. **Xem chi tiết đơn hàng**
- **Endpoint:** `GET /api/orders/{id}`
- **Quyền:** EnterpriseAdmin chỉ xem được đơn hàng có sản phẩm từ Enterprise của mình
- **Kiểm tra:** Hệ thống tự động kiểm tra quyền truy cập

### 3. **Cập nhật trạng thái đơn hàng**
- **Endpoint:** `PUT /api/orders/{id}/status`
- **Quyền:** EnterpriseAdmin chỉ có thể cập nhật đơn hàng có sản phẩm từ Enterprise của mình
- **Trạng thái cho phép:**
  - ✅ `Pending` - Đơn hàng mới
  - ✅ `Processing` - Đang xử lý
  - ✅ `Shipped` - Đã gửi hàng
  - ✅ `Completed` - Hoàn thành
  - ❌ `Cancelled` - Chỉ Customer mới có thể hủy

### 4. **Xác nhận thanh toán**
- **Endpoint:** `POST /api/payments/{id}/status`
- **Quyền:** EnterpriseAdmin chỉ có thể xác nhận payment của Enterprise mình
- **Trạng thái:** `Paid` hoặc `Cancelled`

---

## 🔹 Các trạng thái đơn hàng

| Trạng thái | Mô tả | Ai có thể set |
|------------|-------|---------------|
| `Pending` | Đơn hàng mới, chưa xử lý | EnterpriseAdmin, SystemAdmin |
| `Processing` | Đang xử lý đơn hàng | EnterpriseAdmin, SystemAdmin |
| `Shipped` | Đã gửi hàng | EnterpriseAdmin, SystemAdmin |
| `Completed` | Đơn hàng hoàn thành | EnterpriseAdmin, SystemAdmin |
| `Cancelled` | Đơn hàng bị hủy | Chỉ Customer (khi status = Pending) |

---

## 🔹 Luồng xử lý đơn hàng (EnterpriseAdmin)

1. **Nhận đơn hàng mới:**
   - Đơn hàng có status = `Pending`
   - EnterpriseAdmin xem danh sách đơn hàng → Thấy đơn hàng mới

2. **Bắt đầu xử lý:**
   - EnterpriseAdmin cập nhật status = `Processing`
   - Chuẩn bị hàng hóa

3. **Gửi hàng:**
   - EnterpriseAdmin cập nhật status = `Shipped`
   - Thông báo cho khách hàng

4. **Hoàn thành:**
   - Sau khi khách nhận hàng và thanh toán
   - EnterpriseAdmin cập nhật status = `Completed`

5. **Xác nhận thanh toán:**
   - Nếu payment method = `BankTransfer`
   - EnterpriseAdmin xác nhận payment = `Paid` khi nhận được chuyển khoản

---

## 🔹 API Examples

### 1. Xem danh sách đơn hàng
```http
GET /api/orders
Authorization: Bearer {token}
```

**Response:**
```json
[
  {
    "id": 1,
    "userId": 5,
    "orderDate": "2024-11-12T10:00:00Z",
    "shippingAddress": "123 Đường ABC",
    "totalAmount": 500000,
    "status": "Pending",
    "paymentMethod": "BankTransfer",
    "paymentStatus": "AwaitingTransfer",
    "orderItems": [
      {
        "id": 1,
        "productId": 1,
        "quantity": 2,
        "price": 250000
      }
    ],
    "payments": [
      {
        "id": 1,
        "enterpriseId": 1,
        "enterpriseName": "Doanh nghiệp A",
        "amount": 500000,
        "method": "BankTransfer",
        "status": "AwaitingTransfer",
        "qrCodeUrl": "..."
      }
    ]
  }
]
```

### 2. Xem chi tiết đơn hàng
```http
GET /api/orders/1
Authorization: Bearer {token}
```

### 3. Cập nhật trạng thái đơn hàng
```http
PUT /api/orders/1/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "Processing"
}
```

**Các trạng thái có thể dùng:**
- `"Pending"` - Đơn hàng mới
- `"Processing"` - Đang xử lý
- `"Shipped"` - Đã gửi hàng
- `"Completed"` - Hoàn thành

**Lưu ý:** EnterpriseAdmin không thể set `"Cancelled"` (chỉ Customer mới có thể hủy)

### 4. Xác nhận thanh toán
```http
POST /api/payments/1/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "Paid",
  "notes": "Đã nhận chuyển khoản 500,000đ"
}
```

---

## 🔹 Kiểm tra quyền

Hệ thống tự động kiểm tra:
1. EnterpriseAdmin có thuộc Enterprise nào không
2. Đơn hàng có sản phẩm từ Enterprise của EnterpriseAdmin không
3. Payment có thuộc Enterprise của EnterpriseAdmin không

**Lỗi có thể gặp:**
- `403 Forbid: "EnterpriseAdmin không thuộc Enterprise nào."`
- `403 Forbid: "Bạn chỉ có thể xem/cập nhật đơn hàng có sản phẩm của doanh nghiệp mình."`
- `403 Forbid: "Bạn chỉ có thể cập nhật thanh toán của doanh nghiệp của mình."`
- `403 Forbid: "EnterpriseAdmin không thể hủy đơn hàng. Chỉ Customer mới có thể hủy đơn hàng."`

**Lưu ý về hủy đơn hàng:**
- Customer chỉ có thể hủy đơn hàng khi status = `"Pending"` (EnterpriseAdmin chưa xử lý)
- Nếu EnterpriseAdmin đã xử lý (status = `"Processing"`, `"Shipped"`, hoặc `"Completed"`), Customer không thể hủy nữa
- Thông báo lỗi: `"Không thể hủy đơn hàng. Đơn hàng đã được doanh nghiệp xử lý (trạng thái: ...)."`

---

## 🔹 Best Practices

1. **Cập nhật trạng thái đúng thời điểm:**
   - `Processing` khi bắt đầu chuẩn bị hàng
   - `Shipped` khi đã gửi hàng
   - `Completed` khi khách đã nhận hàng và thanh toán

2. **Xác nhận thanh toán:**
   - Kiểm tra kỹ trước khi xác nhận `Paid`
   - Thêm `notes` để ghi chú thông tin thanh toán

3. **Theo dõi đơn hàng:**
   - Thường xuyên kiểm tra danh sách đơn hàng
   - Xử lý đơn hàng `Pending` sớm nhất có thể

---

**Version:** 1.0  
**Last Updated:** 2024-11-12

