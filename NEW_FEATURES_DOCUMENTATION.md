# 🆕 Tài Liệu Tính Năng Mới

Tài liệu này mô tả các tính năng mới đã được bổ sung vào backend để hỗ trợ frontend hoàn thiện.

---

## ✅ 1. Xóa Đơn Hàng Đã Hủy

**Vấn đề:** Customer chỉ có thể xóa đơn ở trạng thái `Pending`, không thể xóa đơn đã hủy (`Cancelled`).

**Giải pháp:** Đã cập nhật `DELETE /api/orders/{id}` để cho phép Customer xóa đơn ở trạng thái `Pending` hoặc `Cancelled`.

**API:**
```
DELETE /api/orders/{id}
Authorization: Bearer <CustomerToken>
```

**Response:**
- `204 No Content` - Xóa thành công
- `400 Bad Request` - Đơn không ở trạng thái Pending hoặc Cancelled
- `403 Forbid` - Không phải đơn hàng của bạn

---

## ✅ 2. Cập Nhật Profile Người Dùng

**Vấn đề:** Không có endpoint cho user tự cập nhật profile của mình.

**Giải pháp:** Đã thêm 2 endpoint mới:
- `GET /api/users/me` - Lấy thông tin user hiện tại
- `PUT /api/users/me` - Cập nhật profile (tên, email, mật khẩu)

**API:**

### GET /api/users/me
```
GET /api/users/me
Authorization: Bearer <Token>
```

**Response:**
```json
{
  "id": 1,
  "name": "Nguyễn Văn A",
  "email": "user@example.com",
  "role": "Customer",
  "enterpriseId": null,
  "enterprise": null
}
```

### PUT /api/users/me
```
PUT /api/users/me
Authorization: Bearer <Token>
Content-Type: application/json

{
  "name": "Nguyễn Văn B",
  "email": "newemail@example.com",  // Optional
  "password": "newpassword123"       // Optional
}
```

**Response:** Trả về `UserDto` đã cập nhật.

**Lưu ý:**
- Email chỉ được cập nhật nếu khác email hiện tại và chưa được sử dụng
- Mật khẩu là optional, chỉ cập nhật khi có giá trị

---

## ✅ 3. Cập Nhật Địa Chỉ Giao Hàng

**Vấn đề:** Không có cách nào để Customer cập nhật địa chỉ giao hàng sau khi đặt.

**Giải pháp:** Đã thêm endpoint `PUT /api/orders/{id}/shipping-address`.

**API:**
```
PUT /api/orders/{id}/shipping-address
Authorization: Bearer <CustomerToken>
Content-Type: application/json

{
  "shippingAddress": "123 Đường ABC, Phường XYZ, Quận 1, TP.HCM"
}
```

**Response:**
- `204 No Content` - Cập nhật thành công
- `400 Bad Request` - Đơn không ở trạng thái Pending hoặc Processing
- `403 Forbid` - Không phải đơn hàng của bạn

**Lưu ý:** Chỉ cho phép cập nhật khi đơn hàng ở trạng thái `Pending` hoặc `Processing`.

---

## ✅ 4. Upload Hình Ảnh

**Vấn đề:** Backend chưa hỗ trợ upload hình ảnh.

**Giải pháp:** Đã thêm `FileUploadController` với 2 endpoint:
- `POST /api/fileupload/image` - Upload 1 hình ảnh
- `POST /api/fileupload/images` - Upload nhiều hình ảnh (tối đa 10)

**API:**

### Upload 1 hình ảnh
```
POST /api/fileupload/image
Authorization: Bearer <Token>
Content-Type: multipart/form-data

file: <image file>
```

**Response:**
```json
{
  "success": true,
  "message": "Upload hình ảnh thành công.",
  "imageUrl": "https://yourdomain.com/uploads/images/guid.jpg",
  "fileName": "guid.jpg"
}
```

### Upload nhiều hình ảnh
```
POST /api/fileupload/images
Authorization: Bearer <Token>
Content-Type: multipart/form-data

files: [<image1>, <image2>, ...]
```

**Response:**
```json
{
  "success": true,
  "uploadedFiles": [
    {
      "fileName": "image1.jpg",
      "imageUrl": "https://yourdomain.com/uploads/images/guid1.jpg",
      "size": 123456
    }
  ],
  "errors": [],
  "totalUploaded": 1,
  "totalFailed": 0
}
```

**Giới hạn:**
- Định dạng: JPG, JPEG, PNG, GIF, WEBP
- Kích thước tối đa: 10MB/file
- Upload nhiều: Tối đa 10 files/lần

**Lưu ý:**
- Files được lưu trong `wwwroot/uploads/images/`
- Cần cấu hình static files trong `Program.cs` (đã có)
- URL trả về có thể dùng trực tiếp trong frontend

---

## ✅ 5. Role Shipper & Luồng Giao Hàng

**Vấn đề:** Chưa có role Shipper và luồng giao hàng hoàn chỉnh.

**Giải pháp:** Đã thêm:
- Role `Shipper` trong hệ thống
- Model `Order` có thêm các trường: `ShipperId`, `ShippedAt`, `DeliveredAt`, `DeliveryNotes`
- Controller `ShippersController` với các endpoint quản lý giao hàng

### Luồng Giao Hàng:

1. **EnterpriseAdmin/SystemAdmin gán đơn cho Shipper:**
   ```
   POST /api/shippers/orders/{orderId}/assign
   Authorization: Bearer <EnterpriseAdminToken>
   Content-Type: application/json

   {
     "shipperId": 5
   }
   ```
   - Đơn phải ở trạng thái `Processing`
   - Shipper phải có role `Shipper`

2. **Shipper xem danh sách đơn cần giao:**
   ```
   GET /api/shippers/orders
   Authorization: Bearer <ShipperToken>
   ```
   - Chỉ thấy đơn được gán cho mình
   - Trạng thái: `Processing` hoặc `Shipped`

3. **Shipper xác nhận bắt đầu giao hàng:**
   ```
   POST /api/shippers/orders/{orderId}/ship
   Authorization: Bearer <ShipperToken>
   ```
   - Cập nhật status: `Processing` → `Shipped`
   - Ghi lại `ShippedAt`

4. **Shipper xác nhận giao hàng thành công:**
   ```
   POST /api/shippers/orders/{orderId}/deliver
   Authorization: Bearer <ShipperToken>
   Content-Type: application/json

   {
     "notes": "Khách hàng đã nhận hàng và thanh toán COD"
   }
   ```
   - Cập nhật status: `Shipped` → `Completed`
   - Ghi lại `DeliveredAt`
   - Nếu là COD, tự động cập nhật payment status thành `Paid`

### Các Trường Mới Trong OrderDto:

```json
{
  "shipperId": 5,
  "shippedAt": "2024-11-13T10:00:00Z",
  "deliveredAt": "2024-11-13T14:30:00Z",
  "deliveryNotes": "Giao hàng thành công"
}
```

---

## ✅ 6. Cải Thiện Payment Flow

**Vấn đề:** Payment flow đã có nhưng cần kiểm tra lại.

**Giải pháp:** Payment flow hiện tại đã đầy đủ:
- `POST /api/payments` - Tạo payment (tự động tạo QR code cho BankTransfer)
- `GET /api/payments/{id}` - Chi tiết payment
- `GET /api/payments/order/{orderId}` - Danh sách payment của đơn
- `POST /api/payments/{id}/status` - Xác nhận thanh toán

**Lưu ý:**
- Payment đã hỗ trợ QR code qua VietQR
- Mỗi Enterprise có payment riêng trong đơn hàng
- Payment status tự động cập nhật Order.PaymentStatus

---

## 📋 Tổng Hợp API Mới

| Endpoint | Method | Role | Mô tả |
|----------|--------|------|-------|
| `/api/users/me` | GET | All | Lấy thông tin user hiện tại |
| `/api/users/me` | PUT | All | Cập nhật profile |
| `/api/orders/{id}/shipping-address` | PUT | Customer | Cập nhật địa chỉ giao hàng |
| `/api/fileupload/image` | POST | All | Upload 1 hình ảnh |
| `/api/fileupload/images` | POST | All | Upload nhiều hình ảnh |
| `/api/shippers/orders` | GET | Shipper/Admin | Danh sách đơn cần giao |
| `/api/shippers/orders/{id}/assign` | POST | EnterpriseAdmin/SystemAdmin | Gán đơn cho Shipper |
| `/api/shippers/orders/{id}/ship` | POST | Shipper | Xác nhận bắt đầu giao |
| `/api/shippers/orders/{id}/deliver` | POST | Shipper | Xác nhận giao hàng thành công |

---

## 🔄 Migration Cần Chạy

Sau khi pull code mới, cần chạy migration:

```bash
dotnet ef database update
```

Migration mới:
- `AddShipperAndDeliveryFields` - Thêm các trường Shipper vào Order

---

## 🎯 Checklist Tích Hợp Frontend

- [ ] **Xóa đơn hàng:** Thêm nút "Xóa" cho đơn `Pending` và `Cancelled`
- [ ] **Profile:** Tạo trang `/profile` với form cập nhật (GET/PUT `/api/users/me`)
- [ ] **Địa chỉ giao hàng:** Thêm nút "Sửa địa chỉ" trên trang chi tiết đơn (PUT `/api/orders/{id}/shipping-address`)
- [ ] **Upload hình ảnh:** Tích hợp upload cho sản phẩm, doanh nghiệp, avatar (POST `/api/fileupload/image`)
- [ ] **Shipper Dashboard:** Tạo trang quản lý đơn hàng cho Shipper
- [ ] **Gán đơn cho Shipper:** Thêm chức năng gán đơn trong trang quản lý đơn của EnterpriseAdmin
- [ ] **Luồng giao hàng:** Hiển thị trạng thái giao hàng và nút xác nhận cho Shipper

---

## 📝 Lưu Ý Quan Trọng

1. **Static Files:** Đảm bảo thư mục `wwwroot/uploads/images/` tồn tại và có quyền ghi
2. **Shipper Role:** Cần tạo user với role `Shipper` qua SystemAdmin hoặc seed data
3. **Payment Flow:** Đã hoàn chỉnh, chỉ cần frontend tích hợp UI
4. **CORS:** Đảm bảo cấu hình CORS cho phép frontend upload file

---

**Cập nhật lần cuối:** 2024-11-13

