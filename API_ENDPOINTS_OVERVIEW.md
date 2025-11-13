# 📚 Tổng Hợp API GiaLai OCOP Backend

Danh sách chi tiết toàn bộ REST API hiện có, phân loại theo module, role, phương thức, payload và ghi chú tích hợp.

---

## 🔐 Authentication

| Method | Endpoint | Role | Body | Trả về | Ghi chú |
|--------|----------|------|------|--------|---------|
| `POST` | `/api/auth/register` | Public | `{ name, email, password }` | Thông tin user (Id/Name/Email/Role) | Tạo Customer mới |
| `POST` | `/api/auth/login` | Public | `{ email, password }` | `{ token, expires }` | JWT chứa `sub`, `nameidentifier`, `role` |

---

## 👤 Users

| Method | Endpoint | Role | Chức năng |
|--------|----------|------|-----------|
| `GET` | `/api/users` | `SystemAdmin` | Danh sách user + doanh nghiệp tương ứng |
| `GET` | `/api/users/{id}` | `SystemAdmin` hoặc chính user | Chi tiết user |
| `POST` | `/api/users/enterprise-admin` | `SystemAdmin` | Tạo EnterpriseAdmin cho doanh nghiệp đã có |
| `POST` | `/api/users/customer` | Public | Đăng ký Customer (giống `auth/register`) |
| `PUT` | `/api/users/{id}` | `SystemAdmin` | Cập nhật user |
| `DELETE` | `/api/users/{id}` | `SystemAdmin` | Xóa user |

---

## 🏢 Enterprise Applications (Đăng ký OCOP)

| Method | Endpoint | Role | Ghi chú |
|--------|----------|------|---------|
| `POST` | `/api/enterpriseapplications` | `Customer` | Gửi hồ sơ OCOP (validation đầy đủ) |
| `GET` | `/api/enterpriseapplications` | `SystemAdmin` | Danh sách hồ sơ |
| `PUT` | `/api/enterpriseapplications/{id}/approve` | `SystemAdmin` | Phê duyệt, tạo doanh nghiệp, gán role EnterpriseAdmin |
| `PUT` | `/api/enterpriseapplications/{id}/reject` | `SystemAdmin` | Từ chối, ghi kèm nhận xét |

---

## 🏭 Enterprises

> Controller `EnterprisesController` giới hạn `SystemAdmin`. Map/Frontend public dùng API thuộc `MapController`.

| Method | Endpoint | Ghi chú |
|--------|----------|---------|
| `GET` | `/api/enterprises` | Danh sách enterprise + sản phẩm (kèm trạng thái) |
| `GET` | `/api/enterprises/{id}` | Chi tiết enterprise |
| `POST` | `/api/enterprises` | Tạo enterprise thủ công (ít dùng) |
| `PUT` | `/api/enterprises/{id}` | Cập nhật enterprise |
| `DELETE` | `/api/enterprises/{id}` | Xóa enterprise |

---

## 🗂️ Categories (Quản lý danh mục sản phẩm)

| Method | Endpoint | Role | Ghi chú |
|--------|----------|------|---------|
| `GET` | `/api/categories?isActive=true|false` | `SystemAdmin` | Lọc theo trạng thái |
| `GET` | `/api/categories/{id}` | `SystemAdmin` | Chi tiết |
| `POST` | `/api/categories` | `SystemAdmin` | Tạo danh mục |
| `PUT` | `/api/categories/{id}` | `SystemAdmin` | Cập nhật tên/mô tả/trạng thái |
| `DELETE` | `/api/categories/{id}` | `SystemAdmin` | Xóa (Product.CategoryId → `null`) |

---

## 📦 Products

| Method | Endpoint | Role | Ghi chú |
|--------|----------|------|---------|
| `GET` | `/api/products` | Public/EnterpriseAdmin/SystemAdmin | Public chỉ thấy `status=Approved`, EnterpriseAdmin thấy của doanh nghiệp mình, SystemAdmin thấy tất cả |
| `GET` | `/api/products/{id}` | Public (ẩn nếu chưa duyệt) | Tra cứu chi tiết |
| `POST` | `/api/products` | `EnterpriseAdmin` | Tạo sản phẩm mới → `PendingApproval` |
| `PUT` | `/api/products/{id}` | `EnterpriseAdmin` | Cập nhật, reset trạng thái về `PendingApproval` |
| `DELETE` | `/api/products/{id}` | `EnterpriseAdmin` | Chặn nếu sản phẩm đã nằm trong đơn |
| `POST` | `/api/products/{id}/status` | `SystemAdmin` | Duyệt/từ chối/đưa về pending (body `{ status, ocopRating? }`) |

---

## 🛒 Orders

| Method | Endpoint | Role | Ghi chú |
|--------|----------|------|---------|
| `GET` | `/api/orders` | `Customer`/`EnterpriseAdmin`/`SystemAdmin` | Customer: đơn của mình; EnterpriseAdmin: đơn có sản phẩm của doanh nghiệp; SystemAdmin: tất cả |
| `GET` | `/api/orders/{id}` | như trên | Chi tiết đơn |
| `POST` | `/api/orders` | `Customer` | Tạo đơn (sản phẩm bắt buộc `Approved`, transaction) |
| `PUT` | `/api/orders/{id}/status` | `Customer`/`EnterpriseAdmin`/`SystemAdmin` | Customer chỉ hủy Pending; EnterpriseAdmin cập nhật tiến độ; SystemAdmin toàn quyền |
| `DELETE` | `/api/orders/{id}` | `Customer`/`EnterpriseAdmin`/`SystemAdmin` | Customer xóa Pending; EnterpriseAdmin xóa Pending/Cancelled; SystemAdmin toàn quyền |

---

## 💳 Payments

| Method | Endpoint | Role | Ghi chú |
|--------|----------|------|---------|
| `POST` | `/api/payments` | `Customer` | Tạo payment (COD/BankTransfer) → tạo cho từng enterprise trong đơn |
| `GET` | `/api/payments/{id}` | `Customer` (đơn của mình), `EnterpriseAdmin` (doanh nghiệp mình), `SystemAdmin` | Chi tiết payment |
| `GET` | `/api/payments/order/{orderId}` | như trên | Danh sách payment một đơn |
| `POST` | `/api/payments/{id}/status` | `SystemAdmin`/`EnterpriseAdmin` | Cập nhật `Paid`/`Cancelled`; EnterpriseAdmin chỉ với payment thuộc doanh nghiệp mình |

---

## 📍 Map

| Method | Endpoint | Role | Mục đích |
|--------|----------|------|----------|
| `GET` | `/api/map/search` | Public | Tìm kiếm theo keyword, khoảng cách, sort |
| `GET` | `/api/map/bounding-box` | Public | Lọc theo viewport bản đồ |
| `GET` | `/api/map/nearby` | Public | Tìm gần vị trí người dùng |
| `GET` | `/api/map/filter` | Public | Lọc nâng cao (district, province, OCOP rating, distance…) |
| `GET` | `/api/map/enterprises/{id}` | Public | Chi tiết doanh nghiệp + 3 sản phẩm nổi bật đã duyệt |
| `GET` | `/api/map/enterprises/{id}/products` | Public | Danh sách sản phẩm `Approved` của doanh nghiệp |
| `GET` | `/api/map/filter-options` | Public | Options cho dropdown (districts, provinces, business fields, OCOP ratings) |

---

## 📊 Reports (SystemAdmin)

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| `GET` | `/api/reports/summary` | Tổng quan: doanh nghiệp, sản phẩm (theo trạng thái), hồ sơ OCOP, người dùng, thanh toán |
| `GET` | `/api/reports/districts` | Thống kê doanh nghiệp & sản phẩm OCOP theo huyện |
| `GET` | `/api/reports/revenue-by-month` | Doanh thu thanh toán đã duyệt 12 tháng gần nhất |

---

## 🧾 Tài liệu chi tiết

- `MAP_API_DOCUMENTATION.md` – giải thích chi tiết các tham số, ví dụ request/response Map API.
- `PAYMENT_API_DOCUMENTATION.md` – mô tả đầy đủ mô hình thanh toán đa doanh nghiệp, QR, quy trình xác nhận.
- `PROJECT_FEATURES_AND_FLOWS.md` – mô tả chức năng theo module và các luồng nghiệp vụ chính.
- `PROJECT_FLOW_OVERVIEW.md` – tài liệu tổng quát từng chức năng, nhiệm vụ của từng role.

---

### Checklist tích hợp Frontend

- [ ] Đăng nhập/đăng ký (JWT) → lưu token.
- [ ] Danh sách sản phẩm/public map: chỉ show `status=Approved`.
- [ ] Giao diện EnterpriseAdmin: hiển thị các sản phẩm pending/rejected, form tạo sản phẩm kèm danh mục.
- [ ] Workflow đặt hàng → thanh toán → xác nhận: dùng các endpoint Orders/Payments tương ứng.
- [ ] Trang quản trị SystemAdmin: duyệt doanh nghiệp, duyệt sản phẩm, quản lý danh mục, dashboard (Reports API).
- [ ] Áp dụng migration mới nhất trước khi test (`dotnet ef database update`).

---

**Cập nhật:** 2025-11-13  
Mọi câu hỏi bổ sung có thể tra cứu thêm trong các file tài liệu chuyên biệt hoặc liên hệ đội backend.

