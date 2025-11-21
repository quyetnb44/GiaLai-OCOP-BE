# 📊 Tổng Quan Luồng & Chức Năng GiaLai OCOP Backend

Tài liệu này tổng hợp nhanh các luồng nghiệp vụ chính và chức năng đã có trong dự án, phục vụ việc phối hợp với frontend và kiểm thử.

---

## 👥 Phân Quyền & Người Dùng

| Role | Quyền chính |
|------|-------------|
| `Customer` | Đăng ký/đăng nhập, tạo đơn hàng, tạo hồ sơ đăng ký doanh nghiệp (OCOP), theo dõi đơn & thanh toán của chính mình. |
| `EnterpriseAdmin` | Quản lý sản phẩm doanh nghiệp, theo dõi & xử lý đơn hàng có sản phẩm của mình, xác nhận thanh toán thuộc doanh nghiệp, xem báo cáo của doanh nghiệp. |
| `SystemAdmin` | Quản trị toàn hệ thống: duyệt hồ sơ OCOP, duyệt sản phẩm OCOP, quản lý danh mục, người dùng, doanh nghiệp, đơn hàng, thanh toán, báo cáo toàn tỉnh. |

---

## 🧭 Luồng Chính Theo Doanh Nghiệp

### 1. Đăng ký OCOP (Customer → SystemAdmin)
1. Customer gửi hồ sơ qua `POST /api/enterpriseapplications` (đã có validation dữ liệu).
2. SystemAdmin xem danh sách `GET /api/enterpriseapplications`.
3. SystemAdmin phê duyệt `PUT /api/enterpriseapplications/{id}/approve`:
   - Tự động tạo bản ghi `Enterprise` đầy đủ thông tin từ hồ sơ.
   - Chuyển user sang vai trò `EnterpriseAdmin`.
4. SystemAdmin từ chối `PUT /api/enterpriseapplications/{id}/reject` (kèm ghi chú).

### 2. Quản lý sản phẩm (EnterpriseAdmin ➜ SystemAdmin)
- EnterpriseAdmin tạo/sửa sản phẩm → trạng thái tự về `PendingApproval` (yêu cầu duyệt).
- Phải chọn danh mục hợp lệ (nếu có) từ `Categories`.
- SystemAdmin duyệt / từ chối bằng `POST /api/products/{id}/status` (`PendingApproval`, `Approved`, `Rejected`) và có thể cập nhật OCOP rating.
- `GET /api/products`:
  - Khách/Customer chỉ thấy sản phẩm `Approved`.
  - EnterpriseAdmin thấy sản phẩm của doanh nghiệp mình (mọi trạng thái).
  - SystemAdmin thấy toàn bộ.
- Sản phẩm chỉ hiển thị trên Map, popup chi tiết doanh nghiệp và được phép đặt hàng khi trạng thái `Approved`.
- Xóa sản phẩm bị chặn nếu đã nằm trong đơn hàng; FK `OrderItem` → `Product` chuyển sang `Restrict` để bảo toàn lịch sử.

### 3. Bán hàng & Thanh toán
1. Customer tạo đơn `POST /api/orders` (kiểm tra tồn kho, chỉ chấp nhận sản phẩm `Approved`, transaction bảo toàn dữ liệu).
2. Customer tạo thanh toán `POST /api/payments`:
   - Tạo payment riêng cho từng doanh nghiệp trong đơn.
   - Hỗ trợ `COD` & `BankTransfer`, trả QR nếu cần.
3. EnterpriseAdmin/SystemAdmin cập nhật thanh toán `POST /api/payments/{id}/status`.
4. Trạng thái đơn (`PUT /api/orders/{id}/status`):
   - Customer chỉ hủy khi đơn `Pending`.
   - EnterpriseAdmin cập nhật `Processing`, `Shipped`, `Completed`.

### 4. Quản lý danh mục sản phẩm (SystemAdmin)
- CRUD qua `CategoriesController`:
  - `GET /api/categories` (lọc theo `isActive`).
  - `POST /api/categories`
  - `PUT /api/categories/{id}`
  - `DELETE /api/categories/{id}` (Product.CategoryId tự set null nhờ `SetNull`).
- Danh mục giúp chuẩn hóa ngành hàng OCOP, làm dữ liệu lọc thống nhất cho frontend.

### 5. Theo dõi đơn hàng & Báo cáo
- Customer: `GET /api/orders` / `GET /api/orders/{id}` chỉ thấy đơn của mình.
- EnterpriseAdmin: thấy đơn có sản phẩm của doanh nghiệp mình, kèm chi tiết payment.
- SystemAdmin: thấy tất cả.
- Báo cáo tổng hợp:
  - `GET /api/reports/summary` – thống kê doanh nghiệp, sản phẩm, hồ sơ OCOP, người dùng, thanh toán.
  - `GET /api/reports/districts` – số doanh nghiệp & sản phẩm OCOP theo huyện.
  - `GET /api/reports/revenue-by-month` – doanh thu thanh toán đã duyệt 12 tháng gần nhất.

---

## 🗺️ Luồng Map & Tìm kiếm doanh nghiệp

- `GET /api/map/search` – tìm theo từ khóa, tính khoảng cách, sắp xếp.
- `GET /api/map/bounding-box` – lọc theo vùng bản đồ hiện tại.
- `GET /api/map/nearby` – tìm doanh nghiệp gần vị trí người dùng.
- `GET /api/map/filter` – lọc kết hợp huyện/tỉnh, ngành nghề, OCOP rating, khoảng cách.
- `GET /api/map/enterprises/{id}` – chi tiết doanh nghiệp, sản phẩm nổi bật (chỉ sản phẩm `Approved`), đường đi.
- `GET /api/map/enterprises/{id}/products` – danh sách sản phẩm đã duyệt của doanh nghiệp.
- `GET /api/map/filter-options` – dữ liệu cho dropdown filter.

> Backend hỗ trợ cả tham số `userLat/userLng` và `userLatitude/userLongitude`; đã seed dữ liệu mẫu trong môi trường Development. Rating và thống kê chỉ tính trên sản phẩm đã duyệt.

---

## 💳 Thanh toán

- `POST /api/payments` – Customer tạo payment mới (mỗi doanh nghiệp một payment).
- `GET /api/payments/{id}` – Chi tiết payment (kiểm tra theo role).
- `GET /api/payments/order/{orderId}` – Danh sách payment của đơn.
- `POST /api/payments/{id}/status` – EnterpriseAdmin/SystemAdmin xác nhận thanh toán.
- Payment method: `COD`, `BankTransfer`.
- Tự động cập nhật `Order.PaymentStatus` (`Pending`, `AwaitingTransfer`, `PartiallyPaid`, `Paid`, `Cancelled`).
- Chặn tạo payment khi thiếu cấu hình ngân hàng, trả lỗi rõ ràng.

---

## 🛡️ Các biện pháp an toàn

- Tất cả endpoint nhạy cảm yêu cầu JWT.
- EnterpriseAdmin bị chặn xóa sản phẩm đã vào đơn hàng.
- Tạo đơn hàng dùng transaction, tránh order “rác”.
- Mapping hồ sơ OCOP → Enterprise đầy đủ dữ liệu giảm thao tác tay.
- Validation dữ liệu đầu vào (DTOs) cho auth, order, payment, enterprise application.
- Migration `20251113094500_UpdateOrderItemDeleteBehavior` điều chỉnh FK OrderItem để bảo toàn lịch sử đơn hàng.
- Migration `20251113183534_AddProductApprovalAndCategories` tạo bảng `Categories`, thêm trạng thái duyệt sản phẩm, cập nhật FK OrderItem & Products.
- Map chỉ lấy sản phẩm đã duyệt; tạo đơn hàng chỉ nhận sản phẩm `Approved`.

---

## ✅ Check-list tích hợp & kiểm thử

### Authentication & User Management
- [ ] Test đăng ký và đăng nhập (`POST /api/auth/register`, `POST /api/auth/login`).
- [ ] Test đổi mật khẩu (`POST /api/auth/change-password`) - kiểm tra mật khẩu hiện tại đúng/sai.
- [ ] Test lấy thông tin profile hiện tại (`GET /api/users/me`).
- [ ] Test cập nhật profile (`PUT /api/users/me`) - cập nhật Name, Email, PhoneNumber, Gender, DateOfBirth, ShippingAddress, AvatarUrl.

### Business Flows
- [ ] Test luồng OCOP từ Customer → Admin (gửi, duyệt/từ chối).
- [ ] CRUD sản phẩm với EnterpriseAdmin (bao gồm case chỉnh sửa → reset `PendingApproval`, xóa khi chưa có đơn).
- [ ] Duyệt/từ chối sản phẩm qua `POST /api/products/{id}/status`.
- [ ] CRUD danh mục (`Categories`) và verify sản phẩm chỉ sử dụng danh mục active.
- [ ] Đặt hàng, tạo payment (COD/BankTransfer), xác nhận, cập nhật trạng thái.
- [ ] Map API: search, filter, bounding box, nearby, chi tiết doanh nghiệp (kiểm tra sản phẩm chỉ hiển thị khi `Approved`).
- [ ] Báo cáo: gọi `summary`, `districts`, `revenue-by-month` và đối soát dữ liệu mẫu.
- [ ] Phân quyền: đảm bảo mỗi role chỉ nhìn/động được dữ liệu thuộc về mình.
- [ ] Áp dụng migration mới: `dotnet ef database update`.

---

**Cập nhật lần cuối:** 2025-11-13  
Có thể mở rộng thêm báo cáo hoặc endpoint chuyên sâu tùy nhu cầu vận hành.
# 📊 Tổng Quan Luồng & Chức Năng GiaLai OCOP Backend

Tài liệu này tổng hợp nhanh các luồng nghiệp vụ chính và chức năng đã có trong dự án, phục vụ việc phối hợp với frontend và kiểm thử.

---

## 👥 Phân Quyền & Người Dùng

| Role | Quyền chính |
|------|-------------|
| `Customer` | Đăng ký/đăng nhập, tạo đơn hàng, tạo hồ sơ đăng ký doanh nghiệp (OCOP), theo dõi đơn & thanh toán của chính mình. |
| `EnterpriseAdmin` | Quản lý sản phẩm doanh nghiệp, theo dõi & xử lý đơn hàng có sản phẩm của mình, xác nhận thanh toán thuộc doanh nghiệp, xem báo cáo của doanh nghiệp. |
| `SystemAdmin` | Quản trị toàn hệ thống: duyệt hồ sơ OCOP, quản lý người dùng, doanh nghiệp, sản phẩm, đơn hàng, thanh toán, báo cáo tổng. |

---

## 🧭 Luồng Chính Theo Doanh Nghiệp

### 1. Đăng ký OCOP (Customer → SystemAdmin)
1. Customer gửi hồ sơ qua `POST /api/enterpriseapplications` (đã có validation dữ liệu).
2. SystemAdmin xem danh sách `GET /api/enterpriseapplications`.
3. SystemAdmin phê duyệt `PUT /api/enterpriseapplications/{id}/approve`:
   - Tự động tạo bản ghi `Enterprise` đầy đủ thông tin từ hồ sơ.
   - Chuyển user sang vai trò `EnterpriseAdmin`.
4. SystemAdmin từ chối `PUT /api/enterpriseapplications/{id}/reject` (kèm ghi chú).

### 2. Quản lý sản phẩm (EnterpriseAdmin)
- Xem danh sách sản phẩm `GET /api/products` (lọc theo doanh nghiệp khi đăng nhập).
- CRUD sản phẩm:
  - `POST /api/products`
  - `PUT /api/products/{id}`
  - `DELETE /api/products/{id}` (chặn nếu đã có trong đơn hàng).
- Dữ liệu lưu cả OCOP rating, tình trạng kho, hình ảnh.

### 3. Bán hàng & Thanh toán
1. Customer tạo đơn `POST /api/orders` (kiểm tra tồn kho, transaction bảo toàn dữ liệu).
2. Customer tạo thanh toán `POST /api/payments`:
   - Tạo payment riêng cho từng doanh nghiệp trong đơn.
   - Hỗ trợ `COD` & `BankTransfer`, trả QR nếu cần.
3. EnterpriseAdmin/SystemAdmin cập nhật thanh toán `POST /api/payments/{id}/status`.
4. Trạng thái đơn (`PUT /api/orders/{id}/status`):
   - Customer chỉ hủy khi đơn `Pending`.
   - EnterpriseAdmin cập nhật `Processing`, `Shipped`, `Completed`.

### 4. Theo dõi đơn hàng & báo cáo
- Customer: `GET /api/orders` / `GET /api/orders/{id}` chỉ thấy đơn của mình.
- EnterpriseAdmin: thấy đơn có sản phẩm của doanh nghiệp mình, kèm chi tiết payment.
- SystemAdmin: thấy tất cả.
- Báo cáo tổng hợp: thực hiện qua truy vấn Orders/Payments (có thể mở rộng endpoint tùy nhu cầu).

---

## 🗺️ Luồng Map & Tìm kiếm doanh nghiệp

- `GET /api/map/search` – tìm theo từ khóa, tính khoảng cách, sắp xếp.
- `GET /api/map/bounding-box` – lọc theo vùng bản đồ hiện tại.
- `GET /api/map/nearby` – tìm doanh nghiệp gần vị trí người dùng.
- `GET /api/map/filter` – lọc kết hợp huyện/tỉnh, ngành nghề, OCOP rating, khoảng cách.
- `GET /api/map/enterprises/{id}` – chi tiết doanh nghiệp, sản phẩm nổi bật, đường đi.
- `GET /api/map/enterprises/{id}/products` – danh sách sản phẩm của doanh nghiệp.
- `GET /api/map/filter-options` – dữ liệu cho dropdown filter.

> Backend hỗ trợ cả tham số `userLat/userLng` và `userLatitude/userLongitude`; đã seed dữ liệu mẫu trong môi trường Development.

---

## 💳 Thanh toán

- `POST /api/payments` – Customer tạo payment mới (mỗi doanh nghiệp một payment).
- `GET /api/payments/{id}` – Chi tiết payment (kiểm tra theo role).
- `GET /api/payments/order/{orderId}` – Danh sách payment của đơn.
- `POST /api/payments/{id}/status` – EnterpriseAdmin/SystemAdmin xác nhận thanh toán.
- Payment method: `COD`, `BankTransfer`.
- Tự động cập nhật `Order.PaymentStatus` (`Pending`, `AwaitingTransfer`, `PartiallyPaid`, `Paid`, `Cancelled`).
- Chặn tạo payment khi thiếu cấu hình ngân hàng, trả lỗi rõ ràng.

---

## 🛡️ Các biện pháp an toàn

- Tất cả endpoint nhạy cảm yêu cầu JWT.
- EnterpriseAdmin bị chặn xóa sản phẩm đã vào đơn hàng.
- Tạo đơn hàng dùng transaction, tránh order “rác”.
- Mapping hồ sơ OCOP → Enterprise đầy đủ dữ liệu giảm thao tác tay.
- Validation dữ liệu đầu vào (DTOs) cho auth, order, payment, enterprise application.
- Migration `20251113094500_UpdateOrderItemDeleteBehavior` điều chỉnh FK để bảo toàn lịch sử đơn hàng.

---

## ✅ Check-list tích hợp & kiểm thử

- [ ] Test luồng OCOP từ Customer → Admin (gửi, duyệt/từ chối).
- [ ] CRUD sản phẩm với EnterpriseAdmin (bao gồm case xóa khi đã có đơn).
- [ ] Đặt hàng, tạo payment (COD/BankTransfer), xác nhận, cập nhật trạng thái.
- [ ] Map API: search, filter, bounding box, nearby, chi tiết doanh nghiệp.
- [ ] Phân quyền: đảm bảo mỗi role chỉ nhìn/động được dữ liệu thuộc về mình.
- [ ] Áp dụng migration mới: `dotnet ef database update`.

---

**Cập nhật lần cuối:** 2025-11-13  
Nếu cần thêm báo cáo hoặc endpoint thống kê cụ thể, có thể mở rộng dựa trên các truy vấn Orders/Payments hiện có.

