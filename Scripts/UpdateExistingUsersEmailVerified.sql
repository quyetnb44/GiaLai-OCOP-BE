-- Script để cập nhật IsEmailVerified = true cho tất cả user cũ
-- Chạy script này trong database để user cũ có thể đăng nhập

UPDATE "Users"
SET "IsEmailVerified" = true
WHERE "IsEmailVerified" = false OR "IsEmailVerified" IS NULL;

-- Kiểm tra kết quả
SELECT 
    "Id",
    "Name",
    "Email",
    "Role",
    "IsEmailVerified",
    "CreatedAt"
FROM "Users"
ORDER BY "CreatedAt" DESC;

