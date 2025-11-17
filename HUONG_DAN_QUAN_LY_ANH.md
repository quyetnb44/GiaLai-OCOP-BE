# 📸 Hướng Dẫn Quản Lý Ảnh

Tài liệu này mô tả hệ thống quản lý ảnh với phân quyền rõ ràng cho từng role.

---

## 🔐 Phân Quyền

### Customer
- ✅ Upload avatar (ảnh đại diện profile)
- ✅ Update avatar
- ✅ Xóa avatar của chính mình
- ❌ Không thể quản lý ảnh sản phẩm
- ❌ Không thể quản lý ảnh của user khác

### EnterpriseAdmin
- ✅ Upload ảnh sản phẩm (thuộc doanh nghiệp mình)
- ✅ Xóa ảnh sản phẩm (thuộc doanh nghiệp mình)
- ✅ Xem danh sách ảnh sản phẩm (thuộc doanh nghiệp mình)
- ❌ Không thể upload ảnh sản phẩm của doanh nghiệp khác
- ❌ Không thể quản lý avatar của Customer

### SystemAdmin
- ✅ Xem tất cả ảnh trong hệ thống
- ✅ Duyệt/từ chối ảnh sản phẩm
- ✅ Xóa bất kỳ ảnh nào
- ✅ Thống kê ảnh
- ✅ Quản lý toàn bộ ảnh (profile, product, enterprise)

---

## 📋 API Endpoints

### 1. Customer - Profile Avatar

#### POST /api/Profile/Avatar
Upload avatar cho Customer.

**Request:**
- Method: `POST`
- Headers: `Authorization: Bearer {token}`
- Content-Type: `multipart/form-data`
- Body: `file` (IFormFile)

**Response:**
```json
{
  "success": true,
  "message": "Upload avatar thành công.",
  "imageId": 1,
  "imageUrl": "https://example.com/uploads/images/avatars/guid.jpg",
  "fileName": "avatar.jpg"
}
```

**Validation:**
- Format: JPG, JPEG, PNG
- Max size: 5MB

---

#### PUT /api/Profile/Avatar
Update avatar (thực chất là upload mới và vô hiệu hóa avatar cũ).

**Request:**
- Method: `PUT`
- Headers: `Authorization: Bearer {token}`
- Content-Type: `multipart/form-data`
- Body: `file` (IFormFile)

**Response:** Giống POST

---

#### DELETE /api/Profile/Avatar
Xóa avatar (vô hiệu hóa).

**Request:**
- Method: `DELETE`
- Headers: `Authorization: Bearer {token}`

**Response:**
```json
{
  "success": true,
  "message": "Đã xóa avatar thành công."
}
```

---

#### GET /api/Profile/Avatar
Lấy avatar hiện tại.

**Request:**
- Method: `GET`
- Headers: `Authorization: Bearer {token}`

**Response:**
```json
{
  "imageId": 1,
  "imageUrl": "https://example.com/uploads/images/avatars/guid.jpg",
  "fileName": "avatar.jpg",
  "createdAt": "2024-11-17T10:00:00Z"
}
```

---

### 2. EnterpriseAdmin - Product Images

#### POST /api/ProductImages/Products/{productId}/Images
Upload ảnh sản phẩm.

**Request:**
- Method: `POST`
- Headers: `Authorization: Bearer {token}`
- Content-Type: `multipart/form-data`
- Body: `file` (IFormFile)
- Route: `productId` (int)

**Response:**
```json
{
  "success": true,
  "message": "Upload ảnh sản phẩm thành công. Ảnh đang chờ duyệt.",
  "imageId": 2,
  "imageUrl": "https://example.com/uploads/images/products/guid.jpg",
  "fileName": "product.jpg",
  "isApproved": false
}
```

**Validation:**
- Format: JPG, JPEG, PNG
- Max size: 10MB
- Product phải thuộc về Enterprise của EnterpriseAdmin

**Lưu ý:** Ảnh sản phẩm cần SystemAdmin duyệt trước khi hiển thị công khai.

---

#### DELETE /api/ProductImages/Products/{productId}/Images/{imageId}
Xóa ảnh sản phẩm.

**Request:**
- Method: `DELETE`
- Headers: `Authorization: Bearer {token}`
- Route: 
  - `productId` (int)
  - `imageId` (int)

**Response:**
```json
{
  "success": true,
  "message": "Đã xóa ảnh thành công."
}
```

**Lưu ý:** Chỉ có thể xóa ảnh của sản phẩm thuộc doanh nghiệp mình.

---

#### GET /api/ProductImages/Products/{productId}/Images
Lấy danh sách ảnh sản phẩm.

**Request:**
- Method: `GET`
- Headers: `Authorization: Bearer {token}`
- Route: `productId` (int)

**Response:**
```json
[
  {
    "id": 2,
    "url": "https://example.com/uploads/images/products/guid.jpg",
    "fileName": "product.jpg",
    "isApproved": true,
    "createdAt": "2024-11-17T10:00:00Z"
  }
]
```

---

### 3. SystemAdmin - Admin Images

#### GET /api/Admin/Images
Xem tất cả ảnh trong hệ thống (có phân trang và filter).

**Request:**
- Method: `GET`
- Headers: `Authorization: Bearer {token}`
- Query Parameters:
  - `imageType` (string, optional): "ProfileAvatar", "ProductImage", "EnterpriseImage", "Other"
  - `isApproved` (bool, optional): true/false
  - `isActive` (bool, optional): true/false
  - `page` (int, default: 1)
  - `pageSize` (int, default: 20)

**Response:**
```json
{
  "total": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5,
  "images": [
    {
      "id": 1,
      "url": "https://example.com/uploads/images/avatars/guid.jpg",
      "fileName": "avatar.jpg",
      "contentType": "image/jpeg",
      "fileSize": 102400,
      "imageType": "ProfileAvatar",
      "userId": 1,
      "productId": null,
      "enterpriseId": null,
      "productName": null,
      "enterpriseName": null,
      "uploadedByUserId": 1,
      "uploadedByRole": "Customer",
      "uploadedByName": "John Doe",
      "isActive": true,
      "isApproved": true,
      "createdAt": "2024-11-17T10:00:00Z",
      "updatedAt": null,
      "deletedAt": null
    }
  ]
}
```

---

#### GET /api/Admin/Images/{imageId}
Xem chi tiết ảnh.

**Request:**
- Method: `GET`
- Headers: `Authorization: Bearer {token}`
- Route: `imageId` (int)

**Response:**
```json
{
  "id": 1,
  "url": "https://example.com/uploads/images/avatars/guid.jpg",
  "fileName": "avatar.jpg",
  "contentType": "image/jpeg",
  "fileSize": 102400,
  "imageType": "ProfileAvatar",
  "userId": 1,
  "productId": null,
  "enterpriseId": null,
  "productName": null,
  "enterpriseName": null,
  "uploadedByUserId": 1,
  "uploadedByRole": "Customer",
  "uploadedByName": "John Doe",
  "isActive": true,
  "isApproved": true,
  "width": 800,
  "height": 600,
  "createdAt": "2024-11-17T10:00:00Z",
  "updatedAt": null,
  "deletedAt": null
}
```

---

#### PUT /api/Admin/Images/{imageId}/Approve
Duyệt ảnh.

**Request:**
- Method: `PUT`
- Headers: `Authorization: Bearer {token}`
- Route: `imageId` (int)

**Response:**
```json
{
  "success": true,
  "message": "Đã duyệt ảnh thành công."
}
```

---

#### PUT /api/Admin/Images/{imageId}/Reject
Từ chối ảnh.

**Request:**
- Method: `PUT`
- Headers: `Authorization: Bearer {token}`
- Route: `imageId` (int)

**Response:**
```json
{
  "success": true,
  "message": "Đã từ chối ảnh."
}
```

---

#### DELETE /api/Admin/Images/{imageId}
Xóa bất kỳ ảnh nào.

**Request:**
- Method: `DELETE`
- Headers: `Authorization: Bearer {token}`
- Route: `imageId` (int)

**Response:**
```json
{
  "success": true,
  "message": "Đã xóa ảnh thành công."
}
```

---

#### GET /api/Admin/Images/Stats
Thống kê ảnh.

**Request:**
- Method: `GET`
- Headers: `Authorization: Bearer {token}`

**Response:**
```json
{
  "totalImages": 1000,
  "activeImages": 950,
  "approvedImages": 900,
  "pendingImages": 50,
  "byType": [
    {
      "imageType": "ProfileAvatar",
      "count": 500,
      "activeCount": 480,
      "approvedCount": 480
    },
    {
      "imageType": "ProductImage",
      "count": 500,
      "activeCount": 470,
      "approvedCount": 420
    }
  ]
}
```

---

## 🔒 Bảo Mật

### Kiểm Tra Quyền

1. **Customer:**
   - Chỉ có thể upload/update/xóa avatar của chính mình
   - UserId được lấy từ JWT token, không từ client

2. **EnterpriseAdmin:**
   - Chỉ có thể upload/xóa ảnh sản phẩm thuộc doanh nghiệp mình
   - Kiểm tra `product.EnterpriseId == user.EnterpriseId` trước khi thao tác

3. **SystemAdmin:**
   - Có quyền với tất cả ảnh
   - Có thể duyệt/từ chối/xóa bất kỳ ảnh nào

### Validation

- **Format:** Chỉ chấp nhận JPG, JPEG, PNG
- **Size:**
  - Avatar: Tối đa 5MB
  - Product Image: Tối đa 10MB
- **File Extension:** Kiểm tra extension trước khi upload

---

## 💾 Lưu Trữ

### File System
- Avatar: `uploads/images/avatars/{guid}.{ext}`
- Product Images: `uploads/images/products/{guid}.{ext}`
- Enterprise Images: `uploads/images/enterprises/{guid}.{ext}` (nếu có)

### Database
- Bảng `Images` lưu metadata:
  - URL
  - FileName
  - ContentType
  - FileSize
  - ImageType
  - UserId/ProductId/EnterpriseId
  - UploadedByUserId
  - IsActive
  - IsApproved
  - CreatedAt, UpdatedAt, DeletedAt

### Soft Delete
- Xóa ảnh không xóa file vật lý
- Chỉ set `IsActive = false` và `DeletedAt = DateTime.UtcNow`
- Có thể khôi phục sau

---

## 📊 Workflow

### Customer Upload Avatar
1. Customer upload file
2. Validate file (format, size)
3. Upload file vào `uploads/images/avatars/`
4. Vô hiệu hóa avatar cũ (nếu có)
5. Lưu metadata vào database với `IsApproved = true` (tự động approved)
6. Trả về URL

### EnterpriseAdmin Upload Product Image
1. EnterpriseAdmin upload file
2. Validate file (format, size)
3. Kiểm tra quyền (product thuộc enterprise của admin)
4. Upload file vào `uploads/images/products/`
5. Lưu metadata vào database với `IsApproved = false` (chờ duyệt)
6. Trả về URL và trạng thái `isApproved = false`

### SystemAdmin Duyệt Ảnh
1. SystemAdmin xem danh sách ảnh chờ duyệt
2. Xem chi tiết ảnh
3. Duyệt hoặc từ chối
4. Nếu duyệt: `IsApproved = true`
5. Nếu từ chối: `IsApproved = false`, `IsActive = false`

---

## 🚀 Sử Dụng

### Frontend - Upload Avatar (Customer)

```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);

const response = await fetch('/api/Profile/Avatar', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`
  },
  body: formData
});

const result = await response.json();
console.log(result.imageUrl); // URL để hiển thị
```

### Frontend - Upload Product Image (EnterpriseAdmin)

```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);

const response = await fetch(`/api/ProductImages/Products/${productId}/Images`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`
  },
  body: formData
});

const result = await response.json();
if (!result.isApproved) {
  alert('Ảnh đang chờ duyệt từ SystemAdmin');
}
```

### Frontend - Admin Quản Lý Ảnh

```javascript
// Lấy danh sách ảnh chờ duyệt
const response = await fetch('/api/Admin/Images?isApproved=false&isActive=true', {
  headers: {
    'Authorization': `Bearer ${adminToken}`
  }
});

const data = await response.json();
// Hiển thị danh sách ảnh chờ duyệt

// Duyệt ảnh
await fetch(`/api/Admin/Images/${imageId}/Approve`, {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${adminToken}`
  }
});
```

---

## ✅ Checklist

- [x] Model Image với đầy đủ metadata
- [x] ProfileController cho Customer (avatar)
- [x] ProductImagesController cho EnterpriseAdmin
- [x] AdminImagesController cho SystemAdmin
- [x] Validation file (format, size)
- [x] Kiểm tra quyền trước khi thao tác
- [x] Soft delete
- [x] Lưu file và URL vào database
- [x] Migration database
- [x] Tài liệu API

---

**Cập nhật:** 2024-11-17  
**Status:** ✅ **HOÀN THÀNH**

