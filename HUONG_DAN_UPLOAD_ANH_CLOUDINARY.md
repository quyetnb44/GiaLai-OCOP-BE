# Hướng dẫn upload ảnh lên Cloudinary

## Backend
- Endpoint sử dụng: `POST /api/fileupload/image`
- Header: `Authorization: Bearer {token}`
- Body: `FormData` với khóa `file` chứa ảnh cần upload.
- Tham số tùy chọn: `folder` (query string) để đẩy vào thư mục Cloudinary cụ thể.
- Response mẫu:
```json
{
  "success": true,
  "message": "Upload hình ảnh thành công.",
  "imageUrl": "https://res.cloudinary.com/<cloud>/image/upload/v123456789/sample.png",
  "publicId": "GiaLaiOCOP/Images/sample",
  "width": 1024,
  "height": 768,
  "format": "png"
}
```

## Frontend (ví dụ React + TypeScript)
```tsx
import { useState } from "react";

export function CloudinaryUploader() {
  const [preview, setPreview] = useState<string>();
  const [uploading, setUploading] = useState(false);

  const handleChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setPreview(URL.createObjectURL(file));
    const formData = new FormData();
    formData.append("file", file);

    setUploading(true);
    try {
      const response = await fetch("/api/fileupload/image?folder=GiaLaiOCOP/Products", {
        method: "POST",
        headers: {
          Authorization: `Bearer ${localStorage.getItem("accessToken") ?? ""}`,
        },
        body: formData,
      });

      if (!response.ok) throw new Error(await response.text());
      const result = await response.json();
      console.log("Cloudinary URL:", result.imageUrl);
    } finally {
      setUploading(false);
    }
  };

  return (
    <div>
      <label className="upload-button">
        Chọn ảnh
        <input type="file" accept="image/*" onChange={handleChange} hidden />
      </label>
      {uploading && <p>Đang upload...</p>}
      {preview && <img src={preview} alt="preview" style={{ maxWidth: 240 }} />}
    </div>
  );
}
```

> **Lưu ý:** Backend chỉ trả về URL Cloudinary, hãy lưu URL này vào database/thông tin sản phẩm tương ứng.

