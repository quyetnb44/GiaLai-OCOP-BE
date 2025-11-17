# 🔐 Phân Tích Phân Quyền - GiaLai OCOP Backend

Tài liệu này phân tích chi tiết logic phân quyền cho 3 role: **SystemAdmin**, **EnterpriseAdmin** và **Customer**.

---

## 📊 Tổng Quan Phân Quyền

| Chức năng | SystemAdmin | EnterpriseAdmin | Customer |
|-----------|-------------|------------------|----------|
| **Quản lý Users** | ✅ Toàn quyền | ❌ Không | ❌ Không |
| **Quản lý Enterprises** | ✅ Toàn quyền | ⚠️ **THIẾU**: Không có endpoint xem/cập nhật doanh nghiệp của mình | ❌ Không |
| **Quản lý Products** | ✅ Toàn quyền + Duyệt sản phẩm | ✅ Chỉ sản phẩm của enterprise mình | ✅ Xem sản phẩm đã duyệt |
| **Quản lý Orders** | ✅ Toàn quyền | ✅ Chỉ đơn hàng có sản phẩm của enterprise mình | ✅ Chỉ đơn hàng của mình |
| **Quản lý Payments** | ✅ Toàn quyền | ✅ Chỉ payment của enterprise mình | ✅ Tạo payment cho đơn hàng của mình |
| **Quản lý Categories** | ✅ Toàn quyền | ⚠️ **VẤN ĐỀ**: Có thể quản lý tất cả categories | ❌ Không |
| **Quản lý Enterprise Applications** | ✅ Toàn quyền (duyệt/từ chối) | ❌ Không | ✅ Gửi đơn đăng ký |
| **Reports** | ✅ Toàn quyền | ❌ Không | ❌ Không |

---

## ✅ Các Điểm Đạt Chuẩn

### 1. **SystemAdmin - Toàn Quyền** ✅

**Vị trí:** Tất cả controllers

**Logic:**
- SystemAdmin có thể truy cập tất cả endpoints với `[Authorize(Roles = "SystemAdmin")]`
- Trong các method có logic phân quyền, SystemAdmin luôn được bypass:
  ```csharp
  if (User.IsInRole("SystemAdmin"))
      return true; // hoặc query tất cả
  ```

**Ví dụ:**
- `OrdersController.GetOrders()`: SystemAdmin xem tất cả đơn hàng
- `ProductsController.GetProducts()`: SystemAdmin xem tất cả sản phẩm (kể cả chưa duyệt)
- `PaymentsController`: SystemAdmin có thể xác nhận bất kỳ payment nào

**Đánh giá:** ✅ **ĐẠT CHUẨN**

---

### 2. **EnterpriseAdmin - Quản Lý Sản Phẩm** ✅

**Vị trí:** `ProductsController.cs`

**Logic:**
- **GET /api/products**: EnterpriseAdmin chỉ thấy sản phẩm của enterprise mình
  ```csharp
  if (role == "EnterpriseAdmin")
  {
      var enterpriseId = await _context.Users
          .Where(u => u.Id == currentUserId.Value)
          .Select(u => u.EnterpriseId)
          .FirstOrDefaultAsync();
      query = query.Where(p => p.EnterpriseId == enterpriseId);
  }
  ```

- **POST /api/products**: EnterpriseAdmin chỉ tạo sản phẩm cho enterprise mình
  ```csharp
  [Authorize(Roles = "EnterpriseAdmin")]
  var enterpriseId = await _context.Users
      .Where(u => u.Id == currentUserId.Value)
      .Select(u => u.EnterpriseId)
      .FirstOrDefaultAsync();
  if (enterpriseId == null)
      return BadRequest("EnterpriseAdmin không thuộc Enterprise nào.");
  ```

- **PUT /api/products/{id}**: EnterpriseAdmin chỉ sửa sản phẩm của enterprise mình
  ```csharp
  if (product.EnterpriseId != enterpriseId)
      return Forbid();
  ```

- **DELETE /api/products/{id}**: EnterpriseAdmin chỉ xóa sản phẩm của enterprise mình
  ```csharp
  if (product.EnterpriseId != enterpriseId)
      return Forbid();
  ```

**Đánh giá:** ✅ **ĐẠT CHUẨN** - EnterpriseAdmin chỉ quản lý sản phẩm của enterprise mình

---

### 3. **EnterpriseAdmin - Quản Lý Đơn Hàng** ✅

**Vị trí:** `OrdersController.cs`

**Logic:**
- **GET /api/orders**: EnterpriseAdmin chỉ thấy đơn hàng có sản phẩm của enterprise mình
  ```csharp
  else if (role == "EnterpriseAdmin")
  {
      var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
      if (enterpriseId == 0)
          return Forbid("EnterpriseAdmin không thuộc Enterprise nào.");
      
      query = _context.Orders
          .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
          .Include(o => o.Payments)
          .Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId));
  }
  ```

- **PUT /api/orders/{id}/status**: EnterpriseAdmin chỉ cập nhật đơn hàng có sản phẩm của enterprise mình
  ```csharp
  else if (role == "EnterpriseAdmin")
  {
      var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
      if (enterpriseId == 0)
          return Forbid("EnterpriseAdmin không thuộc Enterprise nào.");

      var hasAccess = order.OrderItems.Any(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId);
      if (!hasAccess)
          return Forbid("Bạn chỉ có thể cập nhật đơn hàng có sản phẩm của doanh nghiệp mình.");

      // EnterpriseAdmin không thể set status = "Cancelled"
      if (dto.Status == "Cancelled")
          return Forbid("EnterpriseAdmin không thể hủy đơn hàng. Chỉ Customer mới có thể hủy đơn hàng.");
  }
  ```

**Đánh giá:** ✅ **ĐẠT CHUẨN** - EnterpriseAdmin chỉ quản lý đơn hàng có sản phẩm của enterprise mình, và không thể hủy đơn hàng

---

### 4. **EnterpriseAdmin - Quản Lý Thanh Toán** ✅

**Vị trí:** `PaymentsController.cs`

**Logic:**
- **POST /api/payments/{id}/status**: EnterpriseAdmin chỉ xác nhận payment của enterprise mình
  ```csharp
  [Authorize(Roles = "SystemAdmin,EnterpriseAdmin")]
  // Kiểm tra quyền: EnterpriseAdmin chỉ có thể cập nhật payment của enterprise của mình
  if (User.IsInRole("EnterpriseAdmin") && !User.IsInRole("SystemAdmin"))
  {
      var userEnterpriseId = await _context.Users
          .Where(u => u.Id == userId.Value)
          .Select(u => u.EnterpriseId)
          .FirstOrDefaultAsync();

      if (userEnterpriseId == null || userEnterpriseId != payment.EnterpriseId)
          return Forbid("Bạn chỉ có thể cập nhật thanh toán của doanh nghiệp của mình.");
  }
  ```

**Đánh giá:** ✅ **ĐẠT CHUẨN** - EnterpriseAdmin chỉ xác nhận payment của enterprise mình

---

### 5. **Customer - Quản Lý Đơn Hàng** ✅

**Vị trí:** `OrdersController.cs`

**Logic:**
- **GET /api/orders**: Customer chỉ thấy đơn hàng của mình
  ```csharp
  if (role == "Customer")
  {
      query = _context.Orders
          .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
          .Include(o => o.Payments)
          .Where(o => o.UserId == userId.Value);
  }
  ```

- **POST /api/orders**: Chỉ Customer mới tạo được đơn hàng
  ```csharp
  [Authorize(Roles = "Customer")]
  ```

- **PUT /api/orders/{id}/status**: Customer chỉ có thể hủy đơn hàng của mình (khi còn Pending)
  ```csharp
  if (role == "Customer")
  {
      if (order.UserId != userId.Value)
          return Forbid("Bạn chỉ có thể cập nhật đơn hàng của chính mình.");

      if (dto.Status != "Cancelled")
          return Forbid("Customer chỉ có thể hủy đơn hàng (Cancelled).");

      // Customer chỉ có thể hủy khi đơn hàng vẫn còn ở trạng thái Pending
      if (order.Status != "Pending")
          return Forbid("Không thể hủy đơn hàng. Đơn hàng đã được doanh nghiệp xử lý.");
  }
  ```

**Đánh giá:** ✅ **ĐẠT CHUẨN** - Customer chỉ quản lý đơn hàng của mình, và chỉ có thể hủy khi đơn còn Pending

---

## ⚠️ Các Vấn Đề Cần Khắc Phục

### 1. **CategoriesController - EnterpriseAdmin Có Thể Quản Lý Tất Cả Categories** ✅ **ĐÃ SỬA**

**Vị trí:** `Controllers/CategoriesController.cs`

**Vấn đề (Đã sửa):**
- ~~EnterpriseAdmin có thể tạo/sửa/xóa categories~~ ❌
- Categories là danh mục toàn hệ thống, không thuộc về một enterprise cụ thể

**Giải pháp đã áp dụng:**
```csharp
// GET: Cho phép tất cả người dùng xem (công khai)
[AllowAnonymous]
[HttpGet]
public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(...)

[AllowAnonymous]
[HttpGet("{id}")]
public async Task<ActionResult<CategoryDto>> GetCategory(int id)

// POST/PUT/DELETE: Chỉ SystemAdmin
[Authorize(Roles = "SystemAdmin")]
[HttpPost]
public async Task<ActionResult<CategoryDto>> CreateCategory(...)

[Authorize(Roles = "SystemAdmin")]
[HttpPut("{id}")]
public async Task<IActionResult> UpdateCategory(...)

[Authorize(Roles = "SystemAdmin")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteCategory(...)
```

**Kết quả:** ✅ **ĐÃ KHẮC PHỤC**
- EnterpriseAdmin chỉ có thể **XEM** categories (công khai)
- Chỉ SystemAdmin mới có thể tạo/sửa/xóa categories
- Categories giờ là dữ liệu công khai, ai cũng có thể xem để chọn khi tạo sản phẩm

---

### 2. **EnterprisesController - EnterpriseAdmin Không Có Endpoint Xem/Cập Nhật Doanh Nghiệp Của Mình** ✅ **ĐÃ SỬA**

**Vị trí:** `Controllers/EnterprisesController.cs`

**Vấn đề (Đã sửa):**
- ~~EnterpriseAdmin không thể xem/cập nhật thông tin doanh nghiệp của mình~~ ❌

**Giải pháp đã áp dụng:**
Đã thêm 2 endpoint mới cho EnterpriseAdmin:

```csharp
// GET: api/enterprises/me - EnterpriseAdmin xem doanh nghiệp của mình
[HttpGet("me")]
[Authorize(Roles = "EnterpriseAdmin")]
public async Task<ActionResult<EnterpriseDto>> GetMyEnterprise()
{
    // Lấy userId từ token
    // Kiểm tra user có EnterpriseId
    // Trả về thông tin enterprise của user đó
}

// PUT: api/enterprises/me - EnterpriseAdmin cập nhật thông tin doanh nghiệp của mình
[HttpPut("me")]
[Authorize(Roles = "EnterpriseAdmin")]
public async Task<IActionResult> UpdateMyEnterprise([FromBody] UpdateEnterpriseDto dto)
{
    // Chỉ cho phép cập nhật các trường được phép
    // KHÔNG cho phép cập nhật OCOPRating (chỉ SystemAdmin mới được)
}
```

**Kết quả:** ✅ **ĐÃ KHẮC PHỤC**
- EnterpriseAdmin có thể xem thông tin doanh nghiệp của mình qua `GET /api/enterprises/me`
- EnterpriseAdmin có thể cập nhật thông tin doanh nghiệp của mình qua `PUT /api/enterprises/me`
- EnterpriseAdmin **KHÔNG THỂ** cập nhật `OCOPRating` (chỉ SystemAdmin mới được)
- Đã tạo `UpdateEnterpriseDto` với validation đầy đủ

---

### 3. **ProductsController - EnterpriseAdmin Có Thể Xem Sản Phẩm Chưa Duyệt Của Enterprise Khác** ⚠️

**Vị trí:** `Controllers/ProductsController.cs` - Method `GetProduct(int id)`

**Vấn đề:**
```csharp
[AllowAnonymous]
[HttpGet("{id}")]
public async Task<ActionResult<ProductDto>> GetProduct(int id)
{
    var product = await _context.Products
        .Include(p => p.Reviews)
        .Include(p => p.Category)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (product == null) return NotFound();

    var role = User.FindFirst(ClaimTypes.Role)?.Value;
    if (product.Status != "Approved")
    {
        if (role == "SystemAdmin")
        {
            // allow
        }
        else if (role == "EnterpriseAdmin")
        {
            var currentUserId = await GetUserIdFromTokenAsync();
            var enterpriseId = currentUserId.HasValue
                ? await _context.Users
                    .Where(u => u.Id == currentUserId.Value)
                    .Select(u => u.EnterpriseId)
                    .FirstOrDefaultAsync()
                : null;

            if (enterpriseId == null || product.EnterpriseId != enterpriseId)
                return Forbid("Sản phẩm chưa được duyệt.");
        }
        else
        {
            return NotFound();
        }
    }

    return Ok(MapProductToDto(product));
}
```

**Phân tích:**
- Logic này **ĐÚNG** - EnterpriseAdmin chỉ xem được sản phẩm chưa duyệt của enterprise mình
- Tuy nhiên, có thể cải thiện bằng cách kiểm tra sớm hơn

**Đánh giá:** ✅ **ĐẠT CHUẨN** (nhưng có thể tối ưu)

---

## 📋 Tổng Kết

### ✅ Đạt Chuẩn (9/9) - **HOÀN THIỆN 100%**
1. ✅ SystemAdmin - Toàn quyền
2. ✅ EnterpriseAdmin - Quản lý sản phẩm của enterprise mình
3. ✅ EnterpriseAdmin - Quản lý đơn hàng có sản phẩm của enterprise mình
4. ✅ EnterpriseAdmin - Quản lý payment của enterprise mình
5. ✅ Customer - Quản lý đơn hàng của mình
6. ✅ Customer - Chỉ có thể hủy đơn khi còn Pending
7. ✅ EnterpriseAdmin - Không thể hủy đơn hàng

### ⚠️ Cần Khắc Phục (0/9)
~~1. 🔴 **CategoriesController**: EnterpriseAdmin không nên quản lý categories~~ ✅ **ĐÃ SỬA**
~~2. ⚠️ **EnterprisesController**: Thiếu endpoint cho EnterpriseAdmin xem/cập nhật doanh nghiệp của mình~~ ✅ **ĐÃ SỬA**

---

## 🎯 Khuyến Nghị

### ✅ Đã hoàn thành:
1. ✅ **Sửa CategoriesController**: Chỉ cho phép EnterpriseAdmin **XEM** categories, không cho phép tạo/sửa/xóa
2. ✅ **Thêm endpoint EnterprisesController**: Cho EnterpriseAdmin xem/cập nhật doanh nghiệp của mình

### Ưu tiên trung bình (tùy chọn):
3. Tối ưu logic kiểm tra quyền trong `ProductsController.GetProduct()`
4. Thêm validation để đảm bảo EnterpriseAdmin luôn có `EnterpriseId` khi thực hiện các thao tác

---

**Cập nhật:** 2024-11-13

