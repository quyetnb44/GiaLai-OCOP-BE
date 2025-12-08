# 📱 Frontend Integration Guide

Tài liệu tổng hợp các chức năng đã thêm và thay đổi để tích hợp với Frontend.

**Last Updated**: 2024-12-08

---

## 📋 Mục Lục

1. [Wallet System](#1-wallet-system)
2. [BankAccount Management](#2-bankaccount-management)
3. [WalletRequest System](#3-walletrequest-system)
4. [SystemAdmin Wallet Management](#4-systemadmin-wallet-management)
5. [EnterpriseAdmin Orders với Customer Info](#5-enterpriseadmin-orders-với-customer-info)
6. [VietQR Payment Integration](#6-vietqr-payment-integration)

---

## 1. Wallet System

### 1.1. Xem số dư ví

**Endpoint**: `GET /api/wallet`

**Authorization**: Required (Bearer Token)

**Response**:
```json
{
  "id": 1,
  "userId": 123,
  "balance": 1000000,
  "currency": "VND",
  "createdAt": "2024-12-08T10:00:00Z"
}
```

**Frontend Integration**:
```javascript
// React/Vue example
const getWallet = async () => {
  const response = await fetch('/api/wallet', {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  const wallet = await response.json();
  return wallet;
};
```

### 1.2. Xem lịch sử giao dịch

**Endpoint**: `GET /api/wallet/transactions?page=1&pageSize=20`

**Query Parameters**:
- `page`: Số trang (default: 1)
- `pageSize`: Số item mỗi trang (default: 20, max: 100)

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

**Transaction Types**:
- `deposit`: Nạp tiền
- `withdraw`: Rút tiền
- `payment`: Thanh toán đơn hàng
- `refund`: Hoàn tiền

**Transaction Status**:
- `pending`: Đang chờ
- `success`: Thành công
- `failed`: Thất bại

### 1.3. Nạp tiền bằng VietQR

**Endpoint**: `POST /api/wallet/deposit`

**Request**:
```json
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

**Frontend Integration**:
```javascript
// Tạo yêu cầu nạp tiền
const deposit = async (amount, description) => {
  const response = await fetch('/api/wallet/deposit', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      amount: amount,
      description: description || 'Nạp tiền vào ví'
    })
  });
  const result = await response.json();
  
  // Hiển thị QR code
  // result.paymentUrl là URL của QR code image
  // result.reference là mã tham chiếu để user ghi chú khi chuyển khoản
  
  return result;
};
```

**UI Flow**:
1. User nhập số tiền và mô tả
2. Gọi API `/api/wallet/deposit`
3. Hiển thị QR code từ `paymentUrl`
4. Hiển thị thông tin:
   - Số tiền: `amount`
   - Mã tham chiếu: `reference`
   - Hướng dẫn: "Quét QR code và chuyển khoản với nội dung: {reference}"
5. User quét QR và chuyển khoản
6. User tự xác nhận giao dịch (manual confirmation)

### 1.4. Thanh toán đơn hàng bằng ví

**Endpoint**: `POST /api/wallet/pay`

**Request**:
```json
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

**Frontend Integration**:
```javascript
const payOrder = async (orderId) => {
  const response = await fetch('/api/wallet/pay', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      orderId: orderId,
      description: `Thanh toán đơn hàng #${orderId}`
    })
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Thanh toán thất bại');
  }
  
  return await response.json();
};
```

### 1.5. Hoàn tiền

**Endpoint**: `POST /api/wallet/refund`

**Request**:
```json
{
  "orderId": 123,
  "amount": 50000,
  "description": "Hoàn tiền đơn hàng #123"
}
```

### 1.6. Rút tiền

**Endpoint**: `POST /api/wallet/withdraw`

**Request**:
```json
{
  "amount": 200000,
  "description": "Rút tiền từ ví"
}
```

**Note**: Rút tiền sẽ trừ trực tiếp từ ví. Nếu muốn có quy trình phê duyệt, sử dụng WalletRequest API.

---

## 2. BankAccount Management

### 2.1. Tạo tài khoản ngân hàng

**Endpoint**: `POST /api/bankaccount`

**Authorization**: Required (Customer hoặc EnterpriseAdmin)

**Request**:
```json
{
  "bankCode": "970422",
  "bankName": "MB Bank",
  "accountNumber": "0858153779",
  "accountName": "NGUYEN BA QUYET",
  "branch": "Chi nhánh Hà Nội",
  "isDefault": false
}
```

**Response**:
```json
{
  "id": 1,
  "userId": 123,
  "bankCode": "970422",
  "bankName": "MB Bank",
  "accountNumber": "0858153779",
  "accountName": "NGUYEN BA QUYET",
  "branch": "Chi nhánh Hà Nội",
  "isDefault": false,
  "isActive": true,
  "createdAt": "2024-12-08T10:00:00Z",
  "updatedAt": null,
  "qrCodeUrl": "https://img.vietqr.io/image/970422-0858153779-compact.png"
}
```

**Frontend Integration**:
```javascript
const createBankAccount = async (bankData) => {
  const response = await fetch('/api/bankaccount', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(bankData)
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Tạo tài khoản ngân hàng thất bại');
  }
  
  return await response.json();
};
```

### 2.2. Xem danh sách tài khoản ngân hàng

**Endpoint**: `GET /api/bankaccount`

**Response**:
```json
[
  {
    "id": 1,
    "userId": 123,
    "bankCode": "970422",
    "bankName": "MB Bank",
    "accountNumber": "0858153779",
    "accountName": "NGUYEN BA QUYET",
    "branch": "Chi nhánh Hà Nội",
    "isDefault": true,
    "isActive": true,
    "createdAt": "2024-12-08T10:00:00Z",
    "qrCodeUrl": "https://img.vietqr.io/image/970422-0858153779-compact.png"
  }
]
```

**Frontend Integration**:
```javascript
const getBankAccounts = async () => {
  const response = await fetch('/api/bankaccount', {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  return await response.json();
};
```

### 2.3. Xem tài khoản ngân hàng mặc định

**Endpoint**: `GET /api/bankaccount/default`

**Response**: Tương tự như trong danh sách nhưng chỉ trả về tài khoản mặc định.

### 2.4. Xem chi tiết tài khoản ngân hàng

**Endpoint**: `GET /api/bankaccount/{id}`

**Response**: Tương tự như trong danh sách.

### 2.5. Cập nhật tài khoản ngân hàng

**Endpoint**: `PUT /api/bankaccount/{id}`

**Request**:
```json
{
  "bankCode": "970422",
  "bankName": "MB Bank",
  "accountNumber": "0858153779",
  "accountName": "NGUYEN BA QUYET",
  "branch": "Chi nhánh TP.HCM",
  "isDefault": true,
  "isActive": true
}
```

**Note**: Tất cả các trường đều optional. Chỉ cập nhật các trường được gửi lên.

### 2.6. Xóa tài khoản ngân hàng

**Endpoint**: `DELETE /api/bankaccount/{id}`

**Note**: Không thể xóa tài khoản đang được sử dụng trong yêu cầu rút tiền chưa hoàn thành.

### 2.7. Đặt làm tài khoản mặc định

**Endpoint**: `POST /api/bankaccount/{id}/set-default`

**Response**: Tài khoản được đặt làm mặc định, các tài khoản khác sẽ tự động bỏ mặc định.

**Frontend Integration**:
```javascript
const setDefaultBankAccount = async (bankAccountId) => {
  const response = await fetch(`/api/bankaccount/${bankAccountId}/set-default`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Đặt tài khoản mặc định thất bại');
  }
  
  return await response.json();
};
```

**UI Flow**:
1. User vào trang "Quản lý tài khoản ngân hàng"
2. Hiển thị danh sách tài khoản đã thêm
3. Mỗi tài khoản hiển thị:
   - Tên ngân hàng
   - Số tài khoản (có thể ẩn một phần)
   - Tên chủ tài khoản
   - QR Code (hiển thị từ `qrCodeUrl`)
   - Badge "Mặc định" nếu `isDefault = true`
4. User có thể:
   - Thêm tài khoản mới
   - Chỉnh sửa tài khoản
   - Xóa tài khoản
   - Đặt làm mặc định
   - Xem QR Code

---

## 3. WalletRequest System

### 2.1. Tạo yêu cầu nạp/rút tiền

**Endpoint**: `POST /api/walletrequest`

**Authorization**: Required (Customer hoặc EnterpriseAdmin)

**Request**:
```json
{
  "type": "deposit",  // "deposit" hoặc "withdraw"
  "amount": 500000,
  "description": "Yêu cầu nạp tiền vào ví",
  "bankAccountId": 1  // Bắt buộc khi type = "withdraw", optional khi type = "deposit"
}
```

**Note**: Khi `type = "withdraw"`, `bankAccountId` là bắt buộc. Đây là tài khoản ngân hàng thụ hưởng mà SystemAdmin sẽ chuyển tiền vào.

**Response**:
```json
{
  "id": 1,
  "userId": 123,
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

**Frontend Integration**:
```javascript
// Customer/EnterpriseAdmin tạo yêu cầu
const createWalletRequest = async (type, amount, description) => {
  const response = await fetch('/api/walletrequest', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      type: type, // "deposit" hoặc "withdraw"
      amount: amount,
      description: description
    })
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Tạo yêu cầu thất bại');
  }
  
  return await response.json();
};
```

**UI Flow cho Customer**:
1. User chọn "Nạp tiền" hoặc "Rút tiền"
2. Nhập số tiền và mô tả
3. **Nếu rút tiền**: Chọn tài khoản ngân hàng thụ hưởng từ danh sách (hoặc thêm mới nếu chưa có)
4. Gọi API `POST /api/walletrequest` với `bankAccountId` (nếu rút tiền)
5. Hiển thị thông báo: "Yêu cầu đã được gửi. Vui lòng chờ SystemAdmin phê duyệt."
6. Hiển thị trạng thái: "pending"
7. User có thể xem danh sách yêu cầu của mình

### 2.2. Xem danh sách yêu cầu

**Endpoint**: `GET /api/walletrequest?status=pending&page=1&pageSize=20`

**Query Parameters**:
- `type`: `deposit` hoặc `withdraw` (optional)
- `status`: `pending`, `approved`, `rejected`, `completed` (optional)
- `page`: Số trang (default: 1)
- `pageSize`: Số item mỗi trang (default: 20)

**Response**:
```json
[
  {
    "id": 1,
    "userId": 123,
    "userName": "Nguyễn Văn A",
    "userEmail": "customer@example.com",
    "userRole": "Customer",
    "walletId": 1,
    "currentBalance": 1000000,
    "type": "deposit",
    "amount": 500000,
    "description": "Yêu cầu nạp tiền vào ví",
    "status": "pending",
    "createdAt": "2024-12-08T10:00:00Z"
  }
]
```

**Frontend Integration**:
```javascript
// Customer xem yêu cầu của mình
const getMyRequests = async (status = null, page = 1) => {
  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: '20'
  });
  if (status) params.append('status', status);
  
  const response = await fetch(`/api/walletrequest?${params}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  return await response.json();
};
```

### 2.3. Xem chi tiết yêu cầu

**Endpoint**: `GET /api/walletrequest/{id}`

**Response**: Tương tự như trong danh sách nhưng có đầy đủ thông tin.

### 2.4. SystemAdmin: Xem số lượng yêu cầu đang chờ

**Endpoint**: `GET /api/walletrequest/pending/count`

**Authorization**: Required (SystemAdmin only)

**Response**:
```json
{
  "count": 5
}
```

**Frontend Integration**:
```javascript
// SystemAdmin xem số lượng yêu cầu đang chờ (cho badge notification)
const getPendingRequestsCount = async () => {
  const response = await fetch('/api/walletrequest/pending/count', {
    headers: {
      'Authorization': `Bearer ${systemAdminToken}`
    }
  });
  const data = await response.json();
  return data.count;
};
```

### 2.5. SystemAdmin: Xem tất cả yêu cầu

**Endpoint**: `GET /api/walletrequest?status=pending&page=1&pageSize=20`

**Authorization**: Required (SystemAdmin only)

**Response**: Danh sách tất cả yêu cầu của tất cả user.

**Frontend Integration**:
```javascript
// SystemAdmin xem tất cả yêu cầu
const getAllRequests = async (status = 'pending', page = 1) => {
  const params = new URLSearchParams({
    status: status,
    page: page.toString(),
    pageSize: '20'
  });
  
  const response = await fetch(`/api/walletrequest?${params}`, {
    headers: {
      'Authorization': `Bearer ${systemAdminToken}`
    }
  });
  
  return await response.json();
};
```

### 2.6. SystemAdmin: Phê duyệt yêu cầu

**Endpoint**: `POST /api/walletrequest/{id}/process`

**Authorization**: Required (SystemAdmin only)

**Request**:
```json
{
  "action": "approve"  // "approve" hoặc "reject"
}
```

**Response khi approve**:
```json
{
  "message": "Yêu cầu đã được phê duyệt. Số tiền đã được cập nhật vào ví.",
  "request": {
    "id": 1,
    "status": "completed",
    "currentBalance": 1500000,  // Số dư mới sau khi cộng/trừ
    "processedBy": 999,
    "processedByName": "System Admin",
    "processedAt": "2024-12-08T10:05:00Z"
  }
}
```

**Request khi reject**:
```json
{
  "action": "reject",
  "rejectionReason": "Thông tin không hợp lệ"
}
```

**Response khi reject**:
```json
{
  "message": "Yêu cầu đã bị từ chối.",
  "request": {
    "id": 1,
    "status": "rejected",
    "rejectionReason": "Thông tin không hợp lệ",
    "processedBy": 999,
    "processedAt": "2024-12-08T10:05:00Z"
  }
}
```

**Frontend Integration**:
```javascript
// SystemAdmin phê duyệt yêu cầu
const approveRequest = async (requestId) => {
  const response = await fetch(`/api/walletrequest/${requestId}/process`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${systemAdminToken}`
    },
    body: JSON.stringify({
      action: 'approve'
    })
  });
  
  return await response.json();
};

// SystemAdmin từ chối yêu cầu
const rejectRequest = async (requestId, reason) => {
  const response = await fetch(`/api/walletrequest/${requestId}/process`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${systemAdminToken}`
    },
    body: JSON.stringify({
      action: 'reject',
      rejectionReason: reason
    })
  });
  
  return await response.json();
};
```

**UI Flow cho SystemAdmin**:
1. SystemAdmin xem danh sách yêu cầu đang chờ
2. Mỗi yêu cầu hiển thị:
   - User info (tên, email, role)
   - Loại yêu cầu (nạp/rút)
   - Số tiền
   - Mô tả
   - Số dư hiện tại
   - **Thông tin ngân hàng thụ hưởng** (nếu rút tiền):
     - Tên ngân hàng
     - Số tài khoản
     - Tên chủ tài khoản
     - Chi nhánh
     - QR Code để chuyển khoản
   - Thời gian tạo
3. **Nếu rút tiền**: SystemAdmin xem thông tin ngân hàng và QR Code, sau đó chuyển khoản thủ công vào tài khoản đó
4. **Nếu nạp tiền**: SystemAdmin chuyển khoản từ tài khoản của user vào tài khoản SystemAdmin
5. Sau khi chuyển khoản, SystemAdmin click "Phê duyệt"
6. Hệ thống tự động cộng/trừ tiền vào ví của user
7. User nhận thông báo yêu cầu đã được phê duyệt

---

## 3. SystemAdmin Wallet Management

### 3.1. Xem tổng hợp số tiền hệ thống

**Endpoint**: `GET /api/wallet/system/summary`

**Authorization**: Required (SystemAdmin only)

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

**Frontend Integration**:
```javascript
const getSystemSummary = async () => {
  const response = await fetch('/api/wallet/system/summary', {
    headers: {
      'Authorization': `Bearer ${systemAdminToken}`
    }
  });
  return await response.json();
};
```

**UI Display**:
- Dashboard card hiển thị:
  - Tổng số tiền hệ thống
  - Số tiền SystemAdmin
  - Số tiền của tất cả User
  - Breakdown: Customer vs EnterpriseAdmin

### 3.2. Xem danh sách ví của tất cả User

**Endpoint**: `GET /api/wallet/system/users?page=1&pageSize=50`

**Authorization**: Required (SystemAdmin only)

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
  }
]
```

**Frontend Integration**:
```javascript
const getAllUserWallets = async (page = 1) => {
  const response = await fetch(`/api/wallet/system/users?page=${page}&pageSize=50`, {
    headers: {
      'Authorization': `Bearer ${systemAdminToken}`
    }
  });
  return await response.json();
};
```

**UI Display**:
- Table hiển thị:
  - User Name, Email, Role
  - Balance
  - Total Transactions
  - Wallet Created Date
- Có thể sort theo balance, filter theo role

### 3.3. Xem ví của user cụ thể

**Endpoint**: `GET /api/wallet/user/{userId}`

**Authorization**: Required (SystemAdmin only)

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

### 3.4. Cập nhật số dư ví của user

**Endpoint**: `PUT /api/wallet/user/{userId}/balance`

**Authorization**: Required (SystemAdmin only)

**Request**:
```json
{
  "amount": 100000,  // Dương = cộng, Âm = trừ
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

**Frontend Integration**:
```javascript
// Cộng tiền
const addMoney = async (userId, amount, description) => {
  const response = await fetch(`/api/wallet/user/${userId}/balance`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${systemAdminToken}`
    },
    body: JSON.stringify({
      amount: Math.abs(amount),  // Đảm bảo dương
      description: description
    })
  });
  return await response.json();
};

// Trừ tiền
const subtractMoney = async (userId, amount, description) => {
  const response = await fetch(`/api/wallet/user/${userId}/balance`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${systemAdminToken}`
    },
    body: JSON.stringify({
      amount: -Math.abs(amount),  // Đảm bảo âm
      description: description
    })
  });
  return await response.json();
};
```

**UI Flow**:
1. SystemAdmin chọn user từ danh sách
2. Xem số dư hiện tại
3. Chọn "Cộng tiền" hoặc "Trừ tiền"
4. Nhập số tiền và lý do
5. Confirm action
6. Hiển thị kết quả và transaction mới

---

## 4. EnterpriseAdmin Orders với Customer Info

### 4.1. Xem danh sách đơn hàng

**Endpoint**: `GET /api/orders?status=Pending&page=1&pageSize=20`

**Authorization**: Required (EnterpriseAdmin)

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
      "orderItems": [...],
      "payments": [...]
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 50,
  "totalPages": 3
}
```

**Frontend Integration**:
```javascript
const getOrders = async (status = null, page = 1) => {
  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: '20'
  });
  if (status) params.append('status', status);
  
  const response = await fetch(`/api/orders?${params}`, {
    headers: {
      'Authorization': `Bearer ${enterpriseAdminToken}`
    }
  });
  
  return await response.json();
};
```

**UI Display**:
- Mỗi đơn hàng hiển thị:
  - **Mã đơn hàng**: `id`
  - **Thông tin Customer**:
    - Avatar: `customer.avatarUrl` (hiển thị ảnh đại diện)
    - Tên: `customer.name`
    - Email: `customer.email`
    - Số điện thoại: `customer.phoneNumber`
    - Địa chỉ: `customer.address`
  - **Địa chỉ giao hàng**: `shippingAddress`
  - **Tổng tiền**: `totalAmount`
  - **Trạng thái**: `status`
  - **Ngày đặt**: `orderDate`

**Component Example (React)**:
```jsx
function OrderCard({ order }) {
  return (
    <div className="order-card">
      <div className="order-header">
        <span className="order-id">Đơn hàng #{order.id}</span>
        <span className="order-status">{order.status}</span>
      </div>
      
      <div className="customer-info">
        <img src={order.customer.avatarUrl || '/default-avatar.png'} 
             alt={order.customer.name} 
             className="customer-avatar" />
        <div>
          <h4>{order.customer.name}</h4>
          <p>{order.customer.email}</p>
          <p>{order.customer.phoneNumber}</p>
          <p>{order.customer.address}</p>
        </div>
      </div>
      
      <div className="shipping-info">
        <strong>Địa chỉ giao hàng:</strong>
        <p>{order.shippingAddress}</p>
      </div>
      
      <div className="order-footer">
        <span className="total-amount">
          {order.totalAmount.toLocaleString('vi-VN')} VND
        </span>
        <span className="order-date">
          {new Date(order.orderDate).toLocaleDateString('vi-VN')}
        </span>
      </div>
    </div>
  );
}
```

### 4.2. Xem chi tiết đơn hàng

**Endpoint**: `GET /api/orders/{id}`

**Authorization**: Required (EnterpriseAdmin)

**Response**: Tương tự như trên nhưng có đầy đủ `orderItems` và `payments`.

---

## 5. VietQR Payment Integration

### 5.1. Tạo QR Code cho thanh toán

**QR Code URL Format**:
```
https://img.vietqr.io/image/{BankCode}-{AccountNumber}-{Template}.png?addInfo={Description}&amount={Amount}
```

**Example**:
```
https://img.vietqr.io/image/970422-0858153779-compact.png?addInfo=Nạp tiền vào ví - BT-20241208100000-1&amount=100000
```

**Frontend Integration**:
```javascript
// Hiển thị QR code từ URL
function QRCodeDisplay({ qrUrl, amount, reference }) {
  return (
    <div className="qr-payment">
      <img src={qrUrl} alt="QR Code" className="qr-image" />
      <div className="payment-info">
        <p><strong>Số tiền:</strong> {amount.toLocaleString('vi-VN')} VND</p>
        <p><strong>Mã tham chiếu:</strong> {reference}</p>
        <p className="instruction">
          Quét QR code và chuyển khoản với nội dung: <strong>{reference}</strong>
        </p>
      </div>
    </div>
  );
}
```

### 5.2. Thông tin ngân hàng SystemAdmin

**Bank Info** (từ appsettings.json):
- **Bank Code**: 970422 (MB Bank)
- **Account Number**: 0858153779
- **Account Name**: NGUYEN BA QUYET

**Frontend có thể hiển thị thông tin này khi cần**:
```javascript
const bankInfo = {
  bankCode: '970422',
  bankName: 'MB Bank',
  accountNumber: '0858153779',
  accountName: 'NGUYEN BA QUYET'
};
```

---

## 6. Error Handling

### 6.1. Common Error Responses

**400 Bad Request**:
```json
{
  "message": "Số tiền phải từ 1,000 VND đến 100,000,000 VND."
}
```

**401 Unauthorized**:
```json
"Không tìm thấy thông tin người dùng trong token."
```

**403 Forbid**:
```json
"Chỉ SystemAdmin mới có thể xử lý yêu cầu."
```

**404 Not Found**:
```json
{
  "message": "Yêu cầu không tồn tại."
}
```

**500 Internal Server Error**:
```json
{
  "message": "Lỗi khi xử lý yêu cầu. Vui lòng thử lại."
}
```

### 6.2. Frontend Error Handling

```javascript
const handleApiCall = async (apiCall) => {
  try {
    const response = await apiCall();
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Có lỗi xảy ra');
    }
    return await response.json();
  } catch (error) {
    console.error('API Error:', error);
    // Hiển thị thông báo lỗi cho user
    showError(error.message);
    throw error;
  }
};
```

---

## 7. UI/UX Recommendations

### 7.1. Wallet Dashboard (Customer/EnterpriseAdmin)

**Components**:
- Balance Card: Hiển thị số dư hiện tại
- Quick Actions:
  - Nạp tiền (tạo WalletRequest)
  - Rút tiền (tạo WalletRequest)
  - Xem lịch sử giao dịch
- Recent Transactions: 5 giao dịch gần nhất
- Pending Requests: Yêu cầu đang chờ phê duyệt

### 7.2. WalletRequest Management (Customer/EnterpriseAdmin)

**Components**:
- Request Form: Tạo yêu cầu nạp/rút tiền
- Request List: Danh sách yêu cầu với status
- Status Badges:
  - `pending`: Màu vàng (đang chờ)
  - `completed`: Màu xanh (đã phê duyệt)
  - `rejected`: Màu đỏ (đã từ chối)

### 7.3. SystemAdmin Dashboard

**Components**:
- System Summary Card: Tổng hợp số tiền hệ thống
- Pending Requests Badge: Số lượng yêu cầu đang chờ
- User Wallets Table: Danh sách ví của tất cả user
- Quick Actions:
  - Xem tổng hợp
  - Xem yêu cầu đang chờ
  - Quản lý ví user

### 7.4. EnterpriseAdmin Orders

**Components**:
- Orders List với Customer Info:
  - Avatar hiển thị ảnh đại diện
  - Customer name, email, phone
  - Customer address
  - Shipping address
- Order Details Modal: Chi tiết đơn hàng với đầy đủ thông tin customer

---

## 8. State Management Recommendations

### 8.1. Wallet State

```javascript
// Redux/Zustand example
const walletStore = {
  balance: 0,
  transactions: [],
  pendingRequests: [],
  
  // Actions
  fetchWallet: async () => { ... },
  fetchTransactions: async () => { ... },
  createDepositRequest: async (amount, description) => { ... },
  createWithdrawRequest: async (amount, description) => { ... }
};
```

### 8.2. WalletRequest State (SystemAdmin)

```javascript
const walletRequestStore = {
  requests: [],
  pendingCount: 0,
  
  // Actions
  fetchRequests: async (status) => { ... },
  fetchPendingCount: async () => { ... },
  approveRequest: async (requestId) => { ... },
  rejectRequest: async (requestId, reason) => { ... }
};
```

---

## 9. Testing Checklist

### 9.1. Wallet APIs
- [ ] GET /api/wallet - Xem số dư
- [ ] GET /api/wallet/transactions - Lịch sử giao dịch
- [ ] POST /api/wallet/deposit - Nạp tiền (VietQR)
- [ ] POST /api/wallet/pay - Thanh toán đơn hàng
- [ ] POST /api/wallet/refund - Hoàn tiền
- [ ] POST /api/wallet/withdraw - Rút tiền

### 9.2. WalletRequest APIs
- [ ] POST /api/walletrequest - Tạo yêu cầu
- [ ] GET /api/walletrequest - Xem danh sách
- [ ] GET /api/walletrequest/{id} - Xem chi tiết
- [ ] GET /api/walletrequest/pending/count - Số lượng đang chờ (SystemAdmin)
- [ ] POST /api/walletrequest/{id}/process - Phê duyệt/từ chối (SystemAdmin)

### 9.3. SystemAdmin APIs
- [ ] GET /api/wallet/system/summary - Tổng hợp số tiền
- [ ] GET /api/wallet/system/users - Danh sách ví user
- [ ] GET /api/wallet/user/{userId} - Xem ví user
- [ ] PUT /api/wallet/user/{userId}/balance - Cập nhật số dư

### 9.4. EnterpriseAdmin Orders
- [ ] GET /api/orders - Danh sách đơn hàng với Customer info
- [ ] GET /api/orders/{id} - Chi tiết đơn hàng với Customer info

---

## 10. Important Notes

### 10.1. Authentication
- Tất cả APIs đều yêu cầu Bearer Token trong header
- Token được lấy từ `/api/auth/login`

### 10.2. Role Permissions
- **Customer**: 
  - Xem ví của mình
  - Tạo WalletRequest
  - Xem WalletRequest của mình
  
- **EnterpriseAdmin**:
  - Xem ví của mình
  - Tạo WalletRequest
  - Xem WalletRequest của mình
  - Xem đơn hàng với Customer info
  
- **SystemAdmin**:
  - Tất cả quyền của Customer và EnterpriseAdmin
  - Xem tổng hợp số tiền hệ thống
  - Xem tất cả WalletRequest
  - Phê duyệt/từ chối WalletRequest
  - Xem và cập nhật ví của bất kỳ user nào

### 10.3. VietQR Payment Flow
1. User tạo yêu cầu nạp tiền → Backend tạo QR URL
2. Frontend hiển thị QR code
3. User quét QR và chuyển khoản
4. **Không có callback tự động** - User tự xác nhận thủ công
5. SystemAdmin phê duyệt yêu cầu → Số tiền được cộng vào ví

### 10.4. WalletRequest Flow
1. Customer/EnterpriseAdmin tạo yêu cầu → Status: "pending"
2. SystemAdmin xem yêu cầu và chuyển khoản thủ công
3. SystemAdmin phê duyệt → Status: "completed", số tiền được cập nhật
4. Hoặc SystemAdmin từ chối → Status: "rejected", không thay đổi số dư

---

## 11. Example Integration (React)

### 11.1. Wallet Component

```jsx
import { useState, useEffect } from 'react';

function WalletPage() {
  const [wallet, setWallet] = useState(null);
  const [transactions, setTransactions] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchWallet();
    fetchTransactions();
  }, []);

  const fetchWallet = async () => {
    const response = await fetch('/api/wallet', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const data = await response.json();
    setWallet(data);
  };

  const fetchTransactions = async () => {
    const response = await fetch('/api/wallet/transactions?page=1&pageSize=20', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const data = await response.json();
    setTransactions(data);
  };

  const handleDeposit = async (amount, description) => {
    const response = await fetch('/api/wallet/deposit', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ amount, description })
    });
    const result = await response.json();
    // Hiển thị QR code modal
    showQRCodeModal(result.paymentUrl, result.reference);
  };

  return (
    <div>
      <div className="balance-card">
        <h2>Số dư ví</h2>
        <p className="balance">{wallet?.balance.toLocaleString('vi-VN')} VND</p>
      </div>
      
      <button onClick={() => handleDeposit(100000, 'Nạp tiền')}>
        Nạp tiền
      </button>
      
      <div className="transactions">
        <h3>Lịch sử giao dịch</h3>
        {transactions.map(tx => (
          <div key={tx.id} className="transaction-item">
            <span>{tx.type}</span>
            <span>{tx.amount.toLocaleString('vi-VN')} VND</span>
            <span>{tx.status}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
```

### 11.2. WalletRequest Component (Customer)

```jsx
function WalletRequestPage() {
  const [requests, setRequests] = useState([]);
  const [formData, setFormData] = useState({
    type: 'deposit',
    amount: 0,
    description: ''
  });

  const createRequest = async () => {
    const response = await fetch('/api/walletrequest', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(formData)
    });
    
    if (response.ok) {
      const newRequest = await response.json();
      setRequests([...requests, newRequest]);
      alert('Yêu cầu đã được gửi. Vui lòng chờ SystemAdmin phê duyệt.');
    }
  };

  return (
    <div>
      <form onSubmit={createRequest}>
        <select value={formData.type} onChange={e => setFormData({...formData, type: e.target.value})}>
          <option value="deposit">Nạp tiền</option>
          <option value="withdraw">Rút tiền</option>
        </select>
        <input 
          type="number" 
          value={formData.amount}
          onChange={e => setFormData({...formData, amount: e.target.value})}
          placeholder="Số tiền"
        />
        <textarea
          value={formData.description}
          onChange={e => setFormData({...formData, description: e.target.value})}
          placeholder="Mô tả"
        />
        <button type="submit">Gửi yêu cầu</button>
      </form>
      
      <div className="requests-list">
        {requests.map(req => (
          <div key={req.id} className={`request-item status-${req.status}`}>
            <p>Loại: {req.type === 'deposit' ? 'Nạp tiền' : 'Rút tiền'}</p>
            <p>Số tiền: {req.amount.toLocaleString('vi-VN')} VND</p>
            <p>Trạng thái: {req.status}</p>
            {req.rejectionReason && <p>Lý do từ chối: {req.rejectionReason}</p>}
          </div>
        ))}
      </div>
    </div>
  );
}
```

### 11.3. SystemAdmin WalletRequest Management

```jsx
function SystemAdminWalletRequestPage() {
  const [requests, setRequests] = useState([]);
  const [pendingCount, setPendingCount] = useState(0);

  useEffect(() => {
    fetchPendingCount();
    fetchRequests('pending');
  }, []);

  const fetchPendingCount = async () => {
    const response = await fetch('/api/walletrequest/pending/count', {
      headers: { 'Authorization': `Bearer ${systemAdminToken}` }
    });
    const data = await response.json();
    setPendingCount(data.count);
  };

  const approveRequest = async (requestId) => {
    const response = await fetch(`/api/walletrequest/${requestId}/process`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${systemAdminToken}`
      },
      body: JSON.stringify({ action: 'approve' })
    });
    
    if (response.ok) {
      const result = await response.json();
      alert(result.message);
      fetchRequests('pending'); // Refresh list
      fetchPendingCount(); // Update badge
    }
  };

  return (
    <div>
      <div className="pending-badge">
        Yêu cầu đang chờ: {pendingCount}
      </div>
      
      <div className="requests-table">
        {requests.map(req => (
          <div key={req.id} className="request-row">
            <div className="user-info">
              <img src={req.userAvatarUrl} alt={req.userName} />
              <div>
                <p>{req.userName}</p>
                <p>{req.userEmail}</p>
                <p>{req.userRole}</p>
              </div>
            </div>
            <div className="request-details">
              <p>Loại: {req.type === 'deposit' ? 'Nạp tiền' : 'Rút tiền'}</p>
              <p>Số tiền: {req.amount.toLocaleString('vi-VN')} VND</p>
              <p>Số dư hiện tại: {req.currentBalance.toLocaleString('vi-VN')} VND</p>
              <p>Mô tả: {req.description}</p>
            </div>
            <div className="actions">
              <button onClick={() => approveRequest(req.id)}>
                Phê duyệt
              </button>
              <button onClick={() => rejectRequest(req.id)}>
                Từ chối
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

### 11.4. EnterpriseAdmin Orders với Customer Info

```jsx
function EnterpriseAdminOrdersPage() {
  const [orders, setOrders] = useState([]);

  useEffect(() => {
    fetchOrders();
  }, []);

  const fetchOrders = async () => {
    const response = await fetch('/api/orders?page=1&pageSize=20', {
      headers: { 'Authorization': `Bearer ${enterpriseAdminToken}` }
    });
    const data = await response.json();
    setOrders(data.items);
  };

  return (
    <div className="orders-list">
      {orders.map(order => (
        <div key={order.id} className="order-card">
          <div className="order-header">
            <span className="order-id">Đơn hàng #{order.id}</span>
            <span className="order-status">{order.status}</span>
          </div>
          
          {order.customer && (
            <div className="customer-section">
              <img 
                src={order.customer.avatarUrl || '/default-avatar.png'} 
                alt={order.customer.name}
                className="customer-avatar"
              />
              <div className="customer-details">
                <h4>{order.customer.name}</h4>
                <p>Email: {order.customer.email}</p>
                <p>Phone: {order.customer.phoneNumber}</p>
                <p>Địa chỉ: {order.customer.address}</p>
              </div>
            </div>
          )}
          
          <div className="shipping-section">
            <strong>Địa chỉ giao hàng:</strong>
            <p>{order.shippingAddress}</p>
          </div>
          
          <div className="order-footer">
            <span className="total">
              {order.totalAmount.toLocaleString('vi-VN')} VND
            </span>
            <span className="date">
              {new Date(order.orderDate).toLocaleDateString('vi-VN')}
            </span>
          </div>
        </div>
      ))}
    </div>
  );
}
```

---

## 12. Summary

### 12.1. New Endpoints Added

**Wallet APIs**:
- `GET /api/wallet` - Xem số dư ví
- `GET /api/wallet/transactions` - Lịch sử giao dịch
- `POST /api/wallet/deposit` - Nạp tiền (VietQR)
- `POST /api/wallet/pay` - Thanh toán đơn hàng
- `POST /api/wallet/refund` - Hoàn tiền
- `POST /api/wallet/withdraw` - Rút tiền

**WalletRequest APIs**:
- `POST /api/walletrequest` - Tạo yêu cầu nạp/rút tiền
- `GET /api/walletrequest` - Xem danh sách yêu cầu
- `GET /api/walletrequest/{id}` - Xem chi tiết yêu cầu
- `GET /api/walletrequest/pending/count` - Số lượng đang chờ (SystemAdmin)
- `POST /api/walletrequest/{id}/process` - Phê duyệt/từ chối (SystemAdmin)

**SystemAdmin Wallet Management**:
- `GET /api/wallet/system/summary` - Tổng hợp số tiền hệ thống
- `GET /api/wallet/system/users` - Danh sách ví của tất cả User
- `GET /api/wallet/user/{userId}` - Xem ví của user cụ thể
- `PUT /api/wallet/user/{userId}/balance` - Cập nhật số dư ví
- `POST /api/wallet/system/ensure-all-wallets` - Đảm bảo tất cả user có ví

**Updated Endpoints**:
- `GET /api/orders` - Thêm Customer info cho EnterpriseAdmin
- `GET /api/orders/{id}` - Thêm Customer info cho EnterpriseAdmin

### 12.2. Database Changes

**New Tables**:
- `Wallets` - Lưu thông tin ví của user
- `WalletTransactions` - Lưu lịch sử giao dịch ví
- `WalletRequests` - Lưu yêu cầu nạp/rút tiền

**Migrations**:
- `AddWalletTables` - Tạo bảng Wallet và WalletTransaction
- `AddWalletRequestTable` - Tạo bảng WalletRequest
- `EnsureAllUsersHaveWallets` - Tự động tạo ví cho user cũ

### 12.3. Configuration Changes

**appsettings.json**:
- `BankTransfer` section với thông tin MB Bank
- Đã xóa `PaymentGateways` và `PayOS` sections

---

**Version**: 1.0  
**Last Updated**: 2024-12-08  
**Status**: ✅ Ready for Frontend Integration

