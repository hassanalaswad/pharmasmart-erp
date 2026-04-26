-- =====================================================================
-- إصلاح النصوص المعطبة (???????) في قاعدة البيانات
-- نفّذ هذا الاستعلام لإعادة تعيين الأسماء العربية الصحيحة وتصحيح الترميز
-- =====================================================================

-- 1. التأكد من أن الجداول تستخدم ترميز utf8mb4
ALTER TABLE `systemscreens` CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE `screenpermissions` CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 2. تحديث الأسماء العربية للفئات والشاشات (إصلاح القيمة المعطبة)
UPDATE `systemscreens` SET 
    ScreenArabicName = CASE 
        WHEN ScreenName = 'Sales' THEN 'المبيعات'
        WHEN ScreenName = 'SalesReturn' THEN 'مرتجع المبيعات'
        WHEN ScreenName = 'Purchases' THEN 'المشتريات'
        WHEN ScreenName = 'PurchasesReturn' THEN 'مرتجع المشتريات'
        WHEN ScreenName = 'Customers' THEN 'إدارة العملاء'
        WHEN ScreenName = 'Suppliers' THEN 'إدارة الموردين'
        WHEN ScreenName = 'Drugs' THEN 'الأدوية والمخزون'
        WHEN ScreenName = 'Inventory' THEN 'المخزون والجرد'
        WHEN ScreenName = 'Warehouses' THEN 'المستودعات والرفوف'
        WHEN ScreenName = 'ItemGroups' THEN 'المجموعات العلاجية'
        WHEN ScreenName = 'DrugTransfers' THEN 'التحويلات المخزنية'
        WHEN ScreenName = 'InventoryIntelligence' THEN 'ذكاء المخزون والتخطيط'
        WHEN ScreenName = 'StockAudit' THEN 'جرد المخزون'
        WHEN ScreenName = 'Accounting' THEN 'الدليل المحاسبي'
        WHEN ScreenName = 'JournalEntries' THEN 'القيود اليومية'
        WHEN ScreenName = 'Vouchers' THEN 'سندات القبض والصرف'
        WHEN ScreenName = 'FundTransfers' THEN 'التحويلات المالية'
        WHEN ScreenName = 'AccountReports' THEN 'التقارير المالية'
        WHEN ScreenName = 'ShortageForecast' THEN 'تقرير نواقص الأدوية'
        WHEN ScreenName = 'Report' THEN 'مركز التقارير'
        WHEN ScreenName = 'Employees' THEN 'سجل الموظفين'
        WHEN ScreenName = 'Users' THEN 'إدارة المستخدمين'
        WHEN ScreenName = 'Roles' THEN 'إدارة الأدوار'
        WHEN ScreenName = 'Branches' THEN 'إدارة الفروع'
        WHEN ScreenName = 'Currencies' THEN 'إدارة العملات'
        WHEN ScreenName = 'BarcodeGenerator' THEN 'توليد وطباعة الباركود'
        WHEN ScreenName = 'Admin' THEN 'إعدادات النظام'
        WHEN ScreenName = 'DrugBatches' THEN 'إدارة التشغيلات (Batches)'
        ELSE ScreenArabicName -- احتفظ بالقيمة إذا لم تطابق
    END,
    ScreenCategory = CASE 
        WHEN ScreenName IN ('Sales', 'SalesReturn', 'Customers', 'Suppliers', 'Purchases', 'PurchasesReturn') THEN 'التجاري'
        WHEN ScreenName IN ('Drugs', 'Inventory', 'Warehouses', 'ItemGroups', 'DrugTransfers', 'InventoryIntelligence', 'StockAudit', 'BarcodeGenerator', 'DrugBatches') THEN 'الأدوية والمخزون'
        WHEN ScreenName IN ('Accounting', 'JournalEntries', 'Vouchers', 'FundTransfers') THEN 'المالية'
        WHEN ScreenName IN ('AccountReports', 'ShortageForecast', 'Report') THEN 'التقارير'
        WHEN ScreenName IN ('Employees', 'Users', 'Roles', 'Branches', 'Currencies', 'Admin') THEN 'الإدارة'
        ELSE ScreenCategory
    END;

-- 3. التأكد من عدم وجود قيم NULL أو ?????? متبقية في الفئات
UPDATE `systemscreens` SET ScreenCategory = 'أخرى' WHERE ScreenCategory IS NULL OR ScreenCategory LIKE '%?%';

-- 4. معاينة البيانات للتأكد
SELECT ScreenName, ScreenArabicName, ScreenCategory FROM `systemscreens`;
