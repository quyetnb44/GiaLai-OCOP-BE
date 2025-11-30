# 🔐 Tổng Hợp Tất Cả Chức Năng Của SystemAdmin

Tài liệu này liệt kê đầy đủ tất cả các chức năng mà SystemAdmin có thể thực hiện trong hệ thống GiaLai OCOP Backend.

---

## 📋 Mục Lục

1. [Quản Lý Người Dùng (Users)](#1-quản-lý-người-dùng-users)
2. [Quản Lý Doanh Nghiệp (Enterprises)](#2-quản-lý-doanh-nghiệp-enterprises)
3. [Quản Lý Sản Phẩm (Products)](#3-quản-lý-sản-phẩm-products)
4. [Quản Lý Đơn Hàng (Orders)](#4-quản-lý-đơn-hàng-orders)
5. [Quản Lý Thanh Toán (Payments)](#5-quản-lý-thanh-toán-payments)
6. [Quản Lý Danh Mục (Categories)](#6-quản-lý-danh-mục-categories)
7. [Quản Lý Đơn Đăng Ký OCOP (Enterprise Applications)](#7-quản-lý-đơn-đăng-ký-ocop-enterprise-applications)
8. [Quản Lý Ảnh (Images)](#8-quản-lý-ảnh-images)
9. [Báo Cáo & Thống Kê (Reports)](#9-báo-cáo--thống-kê-reports)
10. [Quản Lý Địa Điểm (Locations)](#10-quản-lý-địa-điểm-locations)
11. [Quản Lý Nhà Sản Xuất (Producers)](#11-quản-lý-nhà-sản-xuất-producers)
12. [Quản Lý Vận Chuyển (Shippers)](#12-quản-lý-vận-chuyển-shippers)
13. [Quản Lý Kho (Inventory)](#13-quản-lý-kho-inventory)

---

## 1. Quản Lý Người Dùng (Users)

### 1.1. Xem danh sách users
- **Endpoint:** `GET /api/users`
- **Mô tả:** Xem tất cả users trong hệ thống
- **Response:** Danh sách đầy đủ thông tin users

### 1.2. Xem chi tiết user
- **Endpoint:** `GET /api/users/{id}`
- **Mô tả:** Xem chi tiết bất kỳ user nào

### 1.3. Tạo user mới
- **Endpoint:** `POST /api/users`
- **Mô tả:** Tạo user mới với bất kỳ role nào (SystemAdmin, EnterpriseAdmin, Customer)
- **DTO:** `CreateUserDto`
- **Chức năng:**
  - Tạo SystemAdmin mới
  - Tạo EnterpriseAdmin (yêu cầu EnterpriseId)
  - Tạo Customer
  - Set trạng thái IsActive
  - Set trạng thái IsEmailVerified

### 1.4. Cập nhật user
- **Endpoint:** `PUT /api/users/{id}`
- **Mô tả:** Cập nhật toàn bộ thông tin user
- **DTO:** `UpdateUserDto`
- **Có thể cập nhật:**
  - Name, Email, PhoneNumber
  - Role (thay đổi vai trò)
  - EnterpriseId
  - Gender, DateOfBirth
  - ShippingAddress, AvatarUrl
  - Địa chỉ chi tiết (ProvinceId, DistrictId, WardId, AddressDetail)
  - IsActive, IsEmailVerified

### 1.5. Vô hiệu hóa/Kích hoạt tài khoản
- **Endpoint:** `PUT /api/users/{id}/toggle-status`
- **Mô tả:** Vô hiệu hóa hoặc kích hoạt tài khoản
- **DTO:** `ToggleUserStatusDto`
- **Lưu ý:** SystemAdmin không thể vô hiệu hóa chính mình

### 1.6. Xóa user
- **Endpoint:** `DELETE /api/users/{id}`
- **Mô tả:** Xóa user khỏi hệ thống
- **Lưu ý:** Có thể xóa bất kỳ user nào (kể cả SystemAdmin khác, EnterpriseAdmin)

### 1.7. Tạo EnterpriseAdmin (endpoint riêng)
- **Endpoint:** `POST /api/users/enterprise-admin`
- **Mô tả:** Endpoint chuyên dụng để tạo EnterpriseAdmin
- **DTO:** `CreateEnterpriseAdminDto`

---

## 2. Quản Lý Doanh Nghiệp (Enterprises)

### 2.1. Xem danh sách doanh nghiệp
- **Endpoint:** `GET /api/enterprises`
- **Mô tả:** Xem tất cả doanh nghiệp trong hệ thống

### 2.2. Xem chi tiết doanh nghiệp
- **Endpoint:** `GET /api/enterprises/{id}`
- **Mô tả:** Xem chi tiết bất kỳ doanh nghiệp nào

### 2.3. Tạo doanh nghiệp
- **Endpoint:** `POST /api/enterprises`
- **Mô tả:** Tạo doanh nghiệp mới

### 2.4. Cập nhật doanh nghiệp
- **Endpoint:** `PUT /api/enterprises/{id}`
- **Mô tả:** Cập nhật toàn bộ thông tin doanh nghiệp
- **Có thể cập nhật:**
  - Tất cả thông tin cơ bản
  - OCOPRating (xếp hạng OCOP)

### 2.5. Xóa doanh nghiệp
- **Endpoint:** `DELETE /api/enterprises/{id}`
- **Mô tả:** Xóa doanh nghiệp khỏi hệ thống

---

## 3. Quản Lý Sản Phẩm (Products)

### 3.1. Xem tất cả sản phẩm
- **Endpoint:** `GET /api/products`
- **Mô tả:** Xem tất cả sản phẩm (kể cả chưa duyệt, đã từ chối)
- **Khác biệt:** SystemAdmin xem được tất cả trạng thái, không bị filter

### 3.2. Xem chi tiết sản phẩm
- **Endpoint:** `GET /api/products/{id}`
- **Mô tả:** Xem chi tiết bất kỳ sản phẩm nào (kể cả chưa duyệt)

### 3.3. Cập nhật sản phẩm
- **Endpoint:** `PUT /api/products/{id}`
- **Mô tả:** Cập nhật sản phẩm (có thể là sản phẩm của bất kỳ enterprise nào)
- **Đặc biệt:** 
  - SystemAdmin cập nhật không reset status về PendingApproval
  - Có thể cập nhật partial (chỉ các field có giá trị)

### 3.4. Duyệt/Từ chối sản phẩm
- **Endpoint:** `POST /api/products/{id}/status`
- **Mô tả:** Duyệt hoặc từ chối sản phẩm OCOP
- **Có thể:**
  - Duyệt sản phẩm (Approved)
  - Từ chối sản phẩm (Rejected)
  - Cập nhật OCOPRating khi duyệt

### 3.5. Cập nhật ảnh sản phẩm
- **Endpoint:** `PUT /api/products/{id}/image`
- **Mô tả:** SystemAdmin cập nhật ảnh sản phẩm
- **Đặc biệt:** Không reset status về PendingApproval

---

## 4. Quản Lý Đơn Hàng (Orders)

### 4.1. Xem tất cả đơn hàng
- **Endpoint:** `GET /api/orders`
- **Mô tả:** Xem tất cả đơn hàng trong hệ thống (không filter theo enterprise)

### 4.2. Xem chi tiết đơn hàng
- **Endpoint:** `GET /api/orders/{id}`
- **Mô tả:** Xem chi tiết bất kỳ đơn hàng nào

### 4.3. Cập nhật trạng thái đơn hàng
- **Endpoint:** `PUT /api/orders/{id}/status`
- **Mô tả:** Cập nhật trạng thái đơn hàng
- **Có thể:** Cập nhật bất kỳ status nào (Pending, Processing, Shipped, Completed, Cancelled)

### 4.4. Xóa đơn hàng
- **Endpoint:** `DELETE /api/orders/{id}`
- **Mô tả:** Xóa bất kỳ đơn hàng nào

---

## 5. Quản Lý Thanh Toán (Payments)

### 5.1. Xem tất cả thanh toán
- **Endpoint:** `GET /api/payments`
- **Mô tả:** Xem tất cả payments trong hệ thống

### 5.2. Xem chi tiết thanh toán
- **Endpoint:** `GET /api/payments/{id}`
- **Mô tả:** Xem chi tiết bất kỳ payment nào

### 5.3. Xem payments của đơn hàng
- **Endpoint:** `GET /api/payments/order/{orderId}`
- **Mô tả:** Xem tất cả payments của một đơn hàng

### 5.4. Xác nhận thanh toán
- **Endpoint:** `POST /api/payments/{id}/status`
- **Mô tả:** Xác nhận thanh toán (có thể xác nhận payment của bất kỳ enterprise nào)
- **Có thể cập nhật:** Pending → Processing → Paid

---

## 6. Quản Lý Danh Mục (Categories)

### 6.1. Xem danh sách danh mục
- **Endpoint:** `GET /api/categories`
- **Mô tả:** Xem tất cả danh mục (công khai, nhưng SystemAdmin có quyền quản lý)

### 6.2. Xem chi tiết danh mục
- **Endpoint:** `GET /api/categories/{id}`
- **Mô tả:** Xem chi tiết danh mục

### 6.3. Tạo danh mục
- **Endpoint:** `POST /api/categories`
- **Mô tả:** Tạo danh mục sản phẩm mới
- **DTO:** `CreateCategoryDto`

### 6.4. Cập nhật danh mục
- **Endpoint:** `PUT /api/categories/{id}`
- **Mô tả:** Cập nhật thông tin danh mục
- **Có thể cập nhật:** Name, Description, IsActive

### 6.5. Xóa danh mục
- **Endpoint:** `DELETE /api/categories/{id}`
- **Mô tả:** Xóa danh mục (Product.CategoryId sẽ được set null)

---

## 7. Quản Lý Đơn Đăng Ký OCOP (Enterprise Applications)

### 7.1. Xem tất cả đơn đăng ký
- **Endpoint:** `GET /api/enterpriseapplications`
- **Mô tả:** Xem tất cả đơn đăng ký OCOP (Pending, Approved, Rejected)

### 7.2. Phê duyệt đơn đăng ký
- **Endpoint:** `PUT /api/enterpriseapplications/{id}/approve`
- **Mô tả:** Phê duyệt đơn đăng ký OCOP
- **Hành động:**
  - Tạo Enterprise mới từ thông tin đơn
  - Chuyển User thành EnterpriseAdmin
  - Gán EnterpriseId cho user
  - Cập nhật status thành "Approved"

### 7.3. Từ chối đơn đăng ký
- **Endpoint:** `PUT /api/enterpriseapplications/{id}/reject`
- **Mô tả:** Từ chối đơn đăng ký OCOP
- **Có thể:** Thêm AdminComment để giải thích lý do từ chối

---

## 8. Quản Lý Ảnh (Images)

### 8.1. Xem tất cả ảnh
- **Endpoint:** `GET /api/Admin/Images`
- **Mô tả:** Xem tất cả ảnh trong hệ thống
- **Filters:**
  - `imageType` (Product, Enterprise, Avatar, etc.)
  - `isApproved` (true/false)
  - `isActive` (true/false)
  - `page`, `pageSize` (phân trang)

### 8.2. Xem chi tiết ảnh
- **Endpoint:** `GET /api/Admin/Images/{imageId}`
- **Mô tả:** Xem chi tiết ảnh (bao gồm thông tin user/product/enterprise liên quan)

### 8.3. Duyệt ảnh
- **Endpoint:** `PUT /api/Admin/Images/{imageId}/Approve`
- **Mô tả:** Duyệt ảnh (set IsApproved = true)

### 8.4. Từ chối ảnh
- **Endpoint:** `PUT /api/Admin/Images/{imageId}/Reject`
- **Mô tả:** Từ chối ảnh (set IsApproved = false, IsActive = false)

### 8.5. Xóa ảnh
- **Endpoint:** `DELETE /api/Admin/Images/{imageId}`
- **Mô tả:** Soft delete ảnh (set IsActive = false, DeletedAt = now)

### 8.6. Thống kê ảnh
- **Endpoint:** `GET /api/Admin/Images/Stats`
- **Mô tả:** Xem thống kê về ảnh trong hệ thống
- **Bao gồm:**
  - Tổng số ảnh
  - Số ảnh active/approved/pending
  - Thống kê theo imageType

---

## 9. Báo Cáo & Thống Kê (Reports)

### 9.1. Tổng quan toàn tỉnh
- **Endpoint:** `GET /api/reports/summary`
- **Mô tả:** Xem tổng quan toàn tỉnh
- **Bao gồm:**
  - Tổng số doanh nghiệp, categories, sản phẩm
  - Số sản phẩm đã duyệt/pending/rejected
  - Tổng số đơn đăng ký, đơn chờ duyệt
  - Tổng số đơn hàng, customers, enterprise admins
  - Tổng số payments, tổng tiền đã thanh toán, đang chờ

### 9.2. Thống kê theo huyện
- **Endpoint:** `GET /api/reports/districts`
- **Mô tả:** Thống kê doanh nghiệp và sản phẩm OCOP theo huyện
- **Bao gồm:**
  - Số doanh nghiệp mỗi huyện
  - Số sản phẩm đã duyệt/pending mỗi huyện

### 9.3. Doanh thu theo tháng
- **Endpoint:** `GET /api/reports/revenue-by-month`
- **Mô tả:** Doanh thu thanh toán đã duyệt theo tháng (12 tháng gần nhất)

---

## 10. Quản Lý Địa Điểm (Locations)

### 10.1. Xem danh sách địa điểm
- **Endpoint:** `GET /api/locations`

### 10.2. Xem chi tiết địa điểm
- **Endpoint:** `GET /api/locations/{id}`

### 10.3. Tạo địa điểm
- **Endpoint:** `POST /api/locations`
- **Mô tả:** Tạo địa điểm mới

### 10.4. Cập nhật địa điểm
- **Endpoint:** `PUT /api/locations/{id}`
- **Mô tả:** Cập nhật thông tin địa điểm

### 10.5. Xóa địa điểm
- **Endpoint:** `DELETE /api/locations/{id}`
- **Mô tả:** Xóa địa điểm

---

## 11. Quản Lý Nhà Sản Xuất (Producers)

### 11.1. Xem danh sách nhà sản xuất
- **Endpoint:** `GET /api/producers`

### 11.2. Xem chi tiết nhà sản xuất
- **Endpoint:** `GET /api/producers/{id}`

### 11.3. Tạo nhà sản xuất
- **Endpoint:** `POST /api/producers`
- **Mô tả:** Tạo nhà sản xuất mới

### 11.4. Cập nhật nhà sản xuất
- **Endpoint:** `PUT /api/producers/{id}`
- **Mô tả:** Cập nhật thông tin nhà sản xuất

### 11.5. Xóa nhà sản xuất
- **Endpoint:** `DELETE /api/producers/{id}`
- **Mô tả:** Xóa nhà sản xuất

---

## 12. Quản Lý Vận Chuyển (Shippers)

### 12.1. Xem danh sách shippers
- **Endpoint:** `GET /api/shippers`
- **Mô tả:** Xem tất cả shippers trong hệ thống

### 12.2. Xem đơn hàng cần giao
- **Endpoint:** `GET /api/shippers/orders`
- **Mô tả:** Xem tất cả đơn hàng cần giao (không filter theo enterprise)

### 12.3. Gán đơn hàng cho shipper
- **Endpoint:** `PUT /api/shippers/{shipperId}/assign-order/{orderId}`
- **Mô tả:** Gán đơn hàng cho shipper (có thể gán đơn của bất kỳ enterprise nào)

---

## 13. Quản Lý Kho (Inventory)

### 13.1. Xem lịch sử kho
- **Endpoint:** `GET /api/inventory/history`
- **Mô tả:** Xem tất cả lịch sử thay đổi kho (không filter theo enterprise)

### 13.2. Các chức năng khác
- SystemAdmin có thể truy cập tất cả endpoints trong InventoryController

---

## 🎯 Tổng Kết

### Đặc Quyền Của SystemAdmin

1. **Toàn Quyền Truy Cập:**
   - Xem tất cả dữ liệu không bị filter
   - Có thể thao tác trên dữ liệu của bất kỳ enterprise nào

2. **Quyền Duyệt:**
   - Duyệt/từ chối sản phẩm OCOP
   - Duyệt/từ chối đơn đăng ký OCOP
   - Duyệt/từ chối ảnh

3. **Quyền Quản Lý:**
   - Quản lý tất cả users (tạo, sửa, xóa, vô hiệu hóa)
   - Quản lý tất cả enterprises
   - Quản lý tất cả categories
   - Quản lý địa điểm, nhà sản xuất

4. **Quyền Báo Cáo:**
   - Xem báo cáo tổng quan toàn tỉnh
   - Thống kê theo huyện
   - Doanh thu theo tháng

5. **Quyền Hệ Thống:**
   - Cập nhật sản phẩm không reset status
   - Xác nhận payment của bất kỳ enterprise nào
   - Quản lý toàn bộ ảnh trong hệ thống

---

**Lưu ý:** SystemAdmin có toàn quyền trong hệ thống, nên cần được bảo vệ cẩn thận. Tài khoản SystemAdmin mặc định được tạo khi khởi động ứng dụng lần đầu (xem `Program.cs`).

---

**Cập nhật:** 2024-11-30

