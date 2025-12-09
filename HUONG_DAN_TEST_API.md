# 🧪 Hướng Dẫn Test API

Tài liệu hướng dẫn test các API đã được xây dựng trong hệ thống.

---

## 📋 Mục Lục

1. [Chuẩn bị](#1-chuẩn-bị)
2. [Test API Wallet](#2-test-api-wallet)
3. [Test API WalletRequest](#3-test-api-walletrequest)
4. [Test API Orders (EnterpriseAdmin)](#4-test-api-orders-enterpriseadmin)
5. [Test API SystemAdmin Wallet Management](#5-test-api-systemadmin-wallet-management)

---

## 1. Chuẩn bị

### 1.1. Cài đặt công cụ test

**Option 1: Sử dụng Postman**
- Download: https://www.postman.com/downloads/
- Import collection từ file hoặc tạo request thủ công

**Option 2: Sử dụng cURL**
- Đã có sẵn trên Windows 10/11
- Hoặc dùng PowerShell với `Invoke-RestMethod`

**Option 3: Sử dụng VS Code REST Client**
- Cài extension: REST Client
- Tạo file `.http` để test

### 1.2. Lấy JWT Token

Trước khi test, cần đăng nhập để lấy token:

```bash
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "name": "User Name",
    "email": "user@example.com",
    "role": "Customer"
  }
}
```

**Lưu token vào biến để dùng cho các request sau.**

---

## 2. Test API Wallet

### 2.1. Xem số dư ví

**Endpoint**: `GET /api/wallet`

**Request**:
```bash
GET http://localhost:5000/api/wallet
Authorization: Bearer {YOUR_TOKEN}
```

**Response**:
```json
{
  "id": 1,
  "userId": 1,
  "balance": 1000000,
  "currency": "VND",
  "createdAt": "2024-12-08T10:00:00Z"
}
```

### 2.2. Xem lịch sử giao dịch

**Endpoint**: `GET /api/wallet/transactions`

**Request**:
```bash
GET http://localhost:5000/api/wallet/transactions?page=1&pageSize=20
Authorization: Bearer {YOUR_TOKEN}
```

**Response**:
```json
[
  {
    "id": 1,
    "walletId": 1,
    "type": "deposit",
    "amount": 100000,
    "balanceAfter": 1000000,
    "description": "Nạp tiền qua VietQR",
    "status": "pending",
    "createdAt": "2024-12-08T10:00:00Z",
    "orderId": null,
    "paymentGatewayTransactionId": "BT-20241208100000-1",
    "paymentGateway": "vietqr"
  }
]
```

### 2.3. Nạp tiền bằng VietQR

**Endpoint**: `POST /api/wallet/deposit`

**Request**:
```bash
POST http://localhost:5000/api/wallet/deposit
Authorization: Bearer {YOUR_TOKEN}
Content-Type: application/json

{
  "amount": 100000,
  "description": "Nạp tiền vào ví"
}
```

**Response**:
```json
{
  "paymentUrl": "https://img.vietqr.io/image/970422-0858153779-compact.png?addInfo=Nạp tiền vào ví - BT-20241208100000-1&amount=100000",
  "transactionId": "1",
  "amount": 100000,
  "paymentGateway": "vietqr",
  "description": "Nạp tiền qua VietQR",
  "reference": "BT-20241208100000-1"
}
```

**Test Steps**:
1. Gọi API này để tạo yêu cầu nạp tiền
2. Copy `paymentUrl` và mở trong browser
3. Quét QR code bằng app ngân hàng
4. Chuyển khoản theo số tiền và reference
5. Kiểm tra transaction status trong database (manual confirmation)

### 2.4. Thanh toán đơn hàng bằng ví

**Endpoint**: `POST /api/wallet/pay`

**Request**:
```bash
POST http://localhost:5000/api/wallet/pay
Authorization: Bearer {YOUR_TOKEN}
Content-Type: application/json

{
  "orderId": 123,
  "description": "Thanh toán đơn hàng #123"
}
```

**Response**:
```json
{
  "id": 10,
  "walletId": 1,
  "type": "payment",
  "amount": 500000,
  "balanceAfter": 500000,
  "description": "Thanh toán đơn hàng #123",
  "status": "success",
  "createdAt": "2024-12-08T10:05:00Z",
  "orderId": 123
}
```

### 2.5. Hoàn tiền

**Endpoint**: `POST /api/wallet/refund`

**Request**:
```bash
POST http://localhost:5000/api/wallet/refund
Authorization: Bearer {YOUR_TOKEN}
Content-Type: application/json

{
  "orderId": 123,
  "amount": 500000,
  "description": "Hoàn tiền đơn hàng #123"
}
```

### 2.6. Rút tiền

**Endpoint**: `POST /api/wallet/withdraw`

**Request**:
```bash
POST http://localhost:5000/api/wallet/withdraw
Authorization: Bearer {YOUR_TOKEN}
Content-Type: application/json

{
  "amount": 200000,
  "description": "Rút tiền từ ví"
}
```

---

## 3. Test API WalletRequest

**Lưu ý**: Customer và EnterpriseAdmin đều có thể tạo yêu cầu nạp/rút tiền. SystemAdmin sẽ xem và phê duyệt các yêu cầu này.

### 3.1. Tạo yêu cầu nạp tiền (Customer hoặc EnterpriseAdmin)

**Endpoint**: `POST /api/walletrequest`

**Request**:
```bash
POST http://localhost:5000/api/walletrequest
Authorization: Bearer {CUSTOMER_OR_ENTERPRISE_ADMIN_TOKEN}
Content-Type: application/json

{
  "type": "deposit",
  "amount": 500000,
  "description": "Yêu cầu nạp tiền vào ví"
}
```

**Response**:
```json
{
  "id": 1,
  "userId": 1,
  "userName": "Nguyễn Văn A",
  "userEmail": "customer@example.com",
  "userRole": "Customer",
  "walletId": 1,
  "currentBalance": 1000000,
  "type": "deposit",
  "amount": 500000,
  "description": "Yêu cầu nạp tiền vào ví",
  "status": "pending",
  "rejectionReason": null,
  "processedBy": null,
  "processedByName": null,
  "processedAt": null,
  "createdAt": "2024-12-08T10:00:00Z",
  "updatedAt": null
}
```

### 3.2. Tạo yêu cầu rút tiền (Customer hoặc EnterpriseAdmin)

**Request**:
```bash
POST http://localhost:5000/api/walletrequest
Authorization: Bearer {CUSTOMER_OR_ENTERPRISE_ADMIN_TOKEN}
Content-Type: application/json

{
  "type": "withdraw",
  "amount": 300000,
  "description": "Yêu cầu rút tiền từ ví"
}
```

### 3.3. Xem danh sách yêu cầu (Customer hoặc EnterpriseAdmin - chỉ xem yêu cầu của chính mình)

**Endpoint**: `GET /api/walletrequest`

**Request**:
```bash
GET http://localhost:5000/api/walletrequest?status=pending&page=1&pageSize=20
Authorization: Bearer {CUSTOMER_OR_ENTERPRISE_ADMIN_TOKEN}
```

**Query Parameters**:
- `type`: `deposit` hoặc `withdraw` (optional)
- `status`: `pending`, `approved`, `rejected`, `completed` (optional)
- `page`: Số trang (default: 1)
- `pageSize`: Số item mỗi trang (default: 20)

### 3.4. Xem chi tiết yêu cầu

**Endpoint**: `GET /api/walletrequest/{id}`

**Request**:
```bash
GET http://localhost:5000/api/walletrequest/1
Authorization: Bearer {YOUR_TOKEN}
```

### 3.5. Xem số lượng yêu cầu đang chờ (SystemAdmin only)

**Endpoint**: `GET /api/walletrequest/pending/count`

**Request**:
```bash
GET http://localhost:5000/api/walletrequest/pending/count
Authorization: Bearer {SYSTEM_ADMIN_TOKEN}
```

**Response**:
```json
{
  "count": 5
}
```

### 3.6. Xem tất cả yêu cầu (SystemAdmin only)

**Request**:
```bash
GET http://localhost:5000/api/walletrequest?status=pending&page=1&pageSize=20
Authorization: Bearer {SYSTEM_ADMIN_TOKEN}
```

### 3.7. Phê duyệt yêu cầu (SystemAdmin only)

**Endpoint**: `POST /api/walletrequest/{id}/process`

**Request**:
```bash
POST http://localhost:5000/api/walletrequest/1/process
Authorization: Bearer {SYSTEM_ADMIN_TOKEN}
Content-Type: application/json

{
  "action": "approve"
}
```

**Response**:
```json
{
  "message": "Yêu cầu đã được phê duyệt. Số tiền đã được cập nhật vào ví.",
  "request": {
    "id": 1,
    "userId": 1,
    "userName": "Nguyễn Văn A",
    "userEmail": "customer@example.com",
    "userRole": "Customer",
    "walletId": 1,
    "currentBalance": 1500000,
    "type": "deposit",
    "amount": 500000,
    "description": "Yêu cầu nạp tiền vào ví",
    "status": "completed",
    "processedBy": 999,
    "processedByName": "System Admin",
    "processedAt": "2024-12-08T10:05:00Z",
    "createdAt": "2024-12-08T10:00:00Z",
    "updatedAt": "2024-12-08T10:05:00Z"
  }
}
```

### 3.8. Từ chối yêu cầu (SystemAdmin only)

**Request**:
```bash
POST http://localhost:5000/api/walletrequest/1/process
Authorization: Bearer {SYSTEM_ADMIN_TOKEN}
Content-Type: application/json

{
  "action": "reject",
  "rejectionReason": "Thông tin không hợp lệ"
}
```

**Response**:
```json
{
  "message": "Yêu cầu đã bị từ chối.",
  "request": {
    "id": 1,
    "status": "rejected",
    "rejectionReason": "Thông tin không hợp lệ",
    ...
  }
}
```

---

## 4. Test API Orders (EnterpriseAdmin)

### 4.1. Xem danh sách đơn hàng (EnterpriseAdmin)

**Endpoint**: `GET /api/orders`

**Request**:
```bash
GET http://localhost:5000/api/orders?status=Pending&page=1&pageSize=20
Authorization: Bearer {ENTERPRISE_ADMIN_TOKEN}
```

**Response**:
```json
{
  "items": [
    {
      "id": 123,
      "userId": 456,
      "orderDate": "2024-12-08T10:00:00Z",
      "shippingAddress": "Nguyễn Văn A, 0123456789, 123 Đường ABC, Phường XYZ, Quận 1, TP.HCM",
      "totalAmount": 500000,
      "status": "Pending",
      "customer": {
        "id": 456,
        "name": "Nguyễn Văn A",
        "email": "customer@example.com",
        "phoneNumber": "0123456789",
        "avatarUrl": "https://example.com/avatar.jpg",
        "address": "123 Đường ABC, Phường XYZ, Quận 1, TP.HCM"
      },
      "orderItems": [
        {
          "id": 1,
          "orderId": 123,
          "productId": 10,
          "quantity": 2,
          "price": 250000
        }
      ],
      "payments": [...]
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 50,
  "totalPages": 3
}
```

**Kiểm tra**:
- ✅ EnterpriseAdmin chỉ thấy đơn hàng có sản phẩm của doanh nghiệp mình
- ✅ Mỗi đơn hàng có đầy đủ thông tin Customer (ảnh đại diện, tên, email, địa chỉ)
- ✅ Có mã đơn hàng (id)

### 4.2. Xem chi tiết đơn hàng (EnterpriseAdmin)

**Endpoint**: `GET /api/orders/{id}`

**Request**:
```bash
GET http://localhost:5000/api/orders/123
Authorization: Bearer {ENTERPRISE_ADMIN_TOKEN}
```

**Response**: Tương tự như trên nhưng có đầy đủ thông tin OrderItems và Payments.

---

## 5. Test API SystemAdmin Wallet Management

### 5.1. Xem tổng hợp số tiền hệ thống

**Endpoint**: `GET /api/wallet/system/summary`

**Request**:
```bash
GET http://localhost:5000/api/wallet/system/summary
Authorization: Bearer {SYSTEM_ADMIN_TOKEN}
```

**Response**:
```json
{
  "totalSystemBalance": 50000000,
  "systemAdminBalance": 10000000,
  "allUsersBalance": 40000000,
  "totalUsers": 150,
  "totalCustomers": 120,
  "totalEnterpriseAdmins": 30,
  "breakdown": {
    "customersBalance": 25000000,
    "enterpriseAdminsBalance": 15000000
  }
}
```

### 5.2. Xem danh sách ví của tất cả User

**Endpoint**: `GET /api/wallet/system/users`

**Request**:
```bash
GET http://localhost:5000/api/wallet/system/users?page=1&pageSize=50
Authorization: Bearer {SYSTEM_ADMIN_TOKEN}
```

**Response**:
```json
[
  {
    "userId": 1,
    "userName": "Nguyễn Văn A",
    "userEmail": "customer@example.com",
    "userRole": "Customer",
    "walletId": 1,
    "balance": 500000,
    "currency": "VND",
    "walletCreatedAt": "2024-01-01T00:00:00Z",
    "totalTransactions": 10
  },
  {
    "userId": 2,
    "userName": "Doanh nghiệp ABC",
    "userEmail": "enterprise@example.com",
    "userRole": "EnterpriseAdmin",
    "walletId": 2,
    "balance": 2000000,
    "currency": "VND",
    "walletCreatedAt": "2024-01-02T00:00:00Z",
    "totalTransactions": 25
  }
]
```

### 5.3. Xem ví của user cụ thể

**Endpoint**: `GET /api/wallet/user/{userId}`

**Request**:
```bash
GET http://localhost:5000/api/wallet/user/123
Authorization: Bearer {SYSTEM_ADMIN_TOKEN}
```

**Response**:
```json
{
  "id": 1,
  "userId": 123,
  "balance": 500000,
  "currency": "VND",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### 5.4. Cập nhật số dư ví của user (Cộng tiền)

**Endpoint**: `PUT /api/wallet/user/{userId}/balance`

**Request**:
```bash
PUT http://localhost:5000/api/wallet/user/123/balance
Authorization: Bearer {SYSTEM_ADMIN_TOKEN}
Content-Type: application/json

{
  "amount": 100000,
  "description": "Bồi thường cho khách hàng"
}
```

**Response**:
```json
{
  "message": "Đã cộng 100,000 VND vào ví của user.",
  "transaction": {
    "id": 100,
    "walletId": 1,
    "type": "deposit",
    "amount": 100000,
    "balanceAfter": 600000,
    "description": "[SystemAdmin] Bồi thường cho khách hàng",
    "status": "success",
    "createdAt": "2024-12-08T14:30:00Z",
    "paymentGateway": "admin"
  }
}
```

### 5.5. Cập nhật số dư ví của user (Trừ tiền)

**Request**:
```bash
PUT http://localhost:5000/api/wallet/user/123/balance
Authorization: Bearer {SYSTEM_ADMIN_TOKEN}
Content-Type: application/json

{
  "amount": -50000,
  "description": "Phạt vi phạm quy định"
}
```

**Response**:
```json
{
  "message": "Đã trừ 50,000 VND từ ví của user.",
  "transaction": {
    "id": 101,
    "walletId": 1,
    "type": "withdraw",
    "amount": 50000,
    "balanceAfter": 550000,
    "description": "[SystemAdmin] Phạt vi phạm quy định",
    "status": "success",
    "createdAt": "2024-12-08T14:35:00Z",
    "paymentGateway": "admin"
  }
}
```

### 5.6. Đảm bảo tất cả user đều có ví

**Endpoint**: `POST /api/wallet/system/ensure-all-wallets`

**Request**:
```bash
POST http://localhost:5000/api/wallet/system/ensure-all-wallets
Authorization: Bearer {SYSTEM_ADMIN_TOKEN}
```

**Response**:
```json
{
  "message": "Đã tạo ví cho 50 user chưa có ví.",
  "createdWalletsCount": 50
}
```

---

## 6. Test Scenarios

### Scenario 1: Customer nạp tiền và thanh toán đơn hàng

1. **Customer đăng nhập** → Lấy token
2. **Xem số dư ví** → `GET /api/wallet`
3. **Tạo yêu cầu nạp tiền** → `POST /api/walletrequest` với `type: "deposit"` (Customer tự tạo yêu cầu)
4. **SystemAdmin xem yêu cầu** → `GET /api/walletrequest?status=pending` (SystemAdmin token)
5. **SystemAdmin phê duyệt** → `POST /api/walletrequest/{id}/process` với `action: "approve"` (SystemAdmin token)
6. **Customer kiểm tra số dư mới** → `GET /api/wallet` (số dư đã tăng)
7. **Customer tạo đơn hàng** → `POST /api/orders`
8. **Customer thanh toán bằng ví** → `POST /api/wallet/pay`
9. **Customer kiểm tra số dư sau thanh toán** → `GET /api/wallet` (số dư đã giảm)

**Lưu ý**: Customer có thể tự tạo yêu cầu nạp/rút tiền, nhưng cần SystemAdmin phê duyệt thì số tiền mới được cập nhật vào ví.

### Scenario 2: EnterpriseAdmin xem thông tin customer đặt hàng

1. **EnterpriseAdmin đăng nhập** → Lấy token
2. **Xem danh sách đơn hàng** → `GET /api/orders`
3. **Kiểm tra mỗi đơn hàng có**:
   - ✅ Mã đơn hàng (`id`)
   - ✅ Thông tin Customer (`customer.id`, `customer.name`, `customer.email`)
   - ✅ Ảnh đại diện (`customer.avatarUrl`)
   - ✅ Địa chỉ (`customer.address`)
   - ✅ Địa chỉ giao hàng (`shippingAddress`)

### Scenario 3: SystemAdmin quản lý ví

1. **SystemAdmin đăng nhập** → Lấy token
2. **Xem tổng hợp số tiền** → `GET /api/wallet/system/summary`
3. **Xem danh sách ví user** → `GET /api/wallet/system/users`
4. **Xem ví của user cụ thể** → `GET /api/wallet/user/{userId}`
5. **Cập nhật số dư** → `PUT /api/wallet/user/{userId}/balance`
6. **Kiểm tra transaction được tạo** → `GET /api/wallet/transactions` của user đó

---

## 7. Test với cURL (PowerShell)

### Ví dụ: Test nạp tiền

```powershell
# Lấy token
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body '{"email":"customer@example.com","password":"password123"}'

$token = $loginResponse.token

# Tạo yêu cầu nạp tiền
$depositRequest = @{
    amount = 100000
    description = "Nạp tiền vào ví"
} | ConvertTo-Json

$depositResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/walletrequest" `
    -Method POST `
    -ContentType "application/json" `
    -Headers @{Authorization = "Bearer $token"} `
    -Body $depositRequest

Write-Host "Request ID: $($depositResponse.id)"
Write-Host "Status: $($depositResponse.status)"
```

---

## 8. Test với VS Code REST Client

Tạo file `test-api.http`:

```http
### Login
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "customer@example.com",
  "password": "password123"
}

### Get Wallet Balance
GET http://localhost:5000/api/wallet
Authorization: Bearer {{token}}

### Create Deposit Request
POST http://localhost:5000/api/walletrequest
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "type": "deposit",
  "amount": 100000,
  "description": "Nạp tiền vào ví"
}

### Get Wallet Requests
GET http://localhost:5000/api/walletrequest?status=pending
Authorization: Bearer {{token}}

### SystemAdmin: Approve Request
POST http://localhost:5000/api/walletrequest/1/process
Authorization: Bearer {{systemAdminToken}}
Content-Type: application/json

{
  "action": "approve"
}

### EnterpriseAdmin: Get Orders
GET http://localhost:5000/api/orders
Authorization: Bearer {{enterpriseAdminToken}}

### SystemAdmin: Get System Summary
GET http://localhost:5000/api/wallet/system/summary
Authorization: Bearer {{systemAdminToken}}
```

---

## 9. Checklist Test

### Wallet APIs
- [ ] GET /api/wallet - Xem số dư ví
- [ ] GET /api/wallet/transactions - Xem lịch sử giao dịch
- [ ] POST /api/wallet/deposit - Nạp tiền bằng VietQR
- [ ] POST /api/wallet/pay - Thanh toán đơn hàng
- [ ] POST /api/wallet/refund - Hoàn tiền
- [ ] POST /api/wallet/withdraw - Rút tiền

### WalletRequest APIs
- [ ] POST /api/walletrequest - Tạo yêu cầu nạp/rút tiền
- [ ] GET /api/walletrequest - Xem danh sách yêu cầu
- [ ] GET /api/walletrequest/{id} - Xem chi tiết yêu cầu
- [ ] GET /api/walletrequest/pending/count - Xem số lượng yêu cầu đang chờ (SystemAdmin)
- [ ] POST /api/walletrequest/{id}/process - Phê duyệt/từ chối yêu cầu (SystemAdmin)

### SystemAdmin Wallet Management
- [ ] GET /api/wallet/system/summary - Tổng hợp số tiền hệ thống
- [ ] GET /api/wallet/system/users - Danh sách ví của tất cả User
- [ ] GET /api/wallet/user/{userId} - Xem ví của user cụ thể
- [ ] PUT /api/wallet/user/{userId}/balance - Cập nhật số dư ví
- [ ] POST /api/wallet/system/ensure-all-wallets - Đảm bảo tất cả user có ví

### EnterpriseAdmin Orders
- [ ] GET /api/orders - Xem danh sách đơn hàng (có thông tin Customer)
- [ ] GET /api/orders/{id} - Xem chi tiết đơn hàng (có thông tin Customer)

---

## 10. Lưu ý khi Test

1. **Token Expiry**: Token có thời hạn, nếu hết hạn cần đăng nhập lại
2. **Role Permissions**: Đảm bảo dùng đúng token cho từng role
3. **Database State**: Một số test có thể phụ thuộc vào dữ liệu trong database
4. **VietQR**: QR code URL có thể test bằng cách mở trong browser
5. **Manual Confirmation**: Nạp tiền qua VietQR cần xác nhận thủ công trong database

---

**Version**: 1.0  
**Last Updated**: 2024-12-08  
**Author**: GiaLai OCOP Team

