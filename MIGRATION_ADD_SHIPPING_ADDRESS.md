# 📦 Migration: Thêm ShippingAddress vào Users table

## ⚠️ QUAN TRỌNG: Database là PostgreSQL (KHÔNG PHẢI SQL Server)

Lỗi `SQL80001: Incorrect syntax near 'COLUMN'` xảy ra khi bạn chạy script PostgreSQL trên SQL Server hoặc tool khác.

## Cách thực hiện (Chọn 1 trong 3 cách)

### ✅ Option 1: Entity Framework Migration (KHuyến nghị nhất)

Cách này tự động tạo migration phù hợp với PostgreSQL:

```bash
cd D:\GiaLai-OCOP-BE

# 1. Tạo migration
dotnet ef migrations add AddShippingAddressToUser

# 2. Áp dụng migration vào database
dotnet ef database update

# 3. Kiểm tra migration đã được tạo
# Xem file mới trong thư mục Migrations/
```

**Ưu điểm:**
- Tự động tạo migration đúng cú pháp PostgreSQL
- Có thể rollback nếu cần
- Theo dõi version của database schema

---

### ✅ Option 2: Chạy SQL trực tiếp trên PostgreSQL

**Nếu bạn dùng pgAdmin hoặc psql:**

```sql
-- PostgreSQL syntax (KHÔNG có từ khóa COLUMN sau ADD)
ALTER TABLE "Users" 
ADD "ShippingAddress" TEXT NULL;
```

**Nếu table name là lowercase (không có quotes):**

```sql
ALTER TABLE users 
ADD "ShippingAddress" TEXT NULL;
```

**Kiểm tra:**

```sql
-- Kiểm tra column đã được thêm chưa
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'Users' AND column_name = 'ShippingAddress';

-- Hoặc xem tất cả columns của bảng Users
SELECT * FROM information_schema.columns 
WHERE table_name = 'Users';
```

**Lưu ý:**
- ❌ KHÔNG chạy trên SQL Server Management Studio
- ❌ KHÔNG dùng cú pháp SQL Server: `ALTER TABLE ... ADD COLUMN ...`
- ✅ Dùng pgAdmin, DBeaver với PostgreSQL connection, hoặc psql command line

---

### ✅ Option 3: Chạy SQL qua Supabase Dashboard

1. Đăng nhập vào [Supabase Dashboard](https://app.supabase.com)
2. Chọn project của bạn
3. Vào **SQL Editor**
4. Chạy query:

```sql
ALTER TABLE "Users" 
ADD "ShippingAddress" TEXT NULL;
```

5. Click **Run** hoặc `Ctrl + Enter`

---

## ✅ Kiểm tra sau migration

### 1. Test trong pgAdmin/Supabase SQL Editor:

```sql
-- Xem cấu trúc bảng Users
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'Users' 
ORDER BY ordinal_position;

-- Xem dữ liệu
SELECT "Id", "Name", "Email", "ShippingAddress" 
FROM "Users" 
LIMIT 5;
```

### 2. Test Backend API:

```bash
# Test GET /api/users/me (phải có token)
curl -X GET "https://your-backend-url/api/users/me" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Response phải có field "shippingAddress" (camelCase) hoặc "ShippingAddress" (PascalCase)
```

### 3. Test Frontend:

1. Mở trang Account
2. Nhập địa chỉ giao hàng
3. Click "Lưu địa chỉ"
4. Refresh trang - địa chỉ phải hiển thị

---

## 🔍 Troubleshooting

### Lỗi: "column already exists"
```sql
-- Kiểm tra xem column đã tồn tại chưa
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'Users' AND column_name = 'ShippingAddress';

-- Nếu đã tồn tại, không cần chạy migration nữa
```

### Lỗi: "table does not exist"
```sql
-- Kiểm tra tên bảng (có thể là "users" lowercase hoặc "Users" với quotes)
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_type = 'BASE TABLE'
  AND table_name ILIKE '%user%';
```

### Lỗi: Permission denied
- Đảm bảo user database có quyền ALTER TABLE
- Nếu dùng Supabase, bạn phải có quyền admin hoặc dùng service role key

---

## 📝 Lưu ý

1. **PostgreSQL vs SQL Server syntax khác nhau:**
   - PostgreSQL: `ALTER TABLE table_name ADD column_name TYPE;`
   - SQL Server: `ALTER TABLE table_name ADD column_name TYPE;` (tương tự nhưng có thể cần GO)

2. **Column name case sensitivity:**
   - PostgreSQL: Case-sensitive khi dùng quotes `"Users"` vs `users`
   - Kiểm tra tên bảng trong database của bạn

3. **Migration với Entity Framework:**
   - Tự động tạo migration file `.cs`
   - Có thể xem trước SQL sẽ được chạy
   - Có thể rollback nếu cần
