# Hướng dẫn tích hợp upload ảnh (Frontend ↔ Backend)

## 1. Endpoint & auth
- API: `POST /api/fileupload/image` (single) hoặc `POST /api/fileupload/images` (multi).
- Tham số query tùy chọn `folder` để lưu đúng thư mục Cloudinary, mặc định `GiaLaiOCOP/Images`.
- Bắt buộc header `Authorization: Bearer {jwt}` giống các API khác.
- Response thành công:
```json
{
  "success": true,
  "imageUrl": "https://res.cloudinary.com/.../sample.png",
  "publicId": "GiaLaiOCOP/Products/sample",
  "width": 1024,
  "height": 768,
  "format": "png"
}
```
→ FE chỉ cần lấy `imageUrl` (hoặc `publicId` nếu muốn xoá ảnh sau này) và gửi kèm payload tạo/cập nhật product/user/enterprise.

## 2. Cách gửi FormData
```ts
const formData = new FormData();
formData.append("file", file); // đơn ảnh
await fetch("/api/fileupload/image?folder=GiaLaiOCOP/Products", {
  method: "POST",
  headers: { Authorization: `Bearer ${token}` },
  body: formData,              // KHÔNG set Content-Type thủ công
});
```
Đa ảnh:
```ts
const formData = new FormData();
files.forEach((file) => formData.append("files", file));
await fetch("/api/fileupload/images", { method: "POST", headers: { Authorization: `Bearer ${token}` }, body: formData });
```

## 3. UI gợi ý (React + TSX)
```tsx
import { useState } from "react";

export function ImageUploader({ token, onUploaded }) {
  const [preview, setPreview] = useState<string>();
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string>();

  const handleChange = async (evt: React.ChangeEvent<HTMLInputElement>) => {
    const file = evt.target.files?.[0];
    if (!file) return;

    setPreview(URL.createObjectURL(file));
    setUploading(true);
    setError(undefined);
    try {
      const formData = new FormData();
      formData.append("file", file);
      const res = await fetch("/api/fileupload/image", {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        body: formData,
      });
      if (!res.ok) throw new Error(await res.text());
      const data = await res.json();
      onUploaded?.(data.imageUrl); // lưu URL
    } catch (err) {
      setError(err instanceof Error ? err.message : "Upload thất bại");
    } finally {
      setUploading(false);
    }
  };

  return (
    <div>
      <label>
        Chọn ảnh
        <input type="file" accept="image/*" onChange={handleChange} hidden />
      </label>
      {uploading && <p>Đang upload...</p>}
      {error && <p className="error">{error}</p>}
      {preview && <img src={preview} alt="preview" style={{ maxWidth: 200 }} />}
    </div>
  );
}
```
Đa ảnh: bật `multiple` và lặp qua `event.target.files`, gọi API `/images`.

## 4. Quy trình lưu dữ liệu
1. User chọn ảnh → FE gọi API upload → nhận `imageUrl`.
2. FE gửi `imageUrl` này trong payload các API khác (ví dụ `POST /api/products`, trường `imageUrl`).
3. Backend chỉ lưu URL nên không cần sửa schema.

## 5. Kiểm thử
- Dùng Swagger hoặc Postman gửi `multipart/form-data` để đảm bảo BE hoạt động.
- Trong FE, kiểm tra Network tab:
  - Request `POST /api/fileupload/image` phải hiển thị `Content-Type: multipart/form-data; boundary=...`
  - Response 200 với JSON như trên.
- Với đa ảnh, kiểm tra trường `uploadedFiles` trong response để lấy từng URL.

## 6. Xử lý lỗi & edge cases
- Kích thước tối đa 10MB/ảnh. Nếu vi phạm, backend trả 400; nên hiển thị thông báo “Ảnh quá lớn”.
- Định dạng hợp lệ: `.jpg .jpeg .png .gif .webp` cho endpoint image; `.pdf` + ảnh cho endpoint document.
- Nếu thiếu token hoặc token hết hạn → backend trả 401 → chuyển user về màn hình đăng nhập.

Giữ đúng các bước trên, FE có thể tích hợp upload mà không cần thay đổi thêm ở backend.














