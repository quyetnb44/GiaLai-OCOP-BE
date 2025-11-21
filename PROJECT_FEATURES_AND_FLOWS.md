# 📘 Tổng Hợp Chức Năng & Các Luồng Chính

Tài liệu dành cho đội frontend/QA để hiểu rõ các module backend hiện có và ba luồng nghiệp vụ trọng tâm theo từng vai trò.

---

## 🧩 Chức năng hiện có

| Nhóm | Chức năng chính | Endpoint tiêu biểu | Ghi chú |
|------|-----------------|--------------------|--------|
| Authentication | Đăng ký, đăng nhập, phát JWT | `POST /api/auth/register`, `POST /api/auth/login` | Mật khẩu hash bằng BCrypt, role trả về trong token |
| Authentication | Đổi mật khẩu | `POST /api/auth/change-password` | Yêu cầu mật khẩu hiện tại, kiểm tra xác thực |
| User Profile | Cập nhật thông tin cá nhân | `PUT /api/users/me`, `GET /api/users/me` | Cho phép cập nhật Name, Email, PhoneNumber, Gender, DateOfBirth, ShippingAddress, AvatarUrl |
| Hồ sơ OCOP | Customer gửi hồ sơ doanh nghiệp OCOP | `POST /api/enterpriseapplications` | Validation đầy đủ, chặn gửi trùng khi còn Pending |
| Duyệt doanh nghiệp | SystemAdmin phê duyệt/từ chối | `PUT /api/enterpriseapplications/{id}/approve|reject` | Phê duyệt tự tạo Enterprise, gán user thành EnterpriseAdmin |
| Sản phẩm OCOP | EnterpriseAdmin CRUD sản phẩm | `POST/PUT/DELETE /api/products` | Trạng thái tự reset `PendingApproval`, chỉ SystemAdmin duyệt |
| Duyệt sản phẩm | SystemAdmin duyệt sản phẩm | `POST /api/products/{id}/status` | Nhận `PendingApproval`/`Approved`/`Rejected`, có thể cập nhật OCOP rating |
| Danh mục sản phẩm | SystemAdmin quản lý Category | `GET/POST/PUT/DELETE /api/categories` | Product.CategoryId set null khi xóa danh mục (`SetNull`) |
| Đơn hàng | Customer tạo đơn, EnterpriseAdmin xử lý | `POST /api/orders`, `PUT /api/orders/{id}/status` | Tạo đơn chỉ chấp nhận sản phẩm `Approved`, dùng transaction |
| Thanh toán | Multipayment theo Enterprise | `POST /api/payments`, `POST /api/payments/{id}/status` | Hỗ trợ COD & BankTransfer, QR theo từng doanh nghiệp |
| Bản đồ | Tìm kiếm doanh nghiệp, sản phẩm | `GET /api/map/...` | Chỉ hiển thị sản phẩm `Approved`, tính distance & rating |
| Báo cáo | Thống kê toàn tỉnh | `GET /api/reports/summary`, `districts`, `revenue-by-month` | Chỉ dành cho SystemAdmin |

---

## 🔁 Luồng 1: Customer → Đặt hàng & Thanh toán

**Mục tiêu:** khách xem sản phẩm đã duyệt, đặt hàng, thanh toán; doanh nghiệp xử lý và hoàn tất đơn.

1. **Xem danh sách sản phẩm**
   - `GET /api/products` (không cần đăng nhập).
   - Chỉ trả về sản phẩm `Approved`, kèm thông tin OCOP rating, danh mục.
2. **Xem chi tiết doanh nghiệp/sản phẩm trên Map (tùy chọn)**
   - `GET /api/map/search`, `GET /api/map/enterprises/{id}`.
   - Popup hiển thị sản phẩm nổi bật đã duyệt và URL chỉ đường.
3. **Đặt hàng**
   - Đăng nhập `POST /api/auth/login` → lấy token.
   - `POST /api/orders` với địa chỉ giao hàng và danh sách sản phẩm (productId, quantity).
   - Backend validate: sản phẩm phải `Approved`, còn hàng (`StockStatus != OutOfStock`), chạy transaction.
4. **Tạo thanh toán**
   - `POST /api/payments` (Customer) chọn `COD` hoặc `BankTransfer`.
   - Backend tự tạo payment cho từng doanh nghiệp trong đơn, trả QR (nếu chuyển khoản).
5. **Doanh nghiệp xử lý đơn hàng**
   - EnterpriseAdmin xem đơn `GET /api/orders` (filtered theo sản phẩm của doanh nghiệp).
   - Cập nhật trạng thái `Processing`/`Shipped`/`Completed` qua `PUT /api/orders/{id}/status`.
   - Nếu chuyển khoản, EnterpriseAdmin/ SystemAdmin xác nhận `POST /api/payments/{id}/status`.
6. **Hoàn tất**
   - Khi tất cả payments `Paid`, backend set `Order.PaymentStatus = Paid`.
   - Doanh nghiệp set `Status = Completed`, đơn hàng kết thúc.

**Điểm lưu ý cho frontend**
- Luôn truyền token cho endpoint cần xác thực.
- Lưu ý hiển thị trạng thái thanh toán (Pending/AwaitingTransfer/PartiallyPaid/Paid).
- Nếu thanh toán chuyển khoản, hiển thị QR từ `PaymentDto.QrCodeUrl`.

---

## 🔁 Luồng 2: EnterpriseAdmin → Quản lý sản phẩm & đơn hàng

1. **Quản lý sản phẩm**
   - Xem sản phẩm của doanh nghiệp: `GET /api/products` với token EnterpriseAdmin.
   - Tạo mới `POST /api/products` (chọn danh mục, upload ảnh nếu có) → trạng thái `PendingApproval`.
   - Chỉnh sửa `PUT /api/products/{id}` → tự reset `PendingApproval`.
   - Xóa `DELETE /api/products/{id}` (bị chặn nếu sản phẩm đã nằm trong đơn).
2. **Gửi duyệt OCOP**
   - Dữ liệu sản phẩm chờ duyệt được SystemAdmin xem & quyết định; EnterpriseAdmin không tự duyệt.
   - Theo dõi trạng thái qua trường `status` trả về.
3. **Quản lý đơn hàng**
   - `GET /api/orders` → chỉ chứa các đơn có sản phẩm của doanh nghiệp.
   - Cập nhật trạng thái: `Processing`, `Shipped`, `Completed` (`PUT /api/orders/{id}/status`).
   - Xác nhận thanh toán chuyển khoản doanh nghiệp mình: `POST /api/payments/{id}/status`.
4. **Báo cáo nội bộ (tùy chỉnh)**
   - Có thể dùng các endpoint order/payment hiện có để dựng báo cáo phía frontend (hiện backend chưa tách riêng).

**Lưu ý frontend**
- Các form sản phẩm cần truyền `CategoryId`, `ImageUrl`, `OCOPRating` tùy nghiệp vụ.
- Khi gọi `GET /api/products`, phân loại theo `status` để dựng UI “Đã duyệt/Chờ duyệt/Bị từ chối”.
- Đảm bảo EnterpriseAdmin chỉ thao tác được dữ liệu doanh nghiệp mình; backend đã kiểm soát nhưng UI nên ẩn hành động không hợp lệ.

---

## 🔁 Luồng 3: SystemAdmin → Vận hành toàn hệ thống

1. **Duyệt doanh nghiệp**
   - Xem hồ sơ: `GET /api/enterpriseapplications`.
   - Phê duyệt: `PUT /api/enterpriseapplications/{id}/approve` → tạo Enterprise, gán user thành EnterpriseAdmin.
   - Từ chối: `PUT /api/enterpriseapplications/{id}/reject`.
2. **Duyệt sản phẩm OCOP**
   - Xem sản phẩm chờ duyệt: `GET /api/products` (SystemAdmin sẽ thấy tất cả trạng thái).
   - Duyệt/ từ chối/ trả về chờ duyệt lại: `POST /api/products/{id}/status` với body `{ "status": "Approved" | "Rejected" | "PendingApproval", "ocopRating": optional }`.
3. **Quản lý danh mục**
   - `GET /api/categories` → dùng cho dropdown trong UI.
   - Tạo/sửa/xóa → `POST/PUT/DELETE`.
   - `IsActive` hỗ trợ ẩn danh mục khỏi danh sách chọn mà không mất dữ liệu.
4. **Báo cáo toàn tỉnh**
   - Tổng quan: `GET /api/reports/summary`.
   - Theo huyện: `GET /api/reports/districts`.
   - Doanh thu theo tháng: `GET /api/reports/revenue-by-month`.
   - Có thể kết hợp với các endpoint Orders/Payments để dựng dashboard nâng cao.

**Lưu ý frontend**
- Cần UI quản lý danh mục chuẩn: toggle `IsActive`, tránh cho EnterpriseAdmin chọn danh mục bị ẩn.
- Trang duyệt sản phẩm nên hiển thị thông tin doanh nghiệp, danh mục, rating đề xuất, hình ảnh.
- Dashboard: nên gọi các endpoint báo cáo định kỳ và hiển thị biểu đồ/summary cards.

---

## 📦 Migrations & Checklist kỹ thuật

- `20251113094500_UpdateOrderItemDeleteBehavior`: FK OrderItem → Product chuyển sang `Restrict`.
- `20251113183534_AddProductApprovalAndCategories`: tạo bảng `Categories`, thêm cột phê duyệt sản phẩm, cập nhật FK.
- Checklist khi triển khai:
  - [ ] `dotnet ef database update`
  - [ ] Seed dev (`MapSeedData`) tạo mẫu sản phẩm đã `Approved`.
  - [ ] Cập nhật frontend để lọc theo `status`, `category`.
  - [ ] Test 3 luồng nghiệp vụ end-to-end.
  - [ ] Test chức năng đổi mật khẩu (`POST /api/auth/change-password`).
  - [ ] Test chức năng cập nhật profile (`PUT /api/users/me`, `GET /api/users/me`).

---

**Liên hệ:**
- Mọi câu hỏi kỹ thuật có thể tra cứu thêm trong `PROJECT_FLOW_OVERVIEW.md`, `MAP_API_DOCUMENTATION.md`, `PAYMENT_API_DOCUMENTATION.md`.
- Nếu cần mở rộng báo cáo hoặc các luồng mới, có thể bổ sung controller tương ứng dựa trên cấu trúc hiện tại.

