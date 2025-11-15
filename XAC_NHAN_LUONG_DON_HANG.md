# ✅ Xác Nhận Luồng Đơn Hàng Hoàn Chỉnh

## Luồng: Customer → Xem sản phẩm → Đặt hàng → Thanh toán → Doanh nghiệp xử lý → Giao hàng → Hoàn tất

---

## ✅ BƯỚC 1: Customer Xem Sản Phẩm

### Endpoints:
- ✅ **GET /api/products** - Xem danh sách sản phẩm
  - **Trạng thái:** Đã cho phép xem công khai (`[AllowAnonymous]`)
  - **Chức năng:** Customer có thể xem tất cả sản phẩm mà không cần đăng nhập
  - **Lưu ý:** Nếu EnterpriseAdmin đã đăng nhập, chỉ xem sản phẩm của Enterprise mình

- ✅ **GET /api/products/{id}** - Xem chi tiết sản phẩm
  - **Trạng thái:** Đã cho phép xem công khai (`[AllowAnonymous]`)
  - **Chức năng:** Customer có thể xem chi tiết sản phẩm mà không cần đăng nhập

### Kết luận: ✅ **HOÀN CHỈNH**

---

## ✅ BƯỚC 2: Customer Đặt Hàng

### Endpoint:
- ✅ **POST /api/orders** - Tạo đơn hàng mới
  - **Yêu cầu:** `[Authorize(Roles = "Customer")]` - Chỉ Customer mới tạo được
  - **Validation:**
    - ✅ ShippingAddress bắt buộc
    - ✅ Items không rỗng
    - ✅ Quantity > 0
    - ✅ Product tồn tại
    - ✅ StockStatus != "OutOfStock" (không cho đặt hàng sản phẩm hết hàng)
  - **Tự động:**
    - ✅ Tính TotalAmount
    - ✅ Set Status = "Pending"
    - ✅ Set PaymentStatus = "Pending"
    - ✅ Tạo OrderItems

### Kết luận: ✅ **HOÀN CHỈNH**

---

## ✅ BƯỚC 3: Customer Thanh Toán

### Endpoint:
- ✅ **POST /api/payments** - Tạo thanh toán
  - **Yêu cầu:** `[Authorize]` - Customer phải đăng nhập
  - **Chức năng:**
    - ✅ Tự động tạo payment riêng cho mỗi Enterprise trong đơn hàng
    - ✅ Tính amount riêng cho từng Enterprise
    - ✅ Hỗ trợ 2 phương thức:
      - **COD** (Cash on Delivery) - Thanh toán khi nhận hàng
      - **BankTransfer** - Chuyển khoản với QR code
    - ✅ Tự động tạo QR code (nếu BankTransfer)
    - ✅ Sử dụng thông tin ngân hàng của Enterprise (nếu có) hoặc global settings
  - **Cập nhật Order.PaymentStatus:**
    - ✅ Tất cả BankTransfer → "AwaitingTransfer"
    - ✅ Tất cả COD → "Pending"
    - ✅ Có cả 2 → "AwaitingTransfer" (ưu tiên)

### Kết luận: ✅ **HOÀN CHỈNH**

---

## ✅ BƯỚC 4: Doanh Nghiệp Xử Lý

### Endpoints:

1. ✅ **GET /api/orders** - Xem danh sách đơn hàng
   - **Quyền:** EnterpriseAdmin chỉ xem được đơn hàng có sản phẩm từ Enterprise mình
   - **Lọc:** Tự động lọc theo EnterpriseId

2. ✅ **GET /api/orders/{id}** - Xem chi tiết đơn hàng
   - **Quyền:** EnterpriseAdmin chỉ xem được đơn hàng có sản phẩm từ Enterprise mình
   - **Dữ liệu:** Bao gồm OrderItems, Payments, thông tin Enterprise

3. ✅ **PUT /api/orders/{id}/status** - Cập nhật trạng thái đơn hàng
   - **Quyền:** EnterpriseAdmin chỉ cập nhật đơn hàng có sản phẩm từ Enterprise mình
   - **Trạng thái cho phép:**
     - ✅ `Pending` → `Processing` (Bắt đầu xử lý)
     - ✅ `Processing` → `Shipped` (Đã gửi hàng)
     - ✅ `Shipped` → `Completed` (Hoàn thành)
     - ❌ `Cancelled` - EnterpriseAdmin KHÔNG thể hủy (chỉ Customer mới hủy được)

4. ✅ **POST /api/payments/{id}/status** - Xác nhận thanh toán
   - **Quyền:** EnterpriseAdmin chỉ xác nhận payment của Enterprise mình
   - **Trạng thái:** `Paid` hoặc `Cancelled`
   - **Tự động cập nhật Order.PaymentStatus:**
     - ✅ Tất cả Paid → "Paid"
     - ✅ Một số Paid → "PartiallyPaid"

### Luồng xử lý:
```
1. EnterpriseAdmin nhận đơn hàng mới (Status = "Pending")
   ↓
2. EnterpriseAdmin cập nhật Status = "Processing" (Bắt đầu xử lý)
   ↓
3. EnterpriseAdmin chuẩn bị hàng hóa
   ↓
4. EnterpriseAdmin cập nhật Status = "Shipped" (Đã gửi hàng)
   ↓
5. Sau khi khách nhận hàng và thanh toán:
   - Nếu BankTransfer: EnterpriseAdmin xác nhận Payment = "Paid"
   - Nếu COD: Tự động thanh toán khi nhận hàng
   ↓
6. EnterpriseAdmin cập nhật Status = "Completed" (Hoàn thành)
```

### Kết luận: ✅ **HOÀN CHỈNH**

---

## ✅ BƯỚC 5: Giao Hàng

### Trạng thái:
- ✅ **"Shipped"** (Đã gửi hàng)
- ✅ EnterpriseAdmin có thể set status = "Shipped" sau khi gửi hàng
- ✅ Customer có thể xem trạng thái đơn hàng qua `GET /api/orders/{id}`
- ✅ Trạng thái "Shipped" nằm trong luồng: `Pending → Processing → Shipped → Completed`

### Kết luận: ✅ **HOÀN CHỈNH**

---

## ✅ BƯỚC 6: Hoàn Tất

### Trạng thái:
- ✅ **"Completed"** (Hoàn thành)
- ✅ EnterpriseAdmin có thể set status = "Completed" sau khi khách nhận hàng và thanh toán
- ✅ Customer có thể xem trạng thái đơn hàng đã hoàn thành
- ✅ Trạng thái "Completed" là trạng thái cuối cùng trong luồng

### Kết luận: ✅ **HOÀN CHỈNH**

---

## 📊 Tổng Kết

### ✅ Tất Cả Các Bước Đã Hoàn Chỉnh:

| Bước | Mô tả | Trạng thái | Endpoint |
|------|-------|------------|----------|
| 1 | Customer xem sản phẩm | ✅ Hoàn chỉnh | `GET /api/products`<br>`GET /api/products/{id}` |
| 2 | Customer đặt hàng | ✅ Hoàn chỉnh | `POST /api/orders` |
| 3 | Customer thanh toán | ✅ Hoàn chỉnh | `POST /api/payments` |
| 4 | Doanh nghiệp xử lý | ✅ Hoàn chỉnh | `GET /api/orders`<br>`PUT /api/orders/{id}/status`<br>`POST /api/payments/{id}/status` |
| 5 | Giao hàng | ✅ Hoàn chỉnh | Status: "Shipped" |
| 6 | Hoàn tất | ✅ Hoàn chỉnh | Status: "Completed" |

---

## 🔄 Luồng Hoàn Chỉnh (Visual)

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Customer Xem Sản Phẩm (Không cần đăng nhập)            │
│    GET /api/products                                        │
│    GET /api/products/{id}                                   │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Customer Đặt Hàng (Cần đăng nhập)                       │
│    POST /api/orders                                          │
│    → Status: "Pending"                                       │
│    → PaymentStatus: "Pending"                                │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Customer Thanh Toán (Cần đăng nhập)                     │
│    POST /api/payments                                        │
│    → Tạo payment riêng cho mỗi Enterprise                   │
│    → PaymentStatus: "AwaitingTransfer" (BankTransfer)       │
│       hoặc "Pending" (COD)                                  │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. Doanh Nghiệp Xử Lý                                       │
│    GET /api/orders → Xem đơn hàng                           │
│    PUT /api/orders/{id}/status → "Processing"              │
│    → Chuẩn bị hàng hóa                                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. Giao Hàng                                                │
│    PUT /api/orders/{id}/status → "Shipped"                  │
│    → Đã gửi hàng                                            │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. Hoàn Tất                                                 │
│    POST /api/payments/{id}/status → "Paid" (nếu BankTransfer)│
│    PUT /api/orders/{id}/status → "Completed"               │
│    → Đơn hàng hoàn thành                                    │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎯 Kết Luận Cuối Cùng

### ✅ **DỰ ÁN ĐÃ PHÙ HỢP 100% VỚI LUỒNG:**

**Customer → Xem sản phẩm → Đặt hàng → Thanh toán → Doanh nghiệp xử lý → Giao hàng → Hoàn tất**

### ✅ Tất Cả Các Bước Đã Được Triển Khai:
1. ✅ Customer có thể xem sản phẩm công khai (không cần đăng nhập)
2. ✅ Customer có thể đặt hàng với validation đầy đủ
3. ✅ Customer có thể thanh toán (COD hoặc BankTransfer)
4. ✅ EnterpriseAdmin có thể xử lý đơn hàng đầy đủ
5. ✅ Có trạng thái "Shipped" để đánh dấu giao hàng
6. ✅ Có trạng thái "Completed" để đánh dấu hoàn tất

### ✅ Các Tính Năng Bổ Sung:
- ✅ Payment riêng cho mỗi Enterprise
- ✅ QR code tự động tạo cho BankTransfer
- ✅ Validation StockStatus (không cho đặt hàng sản phẩm hết hàng)
- ✅ Phân quyền rõ ràng (Customer, EnterpriseAdmin, SystemAdmin)
- ✅ Logic đồng bộ PaymentStatus với Payments

---

## 🚀 **DỰ ÁN SẴN SÀNG ĐỂ TEST VÀ DEPLOY!**

**Ngày xác nhận:** 2024-11-12




