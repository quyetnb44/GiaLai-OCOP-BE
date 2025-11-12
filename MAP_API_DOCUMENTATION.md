# 📍 Map API Documentation

## Tổng quan

API Map cung cấp các endpoints để tìm kiếm, lọc và hiển thị doanh nghiệp OCOP trên bản đồ. Tất cả endpoints đều **public** (không cần authentication).

---

## 🔹 Endpoints

### 1. Tìm kiếm theo từ khóa
**FR-MAP-01**

```
GET /api/map/search
```

**Query Parameters:**
- `keyword` (string, optional): Từ khóa tìm kiếm
- `userLat` (double, optional): Vĩ độ người dùng (để tính khoảng cách)
- `userLng` (double, optional): Kinh độ người dùng (để tính khoảng cách)
- `page` (int, default: 1): Số trang
- `pageSize` (int, default: 20, max: 100): Số lượng mỗi trang
- `sortBy` (string, default: "name"): Sắp xếp theo: `name`, `distance`, `rating`, `ocopRating`
- `sortOrder` (string, default: "asc"): Thứ tự: `asc`, `desc`

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "name": "HTX Nông nghiệp Cà Phê Pleiku",
      "address": "123 Đường Lê Lợi",
      "latitude": 13.9833,
      "longitude": 108.0000,
      "imageUrl": "https://example.com/images/cafe-pleiku.jpg",
      "averageRating": 4.5,
      "ocopRating": 5,
      "district": "Pleiku",
      "province": "Gia Lai",
      "distance": 2.5,
      "ratingCount": 15,
      "directionsUrl": "https://www.google.com/maps/dir/?api=1&destination=13.9833,108.0000"
    }
  ],
  "total": 10,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

**Ví dụ:**
```
GET /api/map/search?keyword=cà phê&userLat=13.98&userLng=108.00&sortBy=distance&page=1&pageSize=10
```

---

### 2. Tìm theo khu vực bản đồ (Bounding Box)
**FR-MAP-02**

```
GET /api/map/bounding-box
```

**Query Parameters:**
- `minLatitude` (double, required): Vĩ độ tối thiểu (-90 đến 90)
- `maxLatitude` (double, required): Vĩ độ tối đa (-90 đến 90)
- `minLongitude` (double, required): Kinh độ tối thiểu (-180 đến 180)
- `maxLongitude` (double, required): Kinh độ tối đa (-180 đến 180)
- `userLat`, `userLng`, `page`, `pageSize`, `sortBy`, `sortOrder` (optional): Giống endpoint search

**Ví dụ:**
```
GET /api/map/bounding-box?minLatitude=13.95&maxLatitude=14.00&minLongitude=107.95&maxLongitude=108.05&userLat=13.98&userLng=108.00
```

---

### 3. Tìm theo tọa độ và bán kính
**FR-MAP-08**

```
GET /api/map/nearby
```

**Query Parameters:**
- `latitude` (double, required): Vĩ độ (-90 đến 90)
- `longitude` (double, required): Kinh độ (-180 đến 180)
- `radius` (double, default: 10, max: 100): Bán kính tính bằng km
- `page`, `pageSize`, `sortBy`, `sortOrder` (optional)

**Ví dụ:**
```
GET /api/map/nearby?latitude=13.9833&longitude=108.0000&radius=5&sortBy=distance
```

**Lưu ý:** Mặc định sort theo `distance` (khoảng cách gần nhất trước).

---

### 4. Lọc doanh nghiệp theo nhiều điều kiện
**FR-MAP-06**

```
GET /api/map/filter
```

**Query Parameters:**
- `keyword` (string, optional): Từ khóa
- `district` (string, optional): Huyện/xã
- `province` (string, optional): Tỉnh/thành phố
- `ocopRating` (int, optional): Xếp hạng OCOP (3, 4, hoặc 5)
- `businessField` (string, optional): Ngành hàng
- `minLatitude`, `maxLatitude`, `minLongitude`, `maxLongitude` (double, optional): Bounding box
- `userLatitude`, `userLongitude`, `maxDistance` (double, optional): Lọc theo khoảng cách
- `page`, `pageSize`, `sortBy`, `sortOrder` (optional)

**Ví dụ:**
```
GET /api/map/filter?district=Pleiku&ocopRating=5&businessField=Cà phê&userLatitude=13.98&userLongitude=108.00&maxDistance=10&sortBy=rating&sortOrder=desc
```

---

### 5. Chi tiết doanh nghiệp
**FR-MAP-04**

```
GET /api/map/enterprises/{id}
```

**Query Parameters:**
- `userLat` (double, optional): Vĩ độ người dùng
- `userLng` (double, optional): Kinh độ người dùng

**Response:**
```json
{
  "id": 1,
  "name": "HTX Nông nghiệp Cà Phê Pleiku",
  "description": "Chuyên sản xuất và chế biến cà phê Robusta...",
  "address": "123 Đường Lê Lợi",
  "ward": "Phường Hội Thương",
  "district": "Pleiku",
  "province": "Gia Lai",
  "latitude": 13.9833,
  "longitude": 108.0000,
  "phoneNumber": "0269.1234567",
  "emailContact": "contact@cafepleiku.vn",
  "website": "https://cafepleiku.vn",
  "imageUrl": "https://example.com/images/cafe-pleiku.jpg",
  "averageRating": 4.5,
  "ocopRating": 5,
  "businessField": "Cà phê",
  "featuredProducts": [
    {
      "id": 1,
      "name": "Cà phê Robusta hạt rang xay",
      "description": "Cà phê Robusta rang xay nguyên chất...",
      "price": 150000,
      "imageUrl": "https://example.com/images/cafe-hat.jpg",
      "ocopRating": 5,
      "stockStatus": "InStock",
      "averageRating": 4.8,
      "enterpriseId": 1
    }
  ],
  "totalProducts": 5,
  "ratingCount": 15,
  "distance": 2.5,
  "directionsUrl": "https://www.google.com/maps/dir/?api=1&destination=13.9833,108.0000"
}
```

**Ví dụ:**
```
GET /api/map/enterprises/1?userLat=13.98&userLng=108.00
```

---

### 6. Danh sách sản phẩm của doanh nghiệp
**FR-MAP-05**

```
GET /api/map/enterprises/{id}/products
```

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 20, max: 100)

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "name": "Cà phê Robusta hạt rang xay",
      "description": "Cà phê Robusta rang xay nguyên chất, đóng gói 500g",
      "price": 150000,
      "imageUrl": "https://example.com/images/cafe-hat.jpg",
      "ocopRating": 5,
      "stockStatus": "InStock",
      "averageRating": 4.8,
      "enterpriseId": 1
    }
  ],
  "total": 5,
  "page": 1,
  "pageSize": 20
}
```

---

### 7. Lấy danh sách options cho filter
**Helper Endpoint**

```
GET /api/map/filter-options
```

**Response:**
```json
{
  "districts": ["Pleiku", "An Khê", "Ayun Pa"],
  "provinces": ["Gia Lai"],
  "businessFields": ["Cà phê", "Thảo dược", "Mật ong", "Rau củ quả"],
  "ocopRatings": [3, 4, 5]
}
```

**Mục đích:** Frontend có thể dùng để populate dropdown filters.

---

## 🔹 Tính năng nổi bật

### ✅ Distance Calculation
- Tự động tính khoảng cách (km) từ vị trí người dùng đến doanh nghiệp
- Sử dụng công thức Haversine (chính xác cho khoảng cách ngắn)
- Chỉ tính khi có `userLat` và `userLng`

### ✅ Directions URL
- Tự động tạo Google Maps directions URL
- Format: `https://www.google.com/maps/dir/?api=1&destination={lat},{lng}`
- Frontend chỉ cần mở URL này để chỉ đường

### ✅ Rating System
- `averageRating`: Điểm đánh giá trung bình (1-5) từ tất cả reviews
- `ratingCount`: Số lượng đánh giá
- `ocopRating`: Xếp hạng OCOP (3-5 sao)

### ✅ Sorting Options
- `name`: Sắp xếp theo tên
- `distance`: Sắp xếp theo khoảng cách (cần userLat/userLng)
- `rating`: Sắp xếp theo điểm đánh giá
- `ocopRating`: Sắp xếp theo xếp hạng OCOP

### ✅ Pagination
- Tất cả endpoints đều hỗ trợ pagination
- Response bao gồm: `total`, `page`, `pageSize`, `totalPages`, `hasNextPage`, `hasPreviousPage`

---

## 🔹 Validation & Error Handling

### Validation Rules:
- `latitude`: -90 đến 90
- `longitude`: -180 đến 180
- `radius`: 0.1 đến 100 km
- `page`: >= 1
- `pageSize`: 1 đến 100
- `ocopRating`: 3, 4, hoặc 5

### Error Responses:
```json
{
  "error": "Latitude phải nằm trong khoảng -90 đến 90."
}
```

---

## 🔹 Performance

### Database Indexes:
- ✅ `IX_Enterprises_Latitude_Longitude`: Tối ưu bounding box và nearby queries
- ✅ `IX_Enterprises_District`: Tối ưu filter theo district
- ✅ `IX_Enterprises_Province`: Tối ưu filter theo province
- ✅ `IX_Enterprises_OCOPRating`: Tối ưu filter theo OCOP rating
- ✅ `IX_Enterprises_BusinessField`: Tối ưu filter theo ngành hàng
- ✅ `IX_Enterprises_Name`: Tối ưu search theo tên

---

## 🔹 Seed Data

Trong môi trường Development, hệ thống tự động seed 5 doanh nghiệp mẫu với tọa độ tại Gia Lai:
1. HTX Nông nghiệp Cà Phê Pleiku
2. Công ty TNHH Hồng Sâm Gia Lai
3. HTX Mật ong rừng Tây Nguyên
4. Cơ sở Sản xuất Rượu cần Gia Lai
5. HTX Rau củ quả sạch An Khê

---

## 🔹 Frontend Integration Tips

### 1. Hiển thị Marker trên Map (FR-MAP-03)
```javascript
// Sử dụng Google Maps hoặc Leaflet
enterprises.forEach(enterprise => {
  const marker = new google.maps.Marker({
    position: { lat: enterprise.latitude, lng: enterprise.longitude },
    map: map,
    title: enterprise.name,
    icon: getMarkerIcon(enterprise.ocopRating) // Custom icon theo OCOP rating
  });
  
  // Click marker -> hiển thị popup
  marker.addListener('click', () => {
    showEnterprisePopup(enterprise);
  });
});
```

### 2. Chỉ đường (FR-MAP-07)
```javascript
// Khi user click nút "Chỉ đường"
function openDirections(enterprise) {
  if (enterprise.directionsUrl) {
    window.open(enterprise.directionsUrl, '_blank');
  }
}
```

### 3. Search this area
```javascript
// Khi user di chuyển map hoặc click "Search this area"
const bounds = map.getBounds();
const request = {
  minLatitude: bounds.getSouthWest().lat(),
  maxLatitude: bounds.getNorthEast().lat(),
  minLongitude: bounds.getSouthWest().lng(),
  maxLongitude: bounds.getNorthEast().lng(),
  userLat: userLocation.lat,
  userLng: userLocation.lng
};

fetch(`/api/map/bounding-box?${new URLSearchParams(request)}`)
  .then(res => res.json())
  .then(data => {
    // Update markers trên map
    updateMarkers(data.data);
  });
```

---

## 🔹 Testing

### Test trên Swagger:
1. Mở `http://localhost:5003/swagger`
2. Tìm section `Map`
3. Test các endpoints với dữ liệu mẫu

### Test Cases:
- ✅ Search với keyword
- ✅ Bounding box với tọa độ hợp lệ
- ✅ Nearby với radius khác nhau
- ✅ Filter với nhiều điều kiện
- ✅ Chi tiết doanh nghiệp
- ✅ Danh sách sản phẩm
- ✅ Filter options

---

## 📝 Notes

- Tất cả endpoints đều **public** (không cần JWT token)
- Distance chỉ được tính khi có `userLat` và `userLng`
- Directions URL luôn được tạo nếu doanh nghiệp có tọa độ
- Pagination mặc định: page=1, pageSize=20
- Sorting mặc định: sortBy=name, sortOrder=asc

---

**Version:** 1.0  
**Last Updated:** 2024-11-12

