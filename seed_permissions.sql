-- =====================================================================
-- تعيين صلاحيات الأدوار — PharmaSmart ERP
-- RoleId=1: SuperAdmin (يصل لكل شيء عبر الكود مباشرة)
-- RoleId=2: مدير الفرع (Branch Manager) — وصول واسع
-- RoleId=3: كاشير/صيدلاني (Pharmacist) — وصول محدود
-- تشغيل مرة واحدة على قاعدة البيانات
-- =====================================================================

-- ──────────────────────────────────────────────────────────────────────
-- الخطوة 1: التأكد من وجود جميع الشاشات في جدول systemscreens
-- ──────────────────────────────────────────────────────────────────────
INSERT IGNORE INTO `systemscreens` (ScreenName, ScreenArabicName, ScreenCategory) VALUES
('Sales',                 'المبيعات',                     'التجاري'),
('SalesReturn',           'مرتجع المبيعات',               'التجاري'),
('Purchases',             'المشتريات',                    'التجاري'),
('PurchasesReturn',       'مرتجع المشتريات',              'التجاري'),
('Customers',             'إدارة العملاء',                'التجاري'),
('Suppliers',             'إدارة الموردين',               'التجاري'),
('Drugs',                 'الأدوية والمخزون',             'المخزون'),
('Inventory',             'المخزون والجرد',               'المخزون'),
('Warehouses',            'المستودعات والرفوف',           'المخزون'),
('ItemGroups',            'المجموعات العلاجية',           'المخزون'),
('DrugTransfers',         'التحويلات المخزنية',           'المخزون'),
('InventoryIntelligence', 'ذكاء المخزون والتخطيط',       'المخزون'),
('Accounting',            'الدليل المحاسبي',              'المالية'),
('JournalEntries',        'القيود اليومية',               'المالية'),
('Vouchers',              'سندات القبض والصرف',           'المالية'),
('FundTransfers',         'التحويلات المالية',            'المالية'),
('AccountReports',        'التقارير المالية',             'التقارير'),
('ShortageForecast',      'تقرير نواقص الأدوية',          'التقارير'),
('Employees',             'سجل الموظفين',                 'الإدارة'),
('Users',                 'إدارة المستخدمين',             'الإدارة'),
('Roles',                 'إدارة الأدوار',                'الإدارة'),
('Branches',              'إدارة الفروع',                 'الإدارة'),
('Currencies',            'إدارة العملات',                'الإدارة'),
('BarcodeGenerator',      'مولد الباركود',                'المخزون');

-- ──────────────────────────────────────────────────────────────────────
-- الخطوة 2: حذف الصلاحيات القديمة لـ RoleId=2 و RoleId=3 لتجنب التكرار
-- ──────────────────────────────────────────────────────────────────────
DELETE FROM `screenpermissions` WHERE RoleID IN (2, 3);

-- ──────────────────────────────────────────────────────────────────────
-- الخطوة 3: إدراج صلاحيات مدير الفرع (RoleId=2)
-- يملك وصولاً لكل الشاشات التشغيلية — مع قيد بعض عمليات الإدارة العليا
-- ──────────────────────────────────────────────────────────────────────
INSERT INTO `screenpermissions` (RoleID, ScreenID, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
SELECT 
    2,
    s.screenID,
    1,  -- CanView
    CASE WHEN s.ScreenName IN ('Users','Roles','Branches') THEN 0 ELSE 1 END,  -- CanAdd
    CASE WHEN s.ScreenName IN ('Users','Roles','Branches') THEN 0 ELSE 1 END,  -- CanEdit
    CASE WHEN s.ScreenName IN ('Users','Roles','Branches','JournalEntries') THEN 0 ELSE 1 END,  -- CanDelete
    1   -- CanPrint
FROM `systemscreens` s;

-- ──────────────────────────────────────────────────────────────────────
-- الخطوة 4: إدراج صلاحيات الكاشير/الصيدلاني (RoleId=3)
-- يملك وصولاً للمبيعات والمخزون الأساسية فقط
-- ──────────────────────────────────────────────────────────────────────
INSERT INTO `screenpermissions` (RoleID, ScreenID, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
SELECT
    3,
    s.screenID,
    CASE WHEN s.ScreenName IN (
        'Sales','SalesReturn','Drugs','Inventory','Customers',
        'BarcodeGenerator','ShortageForecast','Suppliers','Purchases','PurchasesReturn'
    ) THEN 1 ELSE 0 END,  -- CanView
    CASE WHEN s.ScreenName IN ('Sales','SalesReturn') THEN 1 ELSE 0 END,  -- CanAdd
    0,  -- CanEdit
    0,  -- CanDelete
    CASE WHEN s.ScreenName IN ('Sales','BarcodeGenerator') THEN 1 ELSE 0 END  -- CanPrint
FROM `systemscreens` s;

-- ──────────────────────────────────────────────────────────────────────
-- نجاح: تم تعيين الصلاحيات بنجاح.
-- تحقق: SELECT COUNT(*) FROM screenpermissions WHERE RoleID=2;
-- ──────────────────────────────────────────────────────────────────────
SELECT 
    r.RoleID,
    r.RoleName,
    COUNT(p.PermissionID) AS TotalScreens,
    SUM(p.CanView) AS CanView,
    SUM(p.CanAdd) AS CanAdd
FROM screenpermissions p
JOIN userroles r ON r.RoleID = p.RoleID
WHERE r.RoleID IN (2, 3)
GROUP BY r.RoleID, r.RoleName;
