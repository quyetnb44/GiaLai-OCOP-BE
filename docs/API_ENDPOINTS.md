# 📚 API ENDPOINTS - GiaLai OCOP Backend

## 📌 Base URL

- **Production**: `https://gialai-ocop-be.onrender.com/api`
- **Local**: `http://localhost:5003/api`

---

## 🔐 Authentication (`/api/auth`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| POST | `/register` | Đăng ký tài khoản mới | ❌ | - |
| POST | `/register-with-otp` | Đăng ký với xác thực OTP | ❌ | - |
| POST | `/login` | Đăng nhập | ❌ | - |
| POST | `/login-with-otp` | Đăng nhập với OTP | ❌ | - |
| POST | `/change-password` | Đổi mật khẩu | ✅ | All |
| POST | `/forgot-password` | Quên mật khẩu (gửi OTP) | ❌ | - |
| POST | `/reset-password` | Đặt lại mật khẩu | ❌ | - |
| POST | `/send-verification-otp` | Gửi OTP xác thực email | ❌ | - |
| POST | `/verify-email` | Xác thực email | ❌ | - |
| POST | `/google-login` | Đăng nhập bằng Google | ❌ | - |
| POST | `/facebook-login` | Đăng nhập bằng Facebook | ❌ | - |
| POST | `/google-register` | Đăng ký bằng Google | ❌ | - |
| POST | `/facebook-register` | Đăng ký bằng Facebook | ❌ | - |

---

## 👤 Users (`/api/users`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/` | Lấy danh sách users | ✅ | SystemAdmin |
| GET | `/{id}` | Lấy thông tin user theo ID | ✅ | All |
| PUT | `/{id}` | Cập nhật user | ✅ | All (own profile) |
| DELETE | `/{id}` | Xóa user (soft delete) | ✅ | SystemAdmin |
| GET | `/me` | Lấy thông tin user hiện tại | ✅ | All |
| PUT | `/me` | Cập nhật thông tin cá nhân | ✅ | All |
| GET | `/my-customers` | Lấy danh sách khách hàng của doanh nghiệp | ✅ | EnterpriseAdmin |
| POST | `/create-enterprise-admin` | Tạo tài khoản EnterpriseAdmin | ✅ | SystemAdmin |

---

## 📦 Products (`/api/products`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/` | Lấy danh sách sản phẩm (filter, search, pagination) | ❌ | - |
| GET | `/{id}` | Lấy chi tiết sản phẩm | ❌ | - |
| POST | `/` | Tạo sản phẩm mới | ✅ | EnterpriseAdmin |
| PUT | `/{id}` | Cập nhật sản phẩm | ✅ | EnterpriseAdmin, SystemAdmin |
| DELETE | `/{id}` | Xóa sản phẩm | ✅ | EnterpriseAdmin, SystemAdmin |
| PUT | `/{id}/status` | Cập nhật trạng thái sản phẩm (approve/reject) | ✅ | SystemAdmin |

---

## 🛒 Orders (`/api/orders`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/` | Lấy danh sách đơn hàng | ✅ | All |
| GET | `/{id}` | Lấy chi tiết đơn hàng | ✅ | All |
| POST | `/` | Tạo đơn hàng mới | ✅ | Customer |
| PUT | `/{id}/status` | Cập nhật trạng thái đơn hàng | ✅ | EnterpriseAdmin, SystemAdmin |
| PUT | `/{id}/cancel` | Hủy đơn hàng | ✅ | Customer |
| PUT | `/{id}/accept` | Chấp nhận đơn hàng | ✅ | EnterpriseAdmin |
| PUT | `/{id}/request-completion` | Yêu cầu hoàn thành đơn hàng | ✅ | EnterpriseAdmin |
| PUT | `/{id}/approve-completion` | Duyệt hoàn thành đơn hàng | ✅ | SystemAdmin |
| PUT | `/{id}/confirm-bank-transfer` | Xác nhận chuyển khoản | ✅ | SystemAdmin |

---

## 🏢 Enterprises (`/api/enterprises`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/` | Lấy danh sách doanh nghiệp | ❌ | - |
| GET | `/{id}` | Lấy chi tiết doanh nghiệp | ❌ | - |
| POST | `/` | Tạo doanh nghiệp mới | ✅ | SystemAdmin |
| PUT | `/{id}` | Cập nhật doanh nghiệp | ✅ | EnterpriseAdmin (own), SystemAdmin |
| DELETE | `/{id}` | Xóa doanh nghiệp | ✅ | SystemAdmin |
| GET | `/my-enterprise` | Lấy doanh nghiệp của user hiện tại | ✅ | EnterpriseAdmin |
| PUT | `/my-enterprise/settings` | Cập nhật cài đặt doanh nghiệp | ✅ | EnterpriseAdmin |

---

## 📁 Categories (`/api/categories`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/` | Lấy danh sách danh mục | ❌ | - |
| GET | `/{id}` | Lấy chi tiết danh mục | ❌ | - |
| POST | `/` | Tạo danh mục mới | ✅ | SystemAdmin |
| PUT | `/{id}` | Cập nhật danh mục | ✅ | SystemAdmin |
| DELETE | `/{id}` | Xóa danh mục | ✅ | SystemAdmin |

---

## 💳 Payments (`/api/payments`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/` | Lấy danh sách thanh toán | ✅ | All |
| GET | `/{id}` | Lấy chi tiết thanh toán | ✅ | All |
| POST | `/` | Tạo thanh toán cho đơn hàng | ✅ | Customer |
| PUT | `/{id}/status` | Cập nhật trạng thái thanh toán | ✅ | EnterpriseAdmin, SystemAdmin |
| GET | `/order/{orderId}` | Lấy thanh toán theo đơn hàng | ✅ | All |

---

## 💰 Wallet (`/api/wallet`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/balance` | Xem số dư ví | ✅ | All |
| GET | `/transactions` | Xem lịch sử giao dịch | ✅ | All |
| POST | `/deposit` | Nạp tiền vào ví (tạo QR) | ✅ | All |
| POST | `/pay` | Thanh toán đơn hàng bằng ví | ✅ | Customer |
| POST | `/withdraw` | Rút tiền từ ví | ✅ | All |
| GET | `/summary` | Thống kê tổng quan ví | ✅ | SystemAdmin |
| GET | `/all` | Lấy tất cả ví người dùng | ✅ | SystemAdmin |
| POST | `/ensure-all-wallets` | Đảm bảo tất cả user có ví | ✅ | SystemAdmin |
| PUT | `/{userId}/balance` | Cập nhật số dư ví thủ công | ✅ | SystemAdmin |

---

## 📝 Wallet Requests (`/api/walletrequest`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/` | Lấy danh sách yêu cầu | ✅ | All |
| POST | `/` | Tạo yêu cầu nạp/rút tiền | ✅ | Customer, EnterpriseAdmin |
| PUT | `/{id}/process` | Xử lý yêu cầu (approve/reject) | ✅ | SystemAdmin |
| GET | `/pending-count` | Đếm số yêu cầu đang chờ | ✅ | SystemAdmin |
| GET | `/{id}` | Lấy chi tiết yêu cầu | ✅ | All |

---

## 🏦 Bank Accounts (`/api/bankaccount`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/` | Lấy danh sách tài khoản ngân hàng | ✅ | All |
| GET | `/{id}` | Lấy chi tiết tài khoản | ✅ | All |
| POST | `/` | Tạo tài khoản ngân hàng | ✅ | Customer, EnterpriseAdmin |
| PUT | `/{id}` | Cập nhật tài khoản | ✅ | Customer, EnterpriseAdmin |
| DELETE | `/{id}` | Xóa tài khoản | ✅ | All |
| PUT | `/{id}/set-default` | Đặt làm tài khoản mặc định | ✅ | Customer, EnterpriseAdmin |

---

## 🏦 Enterprise Bank Info (`/api/enterprise-bank-info`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/my-enterprise` | Lấy thông tin bank của doanh nghiệp | ✅ | EnterpriseAdmin |
| POST | `/my-enterprise` | Tạo/cập nhật thông tin bank | ✅ | EnterpriseAdmin |
| PUT | `/my-enterprise` | Cập nhật thông tin bank | ✅ | EnterpriseAdmin |
| GET | `/enterprise/{enterpriseId}` | Lấy thông tin bank theo enterprise ID | ❌ | - |

---

## ⭐ Reviews (`/api/reviews`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/product/{productId}` | Lấy đánh giá của sản phẩm | ✅ | All |
| POST | `/` | Tạo đánh giá mới | ✅ | Customer |
| PUT | `/{id}` | Cập nhật đánh giá | ✅ | Customer (own) |
| DELETE | `/{id}` | Xóa đánh giá | ✅ | Customer (own), SystemAdmin |
| GET | `/my-reviews` | Lấy đánh giá của user hiện tại | ✅ | All |

---

## 🔔 Notifications (`/api/notifications`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/` | Lấy danh sách thông báo | ✅ | All |
| PUT | `/{id}/read` | Đánh dấu đã đọc | ✅ | All |
| PUT | `/read-all` | Đánh dấu tất cả đã đọc | ✅ | All |
| DELETE | `/{id}` | Xóa thông báo | ✅ | All |

---

## 📍 Shipping Addresses (`/api/shipping-addresses`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/` | Lấy danh sách địa chỉ | ✅ | All |
| GET | `/{id}` | Lấy chi tiết địa chỉ | ✅ | All |
| POST | `/` | Tạo địa chỉ mới | ✅ | All |
| PUT | `/{id}` | Cập nhật địa chỉ | ✅ | All |
| DELETE | `/{id}` | Xóa địa chỉ | ✅ | All |
| PUT | `/{id}/set-default` | Đặt làm địa chỉ mặc định | ✅ | All |

---

## 🗺️ Map (`/api/map`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/enterprises` | Tìm kiếm doanh nghiệp trên bản đồ | ❌ | - |
| GET | `/enterprises/search` | Tìm kiếm theo keyword | ❌ | - |
| GET | `/enterprises/bbox` | Tìm trong bounding box | ❌ | - |
| GET | `/enterprises/nearby` | Tìm doanh nghiệp gần vị trí | ❌ | - |
| GET | `/enterprises/{id}` | Lấy chi tiết doanh nghiệp | ❌ | - |
| GET | `/enterprises/{id}/products` | Lấy sản phẩm của doanh nghiệp | ❌ | - |
| GET | `/filter-options` | Lấy tùy chọn filter (districts, provinces, etc.) | ❌ | - |

---

## 📍 Locations (`/api/locations`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/` | Lấy danh sách locations | ✅ | SystemAdmin |
| GET | `/{id}` | Lấy chi tiết location | ✅ | SystemAdmin |
| POST | `/` | Tạo location mới | ✅ | SystemAdmin |
| PUT | `/{id}` | Cập nhật location | ✅ | SystemAdmin |
| DELETE | `/{id}` | Xóa location | ✅ | SystemAdmin |
| GET | `/provinces` | Lấy danh sách tỉnh (từ API bên ngoài) | ❌ | - |

---

## 📍 Address (`/api/address`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/provinces` | Lấy danh sách tỉnh/thành phố | ❌ | - |
| GET | `/districts/{provinceId}` | Lấy danh sách quận/huyện theo tỉnh | ❌ | - |
| GET | `/wards/{districtId}` | Lấy danh sách phường/xã theo quận | ❌ | - |

---

## 📤 File Upload (`/api/fileupload`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| POST | `/image` | Upload hình ảnh (Cloudinary) | ✅ | All |
| POST | `/document` | Upload tài liệu (local storage) | ✅ | All |
| DELETE | `/image` | Xóa hình ảnh | ✅ | All |

---

## 👤 Profile (`/api/profile`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/avatar` | Lấy avatar | ✅ | Customer |
| POST | `/avatar` | Upload avatar | ✅ | Customer |
| PUT | `/avatar` | Cập nhật avatar | ✅ | Customer |
| DELETE | `/avatar` | Xóa avatar | ✅ | Customer |

---

## 📊 Inventory (`/api/inventory`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/product/{productId}/history` | Lấy lịch sử kho của sản phẩm | ✅ | EnterpriseAdmin, SystemAdmin |
| POST | `/product/{productId}/adjust` | Điều chỉnh số lượng kho | ✅ | EnterpriseAdmin, SystemAdmin |

---

## 📈 Reports (`/api/reports`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/summary` | Thống kê tổng quan hệ thống | ✅ | SystemAdmin |
| GET | `/by-district` | Thống kê theo quận/huyện | ✅ | SystemAdmin |
| GET | `/monthly-revenue` | Báo cáo doanh thu theo tháng | ✅ | SystemAdmin |

---

## 📋 Enterprise Applications (`/api/enterpriseapplications`)

| Method | Endpoint | Mô tả | Auth | Roles |
|--------|----------|-------|------|-------|
| GET | `/` | Lấy danh sách đơn đăng ký | ✅ | SystemAdmin |
| GET | `/{id}` | Lấy chi tiết đơn đăng ký | ✅ | SystemAdmin, Customer (own) |
| POST | `/` | Nộp đơn đăng ký doanh nghiệp | ✅ | Customer |
| PUT | `/{id}/approve` | Duyệt đơn đăng ký | ✅ | SystemAdmin |
| PUT | `/{id}/reject` | Từ chối đơn đăng ký | ✅ | SystemAdmin |

---

## 🏥 Health Check

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/health` | Kiểm tra trạng thái hệ thống |

---

## 📝 Ghi Chú

### Authentication Header

```
Authorization: Bearer <JWT_TOKEN>
```

### Response Format

```json
{
  "success": true,
  "message": "Success message",
  "data": { ... }
}
```

### Error Response

```json
{
  "success": false,
  "message": "Error message",
  "errors": [ ... ]
}
```

### Pagination Parameters

| Parameter | Type | Default | Mô tả |
|-----------|------|---------|-------|
| `page` | int | 1 | Số trang |
| `pageSize` | int | 10 | Số item mỗi trang |
| `sortBy` | string | - | Trường sắp xếp |
| `sortOrder` | string | asc | Thứ tự (asc/desc) |

### Filter Parameters (Products)

| Parameter | Type | Mô tả |
|-----------|------|-------|
| `search` | string | Tìm kiếm theo tên |
| `categoryId` | int | Lọc theo danh mục |
| `enterpriseId` | int | Lọc theo doanh nghiệp |
| `minPrice` | decimal | Giá tối thiểu |
| `maxPrice` | decimal | Giá tối đa |
| `status` | string | Trạng thái sản phẩm |
| `ocopRating` | int | Xếp hạng OCOP (1-5) |

---

**Tổng số endpoints: ~120**

**Developed with ❤️ for GiaLai OCOP**

