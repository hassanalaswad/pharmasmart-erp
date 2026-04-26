-- =====================================================================
-- تحديث الأسماء العربية لجدول systemscreens
-- نفّذ هذا الاستعلام في phpMyAdmin لإصلاح ??????? نهائياً
-- =====================================================================

UPDATE `systemscreens` SET ScreenArabicName = 'المبيعات',                    ScreenCategory = 'التجاري'    WHERE ScreenName = 'Sales';
UPDATE `systemscreens` SET ScreenArabicName = 'مرتجع المبيعات',              ScreenCategory = 'التجاري'    WHERE ScreenName = 'SalesReturn';
UPDATE `systemscreens` SET ScreenArabicName = 'المشتريات',                   ScreenCategory = 'التجاري'    WHERE ScreenName = 'Purchases';
UPDATE `systemscreens` SET ScreenArabicName = 'مرتجع المشتريات',             ScreenCategory = 'التجاري'    WHERE ScreenName = 'PurchasesReturn';
UPDATE `systemscreens` SET ScreenArabicName = 'إدارة العملاء',               ScreenCategory = 'التجاري'    WHERE ScreenName = 'Customers';
UPDATE `systemscreens` SET ScreenArabicName = 'إدارة الموردين',              ScreenCategory = 'التجاري'    WHERE ScreenName = 'Suppliers';
UPDATE `systemscreens` SET ScreenArabicName = 'الأدوية والمخزون',            ScreenCategory = 'المخزون'    WHERE ScreenName = 'Drugs';
UPDATE `systemscreens` SET ScreenArabicName = 'المخزون والجرد',              ScreenCategory = 'المخزون'    WHERE ScreenName = 'Inventory';
UPDATE `systemscreens` SET ScreenArabicName = 'المستودعات والرفوف',          ScreenCategory = 'المخزون'    WHERE ScreenName = 'Warehouses';
UPDATE `systemscreens` SET ScreenArabicName = 'المجموعات العلاجية',          ScreenCategory = 'المخزون'    WHERE ScreenName = 'ItemGroups';
UPDATE `systemscreens` SET ScreenArabicName = 'التحويلات المخزنية',          ScreenCategory = 'المخزون'    WHERE ScreenName = 'DrugTransfers';
UPDATE `systemscreens` SET ScreenArabicName = 'ذكاء المخزون والتخطيط',      ScreenCategory = 'المخزون'    WHERE ScreenName = 'InventoryIntelligence';
UPDATE `systemscreens` SET ScreenArabicName = 'جرد المخزون',                 ScreenCategory = 'المخزون'    WHERE ScreenName = 'StockAudit';
UPDATE `systemscreens` SET ScreenArabicName = 'الدليل المحاسبي',             ScreenCategory = 'المالية'    WHERE ScreenName = 'Accounting';
UPDATE `systemscreens` SET ScreenArabicName = 'القيود اليومية',              ScreenCategory = 'المالية'    WHERE ScreenName = 'JournalEntries';
UPDATE `systemscreens` SET ScreenArabicName = 'سندات القبض والصرف',          ScreenCategory = 'المالية'    WHERE ScreenName = 'Vouchers';
UPDATE `systemscreens` SET ScreenArabicName = 'التحويلات المالية',           ScreenCategory = 'المالية'    WHERE ScreenName = 'FundTransfers';
UPDATE `systemscreens` SET ScreenArabicName = 'التقارير المالية',            ScreenCategory = 'التقارير'   WHERE ScreenName = 'AccountReports';
UPDATE `systemscreens` SET ScreenArabicName = 'تقرير نواقص الأدوية',        ScreenCategory = 'التقارير'   WHERE ScreenName = 'ShortageForecast';
UPDATE `systemscreens` SET ScreenArabicName = 'مركز التقارير',              ScreenCategory = 'التقارير'   WHERE ScreenName = 'Report';
UPDATE `systemscreens` SET ScreenArabicName = 'سجل الموظفين',               ScreenCategory = 'الإدارة'    WHERE ScreenName = 'Employees';
UPDATE `systemscreens` SET ScreenArabicName = 'إدارة المستخدمين',           ScreenCategory = 'الإدارة'    WHERE ScreenName = 'Users';
UPDATE `systemscreens` SET ScreenArabicName = 'إدارة الأدوار',              ScreenCategory = 'الإدارة'    WHERE ScreenName = 'Roles';
UPDATE `systemscreens` SET ScreenArabicName = 'إدارة الفروع',               ScreenCategory = 'الإدارة'    WHERE ScreenName = 'Branches';
UPDATE `systemscreens` SET ScreenArabicName = 'إدارة العملات',              ScreenCategory = 'الإدارة'    WHERE ScreenName = 'Currencies';
UPDATE `systemscreens` SET ScreenArabicName = 'مولد الباركود',              ScreenCategory = 'المخزون'    WHERE ScreenName = 'BarcodeGenerator';
UPDATE `systemscreens` SET ScreenArabicName = 'إعدادات النظام',             ScreenCategory = 'الإدارة'    WHERE ScreenName = 'Admin';

-- تحقق من النتيجة
SELECT ScreenName, ScreenArabicName, ScreenCategory FROM `systemscreens` ORDER BY ScreenCategory, ScreenArabicName;
