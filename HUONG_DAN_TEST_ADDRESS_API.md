# Hướng dẫn Test API Địa chỉ Giao hàng

## 1. Chạy Migration SQL

Trước tiên, bạn cần chạy migration SQL để tạo các bảng và cột mới:

```sql
-- Chạy file: Migrations/AddAddressFieldsToUsers.sql
-- Sau đó chạy: Scripts/SeedAddressData.sql để seed dữ liệu mẫu
```

Hoặc sử dụng Entity Framework Core migration:

```bash
dotnet ef migrations add AddAddressFieldsToUsers
dotnet ef database update
```

## 2. API Endpoints

### 2.1. GET /api/address/provinces
Lấy danh sách tất cả tỉnh/thành phố

**Request:**
```
GET /api/address/provinces
```

**Response:**
```json
[
  {
    "id": 1,
    "name": "Gia Lai",
    "code": "64"
  },
  {
    "id": 2,
    "name": "Bà Rịa - Vũng Tàu",
    "code": "77"
  }
]
```

### 2.2. GET /api/address/districts?provinceId=1
Lấy danh sách quận/huyện theo tỉnh/thành phố

**Request:**
```
GET /api/address/districts?provinceId=1
```

**Response:**
```json
[
  {
    "id": 1,
    "name": "Pleiku",
    "code": "6401",
    "provinceId": 1
  },
  {
    "id": 2,
    "name": "An Khê",
    "code": "6402",
    "provinceId": 1
  }
]
```

### 2.3. GET /api/address/wards?districtId=1
Lấy danh sách phường/xã theo quận/huyện

**Request:**
```
GET /api/address/wards?districtId=1
```

**Response:**
```json
[
  {
    "id": 1,
    "name": "Phường Yên Đỗ",
    "code": "640101",
    "districtId": 1
  },
  {
    "id": 2,
    "name": "Phường Yên Thế",
    "code": "640102",
    "districtId": 1
  }
]
```

### 2.4. PUT /api/users/update-shipping-address
Cập nhật địa chỉ giao hàng chi tiết (Yêu cầu authentication)

**Request Headers:**
```
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json
```

**Request Body:**
```json
{
  "provinceId": 1,
  "districtId": 1,
  "wardId": 1,
  "addressDetail": "123 Đường ABC"
}
```

**Response:**
```json
{
  "id": 1,
  "name": "Nguyễn Văn A",
  "email": "user@example.com",
  "role": "Customer",
  "shippingAddress": "123 Đường ABC, Phường Yên Đỗ, Pleiku, Gia Lai",
  "provinceId": 1,
  "districtId": 1,
  "wardId": 1,
  "addressDetail": "123 Đường ABC",
  ...
}
```

## 3. Test bằng Postman

### Bước 1: Đăng nhập để lấy JWT Token

```
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

Lưu token từ response.

### Bước 2: Test GET /api/address/provinces

1. Tạo request mới: `GET http://localhost:5001/api/address/provinces`
2. Không cần authentication
3. Send request
4. Kiểm tra response có danh sách tỉnh/thành phố

### Bước 3: Test GET /api/address/districts

1. Tạo request mới: `GET http://localhost:5001/api/address/districts?provinceId=1`
2. Không cần authentication
3. Send request
4. Kiểm tra response có danh sách quận/huyện của tỉnh đã chọn

### Bước 4: Test GET /api/address/wards

1. Tạo request mới: `GET http://localhost:5001/api/address/wards?districtId=1`
2. Không cần authentication
3. Send request
4. Kiểm tra response có danh sách phường/xã của quận/huyện đã chọn

### Bước 5: Test PUT /api/users/update-shipping-address

1. Tạo request mới: `PUT http://localhost:5001/api/users/update-shipping-address`
2. Thêm Header:
   - `Authorization: Bearer {JWT_TOKEN}` (token từ bước 1)
   - `Content-Type: application/json`
3. Body (raw JSON):
```json
{
  "provinceId": 1,
  "districtId": 1,
  "wardId": 1,
  "addressDetail": "123 Đường ABC, Phường Yên Đỗ"
}
```
4. Send request
5. Kiểm tra response có thông tin user đã được cập nhật

## 4. Test Validation

### Test với dữ liệu không hợp lệ:

**Test 1: Thiếu provinceId**
```json
{
  "districtId": 1,
  "wardId": 1,
  "addressDetail": "123 Đường ABC"
}
```
Expected: 400 Bad Request với message "ProvinceId là bắt buộc."

**Test 2: ProvinceId không tồn tại**
```json
{
  "provinceId": 9999,
  "districtId": 1,
  "wardId": 1,
  "addressDetail": "123 Đường ABC"
}
```
Expected: 400 Bad Request với message "Không tìm thấy tỉnh/thành phố với Id = 9999."

**Test 3: DistrictId không thuộc ProvinceId**
```json
{
  "provinceId": 1,
  "districtId": 9999,
  "wardId": 1,
  "addressDetail": "123 Đường ABC"
}
```
Expected: 400 Bad Request với message "Không tìm thấy quận/huyện..."

**Test 4: WardId không thuộc DistrictId**
```json
{
  "provinceId": 1,
  "districtId": 1,
  "wardId": 9999,
  "addressDetail": "123 Đường ABC"
}
```
Expected: 400 Bad Request với message "Không tìm thấy phường/xã..."

**Test 5: Thiếu addressDetail**
```json
{
  "provinceId": 1,
  "districtId": 1,
  "wardId": 1,
  "addressDetail": ""
}
```
Expected: 400 Bad Request với message "Địa chỉ cụ thể là bắt buộc."

## 5. Kiểm tra Database

Sau khi cập nhật địa chỉ, kiểm tra bảng Users:

```sql
SELECT 
  "Id", 
  "Name", 
  "Email",
  "ProvinceId",
  "DistrictId", 
  "WardId",
  "AddressDetail",
  "ShippingAddress"
FROM "Users"
WHERE "Id" = {userId};
```

Kiểm tra các bảng địa chỉ:

```sql
-- Kiểm tra Provinces
SELECT * FROM "Provinces";

-- Kiểm tra Districts
SELECT * FROM "Districts" WHERE "ProvinceId" = 1;

-- Kiểm tra Wards
SELECT * FROM "Wards" WHERE "DistrictId" = 1;
```

## 6. Lưu ý

- Tất cả các API address (provinces, districts, wards) đều public, không cần authentication
- API update-shipping-address yêu cầu authentication (JWT token)
- Backend tự động tạo địa chỉ đầy đủ trong trường `ShippingAddress` từ các thông tin chi tiết
- Cần seed dữ liệu địa chỉ trước khi test (chạy Scripts/SeedAddressData.sql)

