-- Migration: Thêm các trường địa chỉ chi tiết vào bảng Users
-- Chạy migration này để thêm các cột: province_id, district_id, ward_id, address_detail

-- 1. Thêm các cột mới vào bảng Users
ALTER TABLE "Users" 
ADD COLUMN IF NOT EXISTS "ProvinceId" INTEGER NULL,
ADD COLUMN IF NOT EXISTS "DistrictId" INTEGER NULL,
ADD COLUMN IF NOT EXISTS "WardId" INTEGER NULL,
ADD COLUMN IF NOT EXISTS "AddressDetail" TEXT NULL;

-- 2. Tạo bảng Provinces
CREATE TABLE IF NOT EXISTS "Provinces" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Code" TEXT NOT NULL UNIQUE
);

-- 3. Tạo bảng Districts
CREATE TABLE IF NOT EXISTS "Districts" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Code" TEXT NOT NULL UNIQUE,
    "ProvinceId" INTEGER NOT NULL,
    CONSTRAINT "FK_Districts_Provinces_ProvinceId" 
        FOREIGN KEY ("ProvinceId") 
        REFERENCES "Provinces" ("Id") 
        ON DELETE RESTRICT
);

-- 4. Tạo bảng Wards
CREATE TABLE IF NOT EXISTS "Wards" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Code" TEXT NOT NULL UNIQUE,
    "DistrictId" INTEGER NOT NULL,
    CONSTRAINT "FK_Wards_Districts_DistrictId" 
        FOREIGN KEY ("DistrictId") 
        REFERENCES "Districts" ("Id") 
        ON DELETE RESTRICT
);

-- 5. Thêm foreign keys cho Users
ALTER TABLE "Users"
ADD CONSTRAINT "FK_Users_Provinces_ProvinceId" 
    FOREIGN KEY ("ProvinceId") 
    REFERENCES "Provinces" ("Id") 
    ON DELETE RESTRICT;

ALTER TABLE "Users"
ADD CONSTRAINT "FK_Users_Districts_DistrictId" 
    FOREIGN KEY ("DistrictId") 
    REFERENCES "Districts" ("Id") 
    ON DELETE RESTRICT;

ALTER TABLE "Users"
ADD CONSTRAINT "FK_Users_Wards_WardId" 
    FOREIGN KEY ("WardId") 
    REFERENCES "Wards" ("Id") 
    ON DELETE RESTRICT;

-- 6. Tạo indexes để tối ưu performance
CREATE INDEX IF NOT EXISTS "IX_Districts_ProvinceId" ON "Districts" ("ProvinceId");
CREATE INDEX IF NOT EXISTS "IX_Wards_DistrictId" ON "Wards" ("DistrictId");
CREATE INDEX IF NOT EXISTS "IX_Users_ProvinceId" ON "Users" ("ProvinceId");
CREATE INDEX IF NOT EXISTS "IX_Users_DistrictId" ON "Users" ("DistrictId");
CREATE INDEX IF NOT EXISTS "IX_Users_WardId" ON "Users" ("WardId");



