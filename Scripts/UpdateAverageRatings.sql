-- Script để cập nhật AverageRating cho tất cả Products và Enterprises hiện có
-- Chạy script này sau khi deploy RatingService để đảm bảo dữ liệu nhất quán

-- 1. Cập nhật AverageRating cho Products từ Reviews
UPDATE "Products" p
SET "AverageRating" = (
    SELECT ROUND(AVG(r."Rating")::numeric, 2)
    FROM "Reviews" r
    WHERE r."ProductId" = p."Id"
),
"UpdatedAt" = CURRENT_TIMESTAMP
WHERE EXISTS (
    SELECT 1 FROM "Reviews" r WHERE r."ProductId" = p."Id"
);

-- 2. Set AverageRating = NULL cho Products không có Review
UPDATE "Products"
SET "AverageRating" = NULL,
"UpdatedAt" = CURRENT_TIMESTAMP
WHERE NOT EXISTS (
    SELECT 1 FROM "Reviews" r WHERE r."ProductId" = "Products"."Id"
);

-- 3. Cập nhật AverageRating cho Enterprises từ AverageRating của Products Approved
UPDATE "Enterprises" e
SET "AverageRating" = (
    SELECT ROUND(AVG(p."AverageRating")::numeric, 2)
    FROM "Products" p
    WHERE p."EnterpriseId" = e."Id"
        AND p."Status" = 'Approved'
        AND p."AverageRating" IS NOT NULL
),
"UpdatedAt" = CURRENT_TIMESTAMP
WHERE EXISTS (
    SELECT 1 
    FROM "Products" p 
    WHERE p."EnterpriseId" = e."Id"
        AND p."Status" = 'Approved'
        AND p."AverageRating" IS NOT NULL
);

-- 4. Set AverageRating = NULL cho Enterprises không có Product Approved có Review
UPDATE "Enterprises"
SET "AverageRating" = NULL,
"UpdatedAt" = CURRENT_TIMESTAMP
WHERE NOT EXISTS (
    SELECT 1 
    FROM "Products" p 
    WHERE p."EnterpriseId" = "Enterprises"."Id"
        AND p."Status" = 'Approved'
        AND p."AverageRating" IS NOT NULL
);

-- Kiểm tra kết quả
SELECT 
    'Products có AverageRating' as "Type",
    COUNT(*) as "Count"
FROM "Products"
WHERE "AverageRating" IS NOT NULL
UNION ALL
SELECT 
    'Products không có AverageRating' as "Type",
    COUNT(*) as "Count"
FROM "Products"
WHERE "AverageRating" IS NULL
UNION ALL
SELECT 
    'Enterprises có AverageRating' as "Type",
    COUNT(*) as "Count"
FROM "Enterprises"
WHERE "AverageRating" IS NOT NULL
UNION ALL
SELECT 
    'Enterprises không có AverageRating' as "Type",
    COUNT(*) as "Count"
FROM "Enterprises"
WHERE "AverageRating" IS NULL;

