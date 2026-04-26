-- =====================================================
-- جدول إشعارات النظام (systemnotifications)
-- يُنشأ مرة واحدة — PharmaSmart ERP
-- =====================================================

CREATE TABLE IF NOT EXISTS `systemnotifications` (
  `Id`           INT           NOT NULL AUTO_INCREMENT,
  `Category`     VARCHAR(50)   NOT NULL DEFAULT 'inventory' COMMENT 'inventory | shortage | expiry | admin',
  `Severity`     VARCHAR(20)   NOT NULL DEFAULT 'info'      COMMENT 'critical | warning | info',
  `Title`        VARCHAR(300)  NOT NULL DEFAULT '',
  `Body`         VARCHAR(1000) NOT NULL DEFAULT '',
  `Icon`         VARCHAR(100)  NOT NULL DEFAULT 'notifications',
  `IconColor`    VARCHAR(100)  NOT NULL DEFAULT 'text-blue-600',
  `BgColor`      VARCHAR(200)  NOT NULL DEFAULT 'bg-blue-50 border-blue-200',
  `BadgeColor`   VARCHAR(100)  NOT NULL DEFAULT 'bg-blue-500',
  `ActionUrl`    VARCHAR(300)  NOT NULL DEFAULT '#',
  `ActionText`   VARCHAR(100)  NOT NULL DEFAULT '—',
  `WhatsAppSent` TINYINT(1)    NOT NULL DEFAULT 0,
  `CreatedAt`    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `BranchId`     INT           NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  INDEX `idx_severity` (`Severity`),
  INDEX `idx_branch`   (`BranchId`),
  INDEX `idx_created`  (`CreatedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- نجاح: تم إنشاء جدول الإشعارات بنجاح.
