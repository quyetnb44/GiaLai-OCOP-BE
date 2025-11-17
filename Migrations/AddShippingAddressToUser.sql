-- Migration: Add ShippingAddress column to Users table
-- PostgreSQL syntax (NOT SQL Server)
-- Run this SQL script on PostgreSQL database

-- PostgreSQL syntax: ALTER TABLE ... ADD COLUMN (không có từ khóa COLUMN sau ADD)
ALTER TABLE "Users" 
ADD "ShippingAddress" TEXT NULL;

-- Hoặc nếu table name là lowercase (không có quotes):
-- ALTER TABLE users 
-- ADD "ShippingAddress" TEXT NULL;

-- Kiểm tra column đã được thêm:
-- SELECT column_name, data_type, is_nullable 
-- FROM information_schema.columns 
-- WHERE table_name = 'Users' AND column_name = 'ShippingAddress';

