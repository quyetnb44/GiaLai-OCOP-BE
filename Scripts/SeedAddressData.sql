-- Script seed dữ liệu địa chỉ Việt Nam (Tỉnh/Thành phố, Quận/Huyện, Phường/Xã)
-- Chạy script này sau khi đã chạy migration AddAddressFieldsToUsers.sql

-- Xóa dữ liệu cũ nếu có (tùy chọn)
-- TRUNCATE TABLE "Wards" CASCADE;
-- TRUNCATE TABLE "Districts" CASCADE;
-- TRUNCATE TABLE "Provinces" CASCADE;

-- ============================================
-- 1. INSERT PROVINCES (Tỉnh/Thành phố)
-- ============================================

-- Gia Lai
INSERT INTO "Provinces" ("Name", "Code") VALUES ('Gia Lai', '64') ON CONFLICT ("Code") DO NOTHING;

-- Bà Rịa - Vũng Tàu
INSERT INTO "Provinces" ("Name", "Code") VALUES ('Bà Rịa - Vũng Tàu', '77') ON CONFLICT ("Code") DO NOTHING;

-- Thành phố Hồ Chí Minh
INSERT INTO "Provinces" ("Name", "Code") VALUES ('Thành phố Hồ Chí Minh', '79') ON CONFLICT ("Code") DO NOTHING;

-- Thành phố Hà Nội
INSERT INTO "Provinces" ("Name", "Code") VALUES ('Thành phố Hà Nội', '01') ON CONFLICT ("Code") DO NOTHING;

-- ============================================
-- 2. INSERT DISTRICTS (Quận/Huyện)
-- ============================================

-- Gia Lai
INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Pleiku', '6401', "Id" FROM "Provinces" WHERE "Code" = '64' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'An Khê', '6402', "Id" FROM "Provinces" WHERE "Code" = '64' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Ayun Pa', '6403', "Id" FROM "Provinces" WHERE "Code" = '64' 
ON CONFLICT ("Code") DO NOTHING;

-- Bà Rịa - Vũng Tàu
INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Thành phố Vũng Tàu', '7701', "Id" FROM "Provinces" WHERE "Code" = '77' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Thành phố Bà Rịa', '7702', "Id" FROM "Provinces" WHERE "Code" = '77' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Huyện Châu Đức', '7703', "Id" FROM "Provinces" WHERE "Code" = '77' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Huyện Côn Đảo', '7704', "Id" FROM "Provinces" WHERE "Code" = '77' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Huyện Đất Đỏ', '7705', "Id" FROM "Provinces" WHERE "Code" = '77' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Huyện Long Điền', '7706', "Id" FROM "Provinces" WHERE "Code" = '77' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Huyện Tân Thành', '7707', "Id" FROM "Provinces" WHERE "Code" = '77' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Huyện Xuyên Mộc', '7708', "Id" FROM "Provinces" WHERE "Code" = '77' 
ON CONFLICT ("Code") DO NOTHING;

-- Thành phố Hồ Chí Minh
INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Quận 1', '7901', "Id" FROM "Provinces" WHERE "Code" = '79' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Quận 2', '7902', "Id" FROM "Provinces" WHERE "Code" = '79' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Quận 3', '7903', "Id" FROM "Provinces" WHERE "Code" = '79' 
ON CONFLICT ("Code") DO NOTHING;

-- Thành phố Hà Nội
INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Quận Ba Đình', '0101', "Id" FROM "Provinces" WHERE "Code" = '01' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Districts" ("Name", "Code", "ProvinceId") 
SELECT 'Quận Hoàn Kiếm', '0102', "Id" FROM "Provinces" WHERE "Code" = '01' 
ON CONFLICT ("Code") DO NOTHING;

-- ============================================
-- 3. INSERT WARDS (Phường/Xã)
-- ============================================

-- Pleiku, Gia Lai
INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường Yên Đỗ', '640101', "Id" FROM "Districts" WHERE "Code" = '6401' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường Yên Thế', '640102', "Id" FROM "Districts" WHERE "Code" = '6401' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường Trà Bá', '640103', "Id" FROM "Districts" WHERE "Code" = '6401' 
ON CONFLICT ("Code") DO NOTHING;

-- Thành phố Vũng Tàu, Bà Rịa - Vũng Tàu
INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường 1', '770101', "Id" FROM "Districts" WHERE "Code" = '7701' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường 2', '770102', "Id" FROM "Districts" WHERE "Code" = '7701' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường 3', '770103', "Id" FROM "Districts" WHERE "Code" = '7701' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường Thắng Nhất', '770104', "Id" FROM "Districts" WHERE "Code" = '7701' 
ON CONFLICT ("Code") DO NOTHING;

-- Thành phố Bà Rịa, Bà Rịa - Vũng Tàu
INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường Phước Hưng', '770201', "Id" FROM "Districts" WHERE "Code" = '7702' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường Phước Hiệp', '770202', "Id" FROM "Districts" WHERE "Code" = '7702' 
ON CONFLICT ("Code") DO NOTHING;

-- Quận 1, Thành phố Hồ Chí Minh
INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường Bến Nghé', '790101', "Id" FROM "Districts" WHERE "Code" = '7901' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường Đa Kao', '790102', "Id" FROM "Districts" WHERE "Code" = '7901' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường Bến Thành', '790103', "Id" FROM "Districts" WHERE "Code" = '7901' 
ON CONFLICT ("Code") DO NOTHING;

-- Quận Ba Đình, Thành phố Hà Nội
INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường Phúc Xá', '010101', "Id" FROM "Districts" WHERE "Code" = '0101' 
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO "Wards" ("Name", "Code", "DistrictId") 
SELECT 'Phường Trúc Bạch', '010102', "Id" FROM "Districts" WHERE "Code" = '0101' 
ON CONFLICT ("Code") DO NOTHING;

-- Lưu ý: Đây chỉ là dữ liệu mẫu. 
-- Để có đầy đủ dữ liệu địa chỉ Việt Nam, bạn cần import từ nguồn chính thức 
-- hoặc sử dụng API từ Bộ Tài nguyên và Môi trường Việt Nam.

