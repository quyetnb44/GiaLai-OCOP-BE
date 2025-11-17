# 📘 Hướng Dẫn Tích Hợp Frontend - GiaLai OCOP Backend API

Tài liệu chi tiết về tất cả endpoints, logic nghiệp vụ và cách tích hợp với frontend.

---

## 📋 Mục Lục

1. [Tổng Quan](#tổng-quan)
2. [Authentication Flow](#authentication-flow)
3. [API Endpoints Chi Tiết](#api-endpoints-chi-tiết)
4. [Luồng Nghiệp Vụ Chính](#luồng-nghiệp-vụ-chính)
5. [Error Handling](#error-handling)
6. [Ví Dụ Code Frontend](#ví-dụ-code-frontend)

---

## 🎯 Tổng Quan

### Base URL
```
Development: https://localhost:5001
Production: https://api.gialai-ocop.vn
```

### Content-Type
Tất cả requests đều sử dụng: `application/json`

### Response Format
```typescript
// Success Response
{
  data: any,           // Dữ liệu trả về
  message?: string      // Thông báo (nếu có)
}

// Error Response
{
  error: string,       // Loại lỗi
  message: string      // Thông báo lỗi chi tiết
}
```

### Authentication
Sử dụng JWT Bearer Token:
```http
Authorization: Bearer {token}
```

---

## 🔐 Authentication Flow

### 1. Đăng Ký (Register)

**Endpoint:** `POST /api/auth/register`

**Request:**
```json
{
  "name": "Nguyễn Văn A",
  "email": "user@example.com",
  "password": "password123"
}
```

**Response (201 Created):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": "2024-11-13T10:30:00Z",
  "message": "Đăng ký thành công."
}
```

**Logic:**
- Email được normalize (lowercase, trim)
- Password được hash bằng BCrypt
- Role mặc định: `Customer`
- `IsEmailVerified = false` (không bắt buộc)
- Tự động tạo JWT token và trả về ngay

**Frontend Flow:**
```typescript
// 1. User nhập thông tin đăng ký
const registerData = {
  name: "Nguyễn Văn A",
  email: "user@example.com",
  password: "password123"
};

// 2. Gọi API
const response = await fetch('/api/auth/register', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(registerData)
});

// 3. Lưu token vào localStorage/sessionStorage
const data = await response.json();
localStorage.setItem('token', data.token);
localStorage.setItem('tokenExpires', data.expires);

// 4. Redirect đến trang chủ hoặc dashboard
```

---

### 2. Đăng Nhập (Login)

**Endpoint:** `POST /api/auth/login`

**Request:**
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": "2024-11-13T10:30:00Z"
}
```

**Error Responses:**
- `401 Unauthorized`: "Email hoặc mật khẩu không đúng."

**Logic:**
- Email được normalize
- Password được verify bằng BCrypt
- Không kiểm tra email verification (cho phép đăng nhập dù chưa verify)
- Tạo JWT token với claims: `Sub` (email), `NameIdentifier` (userId), `Name`, `Role`

**Frontend Flow:**
```typescript
const loginData = {
  email: "user@example.com",
  password: "password123"
};

const response = await fetch('/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(loginData)
});

if (response.ok) {
  const data = await response.json();
  localStorage.setItem('token', data.token);
  // Decode token để lấy role
  const userRole = decodeToken(data.token).role;
  // Redirect theo role
  if (userRole === 'Customer') {
    router.push('/customer/dashboard');
  } else if (userRole === 'EnterpriseAdmin') {
    router.push('/enterprise/dashboard');
  } else if (userRole === 'SystemAdmin') {
    router.push('/admin/dashboard');
  }
} else {
  const error = await response.json();
  showError(error.message);
}
```

---

### 3. Đăng Ký Với OTP (Optional)

**Luồng:**
1. `POST /api/auth/send-otp` - Gửi OTP đến email
2. `POST /api/auth/register-with-otp` - Đăng ký với OTP đã xác thực

**Endpoint 1: Gửi OTP**
```http
POST /api/auth/send-otp
Content-Type: application/json

{
  "email": "user@example.com",
  "purpose": "Register"  // "Register" | "Login"
}
```

**Response:**
```json
{
  "message": "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư."
}
```

**Rate Limiting:** Không cho gửi quá nhiều OTP trong 1 phút

**Endpoint 2: Đăng Ký Với OTP**
```http
POST /api/auth/register-with-otp
Content-Type: application/json

{
  "name": "Nguyễn Văn A",
  "email": "user@example.com",
  "password": "password123",
  "otpCode": "123456"
}
```

**Response:**
```json
{
  "id": 1,
  "name": "Nguyễn Văn A",
  "email": "user@example.com",
  "role": "Customer",
  "isEmailVerified": true,
  "message": "Đăng ký thành công. Email đã được xác thực."
}
```

**Lưu ý:** Endpoint này không trả về token, cần đăng nhập sau khi đăng ký.

---

### 4. Đăng Nhập Với OTP (Không Cần Mật Khẩu)

**Luồng:**
1. `POST /api/auth/send-otp` với `purpose: "Login"`
2. `POST /api/auth/login-with-otp`

**Endpoint:**
```http
POST /api/auth/login-with-otp
Content-Type: application/json

{
  "email": "user@example.com",
  "otpCode": "123456"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": "2024-11-13T10:30:00Z",
  "message": "Đăng nhập thành công bằng OTP."
}
```

---

### 5. Lấy Thông Tin User Hiện Tại

**Endpoint:** `GET /api/users/me`

**Headers:**
```http
Authorization: Bearer {token}
```

**Response:**
```json
{
  "id": 1,
  "name": "Nguyễn Văn A",
  "email": "user@example.com",
  "role": "Customer",
  "enterpriseId": null,
  "isEmailVerified": false,
  "enterprise": null
}
```

**Frontend Flow:**
```typescript
// Tạo axios instance với interceptor
const api = axios.create({
  baseURL: 'https://localhost:5001/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

// Interceptor để tự động thêm token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Lấy thông tin user
const getCurrentUser = async () => {
  try {
    const response = await api.get('/users/me');
    return response.data;
  } catch (error) {
    // Token hết hạn hoặc không hợp lệ
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      router.push('/login');
    }
    throw error;
  }
};
```

---

## 📦 API Endpoints Chi Tiết

### Module: Products (Sản Phẩm)

#### 1. GET /api/products - Danh Sách Sản Phẩm

**Auth:** Public (không cần đăng nhập)

**Query Parameters:** Không có

**Response Logic:**
- **Public/Customer:** Chỉ trả về sản phẩm có `Status = "Approved"`
- **EnterpriseAdmin:** Chỉ trả về sản phẩm của enterprise mình (mọi trạng thái)
- **SystemAdmin:** Trả về tất cả sản phẩm (mọi trạng thái)

**Response:**
```json
[
  {
    "id": 1,
    "name": "Cà phê Gia Lai",
    "description": "Cà phê đặc sản Gia Lai",
    "price": 150000,
    "enterpriseId": 1,
    "imageUrl": "https://...",
    "ocopRating": 5,
    "stockStatus": "InStock",
    "averageRating": 4.5,
    "status": "Approved",
    "categoryId": 1,
    "categoryName": "Đồ uống",
    "approvedAt": "2024-11-12T10:00:00Z",
    "approvedByUserId": 1
  }
]
```

**Frontend Code:**
```typescript
// Lấy danh sách sản phẩm (public)
const getProducts = async () => {
  const response = await fetch('/api/products');
  const products = await response.json();
  return products;
};

// Lấy danh sách sản phẩm với token (để phân quyền)
const getProductsWithAuth = async (token: string) => {
  const response = await fetch('/api/products', {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  return await response.json();
};
```

---

#### 2. GET /api/products/{id} - Chi Tiết Sản Phẩm

**Auth:** Public

**Response Logic:**
- Nếu `Status != "Approved"`:
  - **SystemAdmin:** Có thể xem
  - **EnterpriseAdmin:** Chỉ xem được sản phẩm của enterprise mình
  - **Public/Customer:** Trả về `404 Not Found`

**Response:**
```json
{
  "id": 1,
  "name": "Cà phê Gia Lai",
  "description": "Cà phê đặc sản Gia Lai",
  "price": 150000,
  "enterpriseId": 1,
  "imageUrl": "https://...",
  "ocopRating": 5,
  "stockStatus": "InStock",
  "averageRating": 4.5,
  "status": "Approved",
  "categoryId": 1,
  "categoryName": "Đồ uống"
}
```

---

#### 3. POST /api/products - Tạo Sản Phẩm

**Auth:** `EnterpriseAdmin`

**Request:**
```json
{
  "name": "Cà phê Gia Lai",
  "description": "Cà phê đặc sản Gia Lai",
  "price": 150000,
  "imageUrl": "https://...",
  "ocopRating": 5,
  "stockStatus": "InStock",
  "categoryId": 1
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "name": "Cà phê Gia Lai",
  "status": "PendingApproval",
  ...
}
```

**Logic:**
- Tự động gán `EnterpriseId` từ user hiện tại
- `Status` tự động set = `"PendingApproval"`
- Cần SystemAdmin duyệt mới được hiển thị công khai

**Frontend Code:**
```typescript
const createProduct = async (productData: CreateProductDto) => {
  const token = localStorage.getItem('token');
  const response = await fetch('/api/products', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(productData)
  });
  
  if (response.status === 201) {
    const product = await response.json();
    showSuccess('Sản phẩm đã được tạo. Đang chờ duyệt.');
    return product;
  } else {
    const error = await response.json();
    showError(error.message);
    throw error;
  }
};
```

---

#### 4. PUT /api/products/{id} - Cập Nhật Sản Phẩm

**Auth:** `EnterpriseAdmin`

**Logic:**
- Chỉ có thể cập nhật sản phẩm của enterprise mình
- Khi cập nhật, `Status` tự động reset về `"PendingApproval"`
- Cần SystemAdmin duyệt lại

**Request:**
```json
{
  "name": "Cà phê Gia Lai Premium",
  "description": "Cà phê đặc sản Gia Lai cao cấp",
  "price": 200000,
  "imageUrl": "https://...",
  "ocopRating": 5,
  "stockStatus": "InStock",
  "categoryId": 1
}
```

**Response:** `204 No Content`

---

#### 5. DELETE /api/products/{id} - Xóa Sản Phẩm

**Auth:** `EnterpriseAdmin`

**Logic:**
- Chỉ có thể xóa sản phẩm của enterprise mình
- Không thể xóa nếu sản phẩm đã có trong đơn hàng

**Response:** `204 No Content` hoặc `400 Bad Request` nếu sản phẩm đã có trong đơn hàng

---

#### 6. POST /api/products/{id}/status - Duyệt/Từ Chối Sản Phẩm

**Auth:** `SystemAdmin`

**Request:**
```json
{
  "status": "Approved",  // "Approved" | "Rejected" | "PendingApproval"
  "ocopRating": 5        // Optional, chỉ khi status = "Approved"
}
```

**Response:** `204 No Content`

**Frontend Code (SystemAdmin):**
```typescript
const approveProduct = async (productId: number, ocopRating?: number) => {
  const token = localStorage.getItem('token');
  const response = await fetch(`/api/products/${productId}/status`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      status: 'Approved',
      ocopRating: ocopRating
    })
  });
  
  if (response.status === 204) {
    showSuccess('Sản phẩm đã được duyệt.');
  }
};
```

---

### Module: Orders (Đơn Hàng)

#### 1. GET /api/orders - Danh Sách Đơn Hàng

**Auth:** `Customer` | `EnterpriseAdmin` | `SystemAdmin`

**Response Logic:**
- **Customer:** Chỉ thấy đơn hàng của mình
- **EnterpriseAdmin:** Chỉ thấy đơn hàng có sản phẩm của enterprise mình
- **SystemAdmin:** Thấy tất cả đơn hàng

**Response:**
```json
[
  {
    "id": 1,
    "userId": 1,
    "orderDate": "2024-11-13T10:00:00Z",
    "shippingAddress": "123 Đường ABC, Phường XYZ, Quận 1, TP.HCM",
    "totalAmount": 300000,
    "status": "Pending",  // "Pending" | "Processing" | "Shipped" | "Completed" | "Cancelled"
    "paymentMethod": "COD",  // "COD" | "BankTransfer"
    "paymentStatus": "Pending",  // "Pending" | "AwaitingTransfer" | "Paid" | "PartiallyPaid" | "Cancelled"
    "orderItems": [
      {
        "id": 1,
        "orderId": 1,
        "productId": 1,
        "quantity": 2,
        "price": 150000
      }
    ],
    "payments": [
      {
        "id": 1,
        "orderId": 1,
        "enterpriseId": 1,
        "enterpriseName": "Doanh nghiệp ABC",
        "amount": 300000,
        "method": "COD",
        "status": "Pending",
        "qrCodeUrl": null
      }
    ]
  }
]
```

---

#### 2. GET /api/orders/{id} - Chi Tiết Đơn Hàng

**Auth:** `Customer` | `EnterpriseAdmin` | `SystemAdmin`

**Logic:**
- **Customer:** Chỉ xem được đơn hàng của mình
- **EnterpriseAdmin:** Chỉ xem được đơn hàng có sản phẩm của enterprise mình
- **SystemAdmin:** Xem được tất cả

**Response:** Tương tự GET /api/orders nhưng chỉ 1 object

---

#### 3. POST /api/orders - Tạo Đơn Hàng

**Auth:** `Customer`

**Request:**
```json
{
  "shippingAddress": "123 Đường ABC, Phường XYZ, Quận 1, TP.HCM",
  "shippingAddressId": 1,  // Optional: ID của địa chỉ đã lưu
  "paymentMethod": "COD",  // "COD" | "BankTransfer"
  "items": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 2,
      "quantity": 1
    }
  ]
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "userId": 1,
  "orderDate": "2024-11-13T10:00:00Z",
  "shippingAddress": "123 Đường ABC...",
  "totalAmount": 300000,
  "status": "Pending",
  "paymentMethod": "COD",
  "paymentStatus": "Pending",
  "orderItems": [...],
  "payments": [...]
}
```

**Validation Logic:**
- Sản phẩm phải có `Status = "Approved"`
- Sản phẩm phải có `StockStatus != "OutOfStock"`
- `Quantity > 0`
- Tự động tính `TotalAmount` từ giá sản phẩm tại thời điểm đặt hàng
- Sử dụng transaction để đảm bảo tính nhất quán

**Frontend Code:**
```typescript
const createOrder = async (orderData: CreateOrderDto) => {
  const token = localStorage.getItem('token');
  const response = await fetch('/api/orders', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(orderData)
  });
  
  if (response.status === 201) {
    const order = await response.json();
    showSuccess('Đơn hàng đã được tạo thành công.');
    // Redirect đến trang thanh toán hoặc chi tiết đơn hàng
    router.push(`/orders/${order.id}`);
    return order;
  } else {
    const error = await response.json();
    showError(error.message);
    throw error;
  }
};
```

---

#### 4. PUT /api/orders/{id}/status - Cập Nhật Trạng Thái Đơn Hàng

**Auth:** `Customer` | `EnterpriseAdmin` | `SystemAdmin`

**Request:**
```json
{
  "status": "Processing"  // "Pending" | "Processing" | "Shipped" | "Completed" | "Cancelled"
}
```

**Logic Phân Quyền:**
- **Customer:**
  - Chỉ có thể set `status = "Cancelled"`
  - Chỉ có thể hủy khi đơn hàng còn ở trạng thái `"Pending"`
- **EnterpriseAdmin:**
  - Có thể set: `"Processing"`, `"Shipped"`, `"Completed"`
  - **KHÔNG THỂ** set `"Cancelled"` (chỉ Customer mới được hủy)
  - Chỉ có thể cập nhật đơn hàng có sản phẩm của enterprise mình
- **SystemAdmin:**
  - Có thể set bất kỳ status nào

**Response:** `204 No Content`

**Frontend Code (EnterpriseAdmin):**
```typescript
const updateOrderStatus = async (orderId: number, status: string) => {
  const token = localStorage.getItem('token');
  const response = await fetch(`/api/orders/${orderId}/status`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ status })
  });
  
  if (response.status === 204) {
    showSuccess('Trạng thái đơn hàng đã được cập nhật.');
    // Refresh danh sách đơn hàng
    await refreshOrders();
  } else {
    const error = await response.json();
    showError(error.message);
  }
};
```

---

#### 5. DELETE /api/orders/{id} - Xóa Đơn Hàng

**Auth:** `Customer` | `EnterpriseAdmin` | `SystemAdmin`

**Logic:**
- **Customer:** Chỉ xóa được đơn ở trạng thái `"Pending"` hoặc `"Cancelled"`
- **EnterpriseAdmin:** Chỉ xóa được đơn ở trạng thái `"Pending"` hoặc `"Cancelled"` và có sản phẩm của enterprise mình
- **SystemAdmin:** Xóa được bất kỳ đơn hàng nào

**Response:** `204 No Content`

---

### Module: Payments (Thanh Toán)

#### 1. POST /api/payments - Tạo Thanh Toán

**Auth:** `Customer`

**Request:**
```json
{
  "orderId": 1,
  "method": "BankTransfer"  // "COD" | "BankTransfer"
}
```

**Response (201 Created):**
```json
[
  {
    "id": 1,
    "orderId": 1,
    "enterpriseId": 1,
    "enterpriseName": "Doanh nghiệp ABC",
    "amount": 150000,
    "method": "BankTransfer",
    "status": "AwaitingTransfer",
    "bankCode": "970415",
    "bankAccount": "123456789",
    "accountName": "DOANH NGHIEP ABC",
    "qrCodeUrl": "https://img.vietqr.io/image/...",
    "createdAt": "2024-11-13T10:00:00Z"
  },
  {
    "id": 2,
    "orderId": 1,
    "enterpriseId": 2,
    "enterpriseName": "Doanh nghiệp XYZ",
    "amount": 150000,
    "method": "BankTransfer",
    "status": "AwaitingTransfer",
    "qrCodeUrl": "https://img.vietqr.io/image/...",
    "createdAt": "2024-11-13T10:00:00Z"
  }
]
```

**Logic:**
- Tự động tạo payment riêng cho **mỗi enterprise** trong đơn hàng
- Nếu `method = "BankTransfer"`:
  - Tự động tạo QR code cho mỗi payment
  - `Status = "AwaitingTransfer"`
  - Cần EnterpriseAdmin/SystemAdmin xác nhận sau khi khách chuyển khoản
- Nếu `method = "COD"`:
  - `Status = "Pending"`
  - Thanh toán khi nhận hàng
- Tự động hủy các payment pending/awaiting cũ của order này

**Frontend Code:**
```typescript
const createPayment = async (orderId: number, method: 'COD' | 'BankTransfer') => {
  const token = localStorage.getItem('token');
  const response = await fetch('/api/payments', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ orderId, method })
  });
  
  if (response.status === 201) {
    const payments = await response.json();
    
    if (method === 'BankTransfer') {
      // Hiển thị QR code cho từng payment
      payments.forEach((payment: PaymentDto) => {
        if (payment.qrCodeUrl) {
          showQRCode(payment.enterpriseName, payment.qrCodeUrl, payment.amount);
        }
      });
      showSuccess('Vui lòng quét QR code để thanh toán cho từng doanh nghiệp.');
    } else {
      showSuccess('Đơn hàng COD đã được tạo. Bạn sẽ thanh toán khi nhận hàng.');
    }
    
    return payments;
  } else {
    const error = await response.json();
    showError(error.message);
    throw error;
  }
};
```

---

#### 2. GET /api/payments/order/{orderId} - Danh Sách Payments Của Đơn Hàng

**Auth:** `Customer` | `EnterpriseAdmin` | `SystemAdmin`

**Response:**
```json
[
  {
    "id": 1,
    "orderId": 1,
    "enterpriseId": 1,
    "enterpriseName": "Doanh nghiệp ABC",
    "amount": 150000,
    "method": "BankTransfer",
    "status": "Paid",
    "qrCodeUrl": "https://...",
    "paidAt": "2024-11-13T11:00:00Z"
  }
]
```

**Logic:**
- **Customer:** Chỉ xem được payments của đơn hàng của mình
- **EnterpriseAdmin:** Chỉ xem được payments của enterprise mình
- **SystemAdmin:** Xem được tất cả

---

#### 3. POST /api/payments/{id}/status - Xác Nhận Thanh Toán

**Auth:** `EnterpriseAdmin` | `SystemAdmin`

**Request:**
```json
{
  "status": "Paid",  // "Paid" | "Cancelled"
  "notes": "Đã nhận được thanh toán"  // Optional
}
```

**Logic:**
- **EnterpriseAdmin:** Chỉ xác nhận được payment của enterprise mình
- **SystemAdmin:** Xác nhận được tất cả payments
- Khi tất cả payments của order đã `"Paid"`:
  - `Order.PaymentStatus` tự động set = `"Paid"`
- Nếu chỉ một phần payments đã `"Paid"`:
  - `Order.PaymentStatus` = `"PartiallyPaid"`

**Response:** `204 No Content`

**Frontend Code (EnterpriseAdmin):**
```typescript
const confirmPayment = async (paymentId: number, notes?: string) => {
  const token = localStorage.getItem('token');
  const response = await fetch(`/api/payments/${paymentId}/status`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      status: 'Paid',
      notes: notes
    })
  });
  
  if (response.status === 204) {
    showSuccess('Thanh toán đã được xác nhận.');
    await refreshPayments();
  } else {
    const error = await response.json();
    showError(error.message);
  }
};
```

---

### Module: Map (Bản Đồ)

#### 1. GET /api/map/search - Tìm Kiếm Doanh Nghiệp

**Auth:** Public

**Query Parameters:**
```
keyword?: string          // Từ khóa tìm kiếm
userLat?: number          // Vĩ độ của user (để tính khoảng cách)
userLng?: number          // Kinh độ của user
page?: number = 1         // Số trang
pageSize?: number = 20    // Số lượng mỗi trang (max: 100)
sortBy?: string = "name"  // "name" | "distance" | "rating" | "ocopRating"
sortOrder?: string = "asc" // "asc" | "desc"
```

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "name": "Doanh nghiệp ABC",
      "address": "123 Đường ABC",
      "ward": "Phường XYZ",
      "district": "Quận 1",
      "province": "TP.HCM",
      "latitude": 10.762622,
      "longitude": 106.660172,
      "distance": 2.5,  // km (nếu có userLat, userLng)
      "ocopRating": 5,
      "averageRating": 4.5,
      "productCount": 10,
      "products": [
        {
          "id": 1,
          "name": "Cà phê Gia Lai",
          "price": 150000,
          "imageUrl": "https://...",
          "averageRating": 4.5
        }
      ]
    }
  ],
  "total": 50,
  "page": 1,
  "pageSize": 20
}
```

**Frontend Code:**
```typescript
const searchEnterprises = async (keyword: string, userLocation?: { lat: number, lng: number }) => {
  const params = new URLSearchParams({
    keyword: keyword,
    page: '1',
    pageSize: '20',
    sortBy: 'distance',
    sortOrder: 'asc'
  });
  
  if (userLocation) {
    params.append('userLat', userLocation.lat.toString());
    params.append('userLng', userLocation.lng.toString());
  }
  
  const response = await fetch(`/api/map/search?${params}`);
  const result = await response.json();
  return result;
};
```

---

#### 2. GET /api/map/bounding-box - Tìm Theo Khu Vực Bản Đồ

**Auth:** Public

**Query Parameters:**
```
minLatitude: number       // Required
maxLatitude: number       // Required
minLongitude: number      // Required
maxLongitude: number      // Required
userLat?: number
userLng?: number
page?: number = 1
pageSize?: number = 20
sortBy?: string = "name"
sortOrder?: string = "asc"
```

**Response:** Tương tự `/api/map/search`

**Frontend Code (Google Maps):**
```typescript
// Khi user kéo/thay đổi viewport của map
const onMapBoundsChanged = async (bounds: google.maps.LatLngBounds) => {
  const params = new URLSearchParams({
    minLatitude: bounds.getSouth().toString(),
    maxLatitude: bounds.getNorth().toString(),
    minLongitude: bounds.getWest().toString(),
    maxLongitude: bounds.getEast().toString(),
    page: '1',
    pageSize: '50'
  });
  
  const response = await fetch(`/api/map/bounding-box?${params}`);
  const result = await response.json();
  
  // Hiển thị markers trên map
  displayMarkersOnMap(result.data);
};
```

---

#### 3. GET /api/map/nearby - Tìm Theo Tọa Độ Và Bán Kính

**Auth:** Public

**Query Parameters:**
```
latitude: number          // Required
longitude: number         // Required
radiusKm?: number = 10    // Bán kính (km)
page?: number = 1
pageSize?: number = 20
sortBy?: string = "distance"
sortOrder?: string = "asc"
```

**Response:** Tương tự `/api/map/search`

---

#### 4. GET /api/map/enterprises/{id} - Chi Tiết Doanh Nghiệp

**Auth:** Public

**Response:**
```json
{
  "id": 1,
  "name": "Doanh nghiệp ABC",
  "description": "Mô tả doanh nghiệp",
  "address": "123 Đường ABC",
  "ward": "Phường XYZ",
  "district": "Quận 1",
  "province": "TP.HCM",
  "latitude": 10.762622,
  "longitude": 106.660172,
  "phoneNumber": "0123456789",
  "emailContact": "contact@example.com",
  "website": "https://...",
  "ocopRating": 5,
  "averageRating": 4.5,
  "businessField": "Thực phẩm",
  "imageUrl": "https://...",
  "products": [
    {
      "id": 1,
      "name": "Cà phê Gia Lai",
      "price": 150000,
      "imageUrl": "https://...",
      "averageRating": 4.5,
      "status": "Approved"
    }
  ],
  "distance": 2.5  // Nếu có userLat, userLng trong query
}
```

---

### Module: Enterprise Applications (Đăng Ký Doanh Nghiệp)

#### 1. POST /api/enterpriseapplications - Gửi Đơn Đăng Ký OCOP

**Auth:** `Customer`

**Request:**
```json
{
  "enterpriseName": "Doanh nghiệp ABC",
  "businessType": "Công ty TNHH",
  "taxCode": "123456789",
  "businessLicenseNumber": "123456",
  "licenseIssuedDate": "2024-01-01",
  "licenseIssuedBy": "Sở Kế hoạch và Đầu tư",
  "address": "123 Đường ABC",
  "ward": "Phường XYZ",
  "district": "Quận 1",
  "province": "TP.HCM",
  "phoneNumber": "0123456789",
  "emailContact": "contact@example.com",
  "website": "https://...",
  "representativeName": "Nguyễn Văn A",
  "representativePosition": "Giám đốc",
  "representativeIdNumber": "123456789",
  "representativeIdIssuedDate": "2020-01-01",
  "representativeIdIssuedBy": "CA TP.HCM",
  "productionLocation": "123 Đường ABC",
  "numberOfEmployees": 50,
  "productionScale": "Vừa",
  "businessField": "Thực phẩm",
  "productName": "Cà phê Gia Lai",
  "productCategory": "Đồ uống",
  "productDescription": "Cà phê đặc sản",
  "productOrigin": "Gia Lai",
  "productCertifications": "ISO 9001",
  "productImages": "https://...",
  "attachedDocuments": "https://...",
  "additionalNotes": "Ghi chú thêm"
}
```

**Response:**
```json
{
  "message": "Đơn đăng ký OCOP đã được gửi thành công.",
  "id": 1
}
```

**Validation:**
- User phải có role = `"Customer"`
- Không được gửi đơn mới nếu đã có đơn `"Pending"`
- User đã là `EnterpriseAdmin` không được gửi đơn

---

#### 2. GET /api/enterpriseapplications - Xem Tất Cả Đơn (SystemAdmin)

**Auth:** `SystemAdmin`

**Response:**
```json
[
  {
    "id": 1,
    "userId": 1,
    "enterpriseName": "Doanh nghiệp ABC",
    "status": "Pending",  // "Pending" | "Approved" | "Rejected"
    "createdAt": "2024-11-13T10:00:00Z",
    "updatedAt": null,
    ...
  }
]
```

---

#### 3. PUT /api/enterpriseapplications/{id}/approve - Phê Duyệt Đơn

**Auth:** `SystemAdmin`

**Response:**
```json
{
  "message": "Đã phê duyệt và tạo hồ sơ doanh nghiệp OCOP thành công."
}
```

**Logic:**
- Tự động tạo `Enterprise` mới
- Gán `User.Role = "EnterpriseAdmin"`
- Gán `User.EnterpriseId = enterprise.Id`
- Set `Status = "Approved"`

---

#### 4. PUT /api/enterpriseapplications/{id}/reject - Từ Chối Đơn

**Auth:** `SystemAdmin`

**Request:**
```json
"Không đạt yêu cầu về giấy phép kinh doanh."
```

**Response:**
```json
{
  "message": "Đã từ chối đơn đăng ký OCOP."
}
```

---

### Module: Enterprises (Doanh Nghiệp)

#### 1. GET /api/enterprises/me - Xem Doanh Nghiệp Của Mình

**Auth:** `EnterpriseAdmin`

**Response:**
```json
{
  "id": 1,
  "name": "Doanh nghiệp ABC",
  "description": "Mô tả",
  "address": "123 Đường ABC",
  "ward": "Phường XYZ",
  "district": "Quận 1",
  "province": "TP.HCM",
  "latitude": 10.762622,
  "longitude": 106.660172,
  "phoneNumber": "0123456789",
  "emailContact": "contact@example.com",
  "website": "https://...",
  "ocopRating": 5,
  "businessField": "Thực phẩm",
  "imageUrl": "https://...",
  "averageRating": 4.5,
  "products": [...],
  "users": [...]
}
```

---

#### 2. PUT /api/enterprises/me - Cập Nhật Doanh Nghiệp Của Mình

**Auth:** `EnterpriseAdmin`

**Request:**
```json
{
  "name": "Doanh nghiệp ABC",
  "description": "Mô tả mới",
  "address": "123 Đường ABC",
  "ward": "Phường XYZ",
  "district": "Quận 1",
  "province": "TP.HCM",
  "latitude": 10.762622,
  "longitude": 106.660172,
  "phoneNumber": "0123456789",
  "emailContact": "contact@example.com",
  "website": "https://...",
  "businessField": "Thực phẩm",
  "imageUrl": "https://..."
}
```

**Lưu ý:** Không được cập nhật `ocopRating` (chỉ SystemAdmin mới được)

**Response:** `204 No Content`

---

### Module: Categories (Danh Mục)

#### 1. GET /api/categories - Danh Sách Danh Mục

**Auth:** Public

**Query Parameters:**
```
isActive?: boolean  // true | false | null (tất cả)
```

**Response:**
```json
[
  {
    "id": 1,
    "name": "Đồ uống",
    "description": "Các sản phẩm đồ uống",
    "isActive": true
  }
]
```

---

#### 2. POST /api/categories - Tạo Danh Mục

**Auth:** `SystemAdmin`

**Request:**
```json
{
  "name": "Đồ uống",
  "description": "Các sản phẩm đồ uống",
  "isActive": true
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "name": "Đồ uống",
  "description": "Các sản phẩm đồ uống",
  "isActive": true
}
```

---

### Module: Shipping Addresses (Địa Chỉ Giao Hàng)

#### 1. GET /api/shipping-addresses - Danh Sách Địa Chỉ

**Auth:** Authenticated (tất cả roles)

**Response:**
```json
[
  {
    "id": 1,
    "userId": 1,
    "fullName": "Nguyễn Văn A",
    "phoneNumber": "0123456789",
    "addressLine": "123 Đường ABC",
    "ward": "Phường XYZ",
    "district": "Quận 1",
    "province": "TP.HCM",
    "label": "Nhà riêng",
    "isDefault": true,
    "createdAt": "2024-11-13T10:00:00Z",
    "updatedAt": null
  }
]
```

**Logic:**
- Chỉ trả về địa chỉ của user hiện tại
- Sắp xếp: `isDefault = true` trước, sau đó theo `createdAt` mới nhất

---

#### 2. POST /api/shipping-addresses - Tạo Địa Chỉ Mới

**Auth:** Authenticated

**Request:**
```json
{
  "fullName": "Nguyễn Văn A",
  "phoneNumber": "0123456789",
  "addressLine": "123 Đường ABC",
  "ward": "Phường XYZ",
  "district": "Quận 1",
  "province": "TP.HCM",
  "label": "Nhà riêng",  // Optional
  "isDefault": true       // Optional
}
```

**Logic:**
- Nếu `isDefault = true`: Tự động bỏ mặc định của các địa chỉ khác
- Nếu chưa có địa chỉ nào: Tự động set `isDefault = true`

**Response (201 Created):**
```json
{
  "id": 1,
  "userId": 1,
  "fullName": "Nguyễn Văn A",
  ...
}
```

---

#### 3. PUT /api/shipping-addresses/{id} - Cập Nhật Địa Chỉ

**Auth:** Authenticated

**Logic:**
- Không thể cập nhật địa chỉ đang được sử dụng trong đơn hàng
- Nếu set `isDefault = true`: Tự động bỏ mặc định của các địa chỉ khác

---

#### 4. DELETE /api/shipping-addresses/{id} - Xóa Địa Chỉ

**Auth:** Authenticated

**Logic:**
- Không thể xóa địa chỉ đang được sử dụng trong đơn hàng
- Nếu xóa địa chỉ mặc định: Tự động set địa chỉ đầu tiên khác làm mặc định

---

## 🔄 Luồng Nghiệp Vụ Chính

### Luồng 1: Customer Đặt Hàng & Thanh Toán

```
1. Customer xem sản phẩm (GET /api/products)
   ↓
2. Customer thêm vào giỏ hàng (frontend state)
   ↓
3. Customer chọn địa chỉ giao hàng
   - Lấy danh sách địa chỉ đã lưu (GET /api/shipping-addresses)
   - Hoặc nhập địa chỉ mới
   ↓
4. Customer tạo đơn hàng (POST /api/orders)
   - Validation: Sản phẩm phải Approved, còn hàng
   ↓
5. Customer tạo thanh toán (POST /api/payments)
   - Chọn phương thức: COD hoặc BankTransfer
   - Nếu BankTransfer: Hiển thị QR code cho từng enterprise
   ↓
6. Customer chuyển khoản (nếu BankTransfer)
   ↓
7. EnterpriseAdmin xác nhận thanh toán (POST /api/payments/{id}/status)
   ↓
8. EnterpriseAdmin cập nhật trạng thái đơn hàng
   - Processing → Shipped → Completed
   ↓
9. Hoàn tất
```

**Frontend Code:**
```typescript
// Luồng đặt hàng hoàn chỉnh
const placeOrder = async (cartItems: CartItem[], shippingAddressId: number, paymentMethod: 'COD' | 'BankTransfer') => {
  try {
    // 1. Tạo đơn hàng
    const order = await createOrder({
      shippingAddressId: shippingAddressId,
      paymentMethod: paymentMethod,
      items: cartItems.map(item => ({
        productId: item.productId,
        quantity: item.quantity
      }))
    });
    
    // 2. Tạo thanh toán
    if (paymentMethod === 'BankTransfer') {
      const payments = await createPayment(order.id, 'BankTransfer');
      
      // Hiển thị modal với QR code cho từng payment
      showPaymentModal(payments);
    } else {
      showSuccess('Đơn hàng COD đã được tạo. Bạn sẽ thanh toán khi nhận hàng.');
    }
    
    // 3. Clear cart
    clearCart();
    
    // 4. Redirect
    router.push(`/orders/${order.id}`);
    
  } catch (error) {
    showError('Có lỗi xảy ra khi đặt hàng. Vui lòng thử lại.');
  }
};
```

---

### Luồng 2: EnterpriseAdmin Quản Lý Sản Phẩm

```
1. EnterpriseAdmin xem sản phẩm của mình (GET /api/products)
   - Chỉ thấy sản phẩm của enterprise mình
   - Có thể thấy tất cả trạng thái: PendingApproval, Approved, Rejected
   ↓
2. EnterpriseAdmin tạo sản phẩm mới (POST /api/products)
   - Status tự động = "PendingApproval"
   ↓
3. SystemAdmin duyệt sản phẩm (POST /api/products/{id}/status)
   - Status = "Approved" → Hiển thị công khai
   - Status = "Rejected" → Không hiển thị
   ↓
4. EnterpriseAdmin có thể cập nhật sản phẩm (PUT /api/products/{id})
   - Status tự động reset về "PendingApproval"
   - Cần SystemAdmin duyệt lại
```

---

### Luồng 3: Customer Đăng Ký Doanh Nghiệp OCOP

```
1. Customer đăng nhập (POST /api/auth/login)
   ↓
2. Customer gửi đơn đăng ký (POST /api/enterpriseapplications)
   - Status = "Pending"
   ↓
3. SystemAdmin xem đơn (GET /api/enterpriseapplications)
   ↓
4. SystemAdmin phê duyệt (PUT /api/enterpriseapplications/{id}/approve)
   - Tự động tạo Enterprise
   - Gán User.Role = "EnterpriseAdmin"
   - Gán User.EnterpriseId = enterprise.Id
   ↓
5. Customer (giờ là EnterpriseAdmin) có thể quản lý sản phẩm
```

---

## ⚠️ Error Handling

### HTTP Status Codes

- `200 OK`: Request thành công
- `201 Created`: Tạo mới thành công
- `204 No Content`: Cập nhật/xóa thành công (không có body)
- `400 Bad Request`: Dữ liệu không hợp lệ
- `401 Unauthorized`: Chưa đăng nhập hoặc token không hợp lệ
- `403 Forbid`: Không có quyền truy cập
- `404 Not Found`: Không tìm thấy resource
- `409 Conflict`: Xung đột dữ liệu (ví dụ: email đã tồn tại)
- `500 Internal Server Error`: Lỗi server

### Error Response Format

```json
{
  "error": "Bad Request",
  "message": "Địa chỉ giao hàng là bắt buộc."
}
```

### Frontend Error Handling

```typescript
// Axios interceptor để xử lý lỗi tự động
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      const { status, data } = error.response;
      
      switch (status) {
        case 401:
          // Token hết hạn hoặc không hợp lệ
          localStorage.removeItem('token');
          router.push('/login');
          showError('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
          break;
          
        case 403:
          // Không có quyền
          showError(data.message || 'Bạn không có quyền thực hiện thao tác này.');
          break;
          
        case 404:
          showError('Không tìm thấy dữ liệu.');
          break;
          
        case 400:
        case 409:
          // Lỗi validation hoặc xung đột
          showError(data.message || 'Dữ liệu không hợp lệ.');
          break;
          
        case 500:
          showError('Lỗi server. Vui lòng thử lại sau.');
          break;
          
        default:
          showError('Có lỗi xảy ra. Vui lòng thử lại.');
      }
    } else {
      // Network error
      showError('Không thể kết nối đến server. Vui lòng kiểm tra kết nối mạng.');
    }
    
    return Promise.reject(error);
  }
);
```

---

## 💻 Ví Dụ Code Frontend

### React + TypeScript + Axios

```typescript
// api/client.ts
import axios from 'axios';

const api = axios.create({
  baseURL: process.env.REACT_APP_API_URL || 'https://localhost:5001/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

// Interceptor để thêm token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Interceptor để xử lý lỗi
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default api;
```

```typescript
// api/products.ts
import api from './client';

export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  enterpriseId: number;
  imageUrl?: string;
  ocopRating?: number;
  stockStatus: string;
  averageRating?: number;
  status: string;
  categoryId?: number;
  categoryName?: string;
}

export const getProducts = async (): Promise<Product[]> => {
  const response = await api.get('/products');
  return response.data;
};

export const getProduct = async (id: number): Promise<Product> => {
  const response = await api.get(`/products/${id}`);
  return response.data;
};

export const createProduct = async (data: {
  name: string;
  description: string;
  price: number;
  imageUrl?: string;
  ocopRating?: number;
  stockStatus?: string;
  categoryId?: number;
}): Promise<Product> => {
  const response = await api.post('/products', data);
  return response.data;
};
```

```typescript
// api/orders.ts
import api from './client';

export interface Order {
  id: number;
  userId: number;
  orderDate: string;
  shippingAddress: string;
  totalAmount: number;
  status: string;
  paymentMethod: string;
  paymentStatus: string;
  orderItems: OrderItem[];
  payments: Payment[];
}

export interface CreateOrderDto {
  shippingAddress?: string;
  shippingAddressId?: number;
  paymentMethod: 'COD' | 'BankTransfer';
  items: Array<{
    productId: number;
    quantity: number;
  }>;
}

export const getOrders = async (): Promise<Order[]> => {
  const response = await api.get('/orders');
  return response.data;
};

export const createOrder = async (data: CreateOrderDto): Promise<Order> => {
  const response = await api.post('/orders', data);
  return response.data;
};

export const updateOrderStatus = async (id: number, status: string): Promise<void> => {
  await api.put(`/orders/${id}/status`, { status });
};
```

```typescript
// hooks/useAuth.ts
import { useState, useEffect } from 'react';
import api from '../api/client';

interface User {
  id: number;
  name: string;
  email: string;
  role: string;
  enterpriseId?: number;
  isEmailVerified: boolean;
}

export const useAuth = () => {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (token) {
      api.get('/users/me')
        .then((response) => {
          setUser(response.data);
        })
        .catch(() => {
          localStorage.removeItem('token');
        })
        .finally(() => {
          setLoading(false);
        });
    } else {
      setLoading(false);
    }
  }, []);

  const login = async (email: string, password: string) => {
    const response = await api.post('/auth/login', { email, password });
    localStorage.setItem('token', response.data.token);
    localStorage.setItem('tokenExpires', response.data.expires);
    
    // Lấy thông tin user
    const userResponse = await api.get('/users/me');
    setUser(userResponse.data);
    
    return response.data;
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('tokenExpires');
    setUser(null);
  };

  return { user, loading, login, logout };
};
```

---

## 📝 Lưu Ý Quan Trọng

### 1. Token Management
- Lưu token vào `localStorage` hoặc `sessionStorage`
- Kiểm tra token hết hạn trước khi gọi API
- Tự động refresh token nếu cần (hiện tại chưa có endpoint refresh)

### 2. Role-Based UI
- Ẩn/hiện các chức năng theo role của user
- Customer: Chỉ thấy chức năng đặt hàng, xem đơn hàng
- EnterpriseAdmin: Thấy quản lý sản phẩm, đơn hàng của enterprise mình
- SystemAdmin: Thấy tất cả chức năng quản trị

### 3. Real-time Updates
- Hiện tại chưa có WebSocket/SignalR
- Cần polling hoặc manual refresh để cập nhật dữ liệu
- Ví dụ: EnterpriseAdmin cần refresh để xem đơn hàng mới

### 4. File Upload
- Có `FileUploadController` nhưng cần kiểm tra implementation
- Sử dụng `multipart/form-data` cho file upload

### 5. Pagination
- Map API có pagination
- Các API khác có thể cần thêm pagination nếu dữ liệu lớn

---

**Cập nhật:** 2024-11-13  
**Version:** 1.0

