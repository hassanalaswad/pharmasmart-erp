-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: 08 أبريل 2026 الساعة 14:22
-- إصدار الخادم: 10.4.32-MariaDB
-- PHP Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `dblast`
--

-- --------------------------------------------------------

--
-- بنية الجدول `accountingtemplatelines`
--

CREATE TABLE `accountingtemplatelines` (
  `LineId` int(11) NOT NULL,
  `TemplateId` int(11) NOT NULL,
  `IsDebit` tinyint(1) NOT NULL,
  `Role` int(11) NOT NULL,
  `Source` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- إرجاع أو استيراد بيانات الجدول `accountingtemplatelines`
--

INSERT INTO `accountingtemplatelines` (`LineId`, `TemplateId`, `IsDebit`, `Role`, `Source`) VALUES
(1, 1, 1, 0, 1),
(2, 1, 1, 1, 2),
(3, 1, 1, 2, 3),
(4, 1, 1, 6, 4),
(5, 1, 0, 4, 0),
(6, 1, 0, 7, 4),
(7, 2, 1, 4, 0),
(8, 2, 1, 7, 4),
(9, 2, 0, 0, 1),
(10, 2, 0, 1, 2),
(11, 2, 0, 2, 3),
(12, 2, 0, 6, 4),
(17, 3, 1, 7, 0),
(18, 3, 0, 0, 1),
(19, 3, 0, 1, 2),
(20, 3, 0, 3, 3),
(21, 3, 0, 8, 4),
(22, 4, 1, 0, 1),
(23, 4, 1, 1, 2),
(24, 4, 1, 3, 3),
(25, 4, 0, 7, 0);

-- --------------------------------------------------------

--
-- بنية الجدول `accountingtemplates`
--

CREATE TABLE `accountingtemplates` (
  `TemplateId` int(11) NOT NULL,
  `TemplateName` longtext DEFAULT NULL,
  `TransactionType` int(11) NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- إرجاع أو استيراد بيانات الجدول `accountingtemplates`
--

INSERT INTO `accountingtemplates` (`TemplateId`, `TemplateName`, `TransactionType`, `IsActive`) VALUES
(1, 'قالب فاتورة المبيعات القياسي', 0, 1),
(2, 'قالب مرتجع المبيعات القياسي', 1, 1),
(3, 'قيد فاتورة المشتريات القياسي', 2, 1),
(4, 'قالب مرتجع المشتريات القياسي', 3, 1);

-- --------------------------------------------------------

--
-- بنية الجدول `accountmappings`
--

CREATE TABLE `accountmappings` (
  `MappingId` int(11) NOT NULL,
  `Role` int(11) NOT NULL,
  `BranchId` int(11) DEFAULT NULL,
  `PaymentMethodId` int(11) DEFAULT NULL,
  `AccountId` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- إرجاع أو استيراد بيانات الجدول `accountmappings`
--

INSERT INTO `accountmappings` (`MappingId`, `Role`, `BranchId`, `PaymentMethodId`, `AccountId`) VALUES
(1, 0, 1, NULL, 27),
(2, 1, 1, NULL, 21),
(3, 6, 1, NULL, 15),
(4, 7, 1, NULL, 20),
(5, 4, 1, NULL, 12);

-- --------------------------------------------------------

--
-- بنية الجدول `accounts`
--

CREATE TABLE `accounts` (
  `AccountID` int(11) NOT NULL,
  `AccountCode` varchar(50) NOT NULL,
  `AccountName` varchar(150) NOT NULL,
  `AccountType` varchar(50) NOT NULL,
  `BranchId` int(11) DEFAULT NULL,
  `AccountNature` tinyint(1) NOT NULL DEFAULT 1 COMMENT '1 = Debit (مدين), 0 = Credit (دائن)',
  `ParentAccountID` int(11) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  `IsDeleted` tinyint(1) DEFAULT 0,
  `CreatedAt` datetime DEFAULT current_timestamp(),
  `CreatedBy` int(11) DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `UpdatedBy` int(11) DEFAULT NULL,
  `IsParent` tinyint(1) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `accounts`
--

INSERT INTO `accounts` (`AccountID`, `AccountCode`, `AccountName`, `AccountType`, `BranchId`, `AccountNature`, `ParentAccountID`, `IsActive`, `IsDeleted`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsParent`) VALUES
(1, '1', 'الأصول', 'Assets', NULL, 1, NULL, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 1),
(2, '11', 'الأصول المتداولة', 'Assets', NULL, 1, 1, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 1),
(3, '12', 'الأصول غير المتداولة', 'Assets', NULL, 1, 1, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 0),
(4, '2', 'الخصوم', 'Liabilities', NULL, 0, NULL, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 1),
(5, '21', 'الخصوم المتداولة', 'Liabilities', NULL, 0, 4, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 1),
(6, '22', 'الخصوم طويلة الأجل', 'Liabilities', NULL, 0, 4, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 0),
(7, '3', 'حقوق الملكية', 'Equity', NULL, 0, NULL, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 1),
(8, '31', 'رأس المال', 'Equity', NULL, 0, 7, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 0),
(9, '32', 'المسحوبات', 'Equity', NULL, 1, 7, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 0),
(10, '33', 'الأرباح المحتجزة', 'Equity', NULL, 0, 7, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 0),
(11, '4', 'الإيرادات', 'Revenue', NULL, 0, NULL, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 1),
(12, '41', 'المبيعات', 'Revenue', NULL, 0, 11, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 0),
(13, '42', 'إيرادات أخرى', 'Revenue', NULL, 0, 11, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 0),
(14, '5', 'تكلفة المبيعات', 'Expenses', NULL, 1, NULL, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 1),
(15, '51', 'المشتريات', 'Expenses', NULL, 1, 14, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 0),
(16, '6', 'المصروفات', 'Expenses', NULL, 1, NULL, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 1),
(17, '61', 'مصروفات تشغيلية', 'Expenses', NULL, 1, 16, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 0),
(18, '62', 'مصروفات إدارية', 'Expenses', NULL, 1, 16, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 0),
(19, '63', 'مصروفات تسويقية', 'Expenses', NULL, 1, 16, 1, 0, '2026-03-14 22:35:24', NULL, NULL, NULL, 0),
(20, '111', 'مخزون الادوية ', 'Assets', NULL, 1, 2, 1, NULL, NULL, NULL, NULL, NULL, 0),
(21, '112', 'حساب البنك 1', 'Assets', NULL, 1, 2, 1, NULL, NULL, NULL, NULL, NULL, 0),
(22, '211', 'الموردين', 'Liabilities', NULL, 0, 5, 1, NULL, NULL, NULL, NULL, NULL, 1),
(23, '21101', 'القطريفي', 'Liabilities', NULL, 0, 22, 0, NULL, NULL, NULL, NULL, NULL, 0),
(24, '212', 'العملاء', 'Liabilities', NULL, 0, 5, 1, NULL, NULL, NULL, NULL, NULL, 1),
(25, '21201', 'احمد ', 'Liabilities', NULL, 0, 24, 1, NULL, NULL, NULL, NULL, NULL, 0),
(26, '21102', 'الشرق الاوسط', 'Liabilities', NULL, 0, 22, 1, NULL, NULL, NULL, NULL, NULL, 0),
(27, '113', 'الفرع  الرئسي صندوق رقم 1', 'Assets', 1, 1, 2, 1, NULL, NULL, NULL, NULL, NULL, 0),
(28, '43', 'ايراد مشتريات ', 'Revenue', 1, 0, 11, 1, NULL, NULL, NULL, NULL, NULL, 0),
(29, '44', 'ايرد مبيعات ', 'Revenue', 1, 0, 11, 1, NULL, NULL, NULL, NULL, NULL, 0),
(30, '21103', 'حساب مورد: شركة ابن سينا', 'Liabilities', NULL, 0, 22, 1, 0, '2026-03-30 01:19:57', NULL, NULL, NULL, 0),
(31, '21104', 'حساب مورد: شركة الرازي', 'Liabilities', NULL, 0, 22, 1, 0, '2026-03-30 01:19:57', NULL, NULL, NULL, 0),
(32, '21105', 'حساب مورد: مؤسسة الأدوية', 'Liabilities', NULL, 0, 22, 1, 0, '2026-03-30 01:19:57', NULL, NULL, NULL, 0),
(33, '21106', 'حساب مورد: فارماكير', 'Liabilities', NULL, 0, 22, 1, 0, '2026-03-30 01:19:57', NULL, NULL, NULL, 0),
(34, '21107', 'حساب مورد: جلوبال ميد', 'Liabilities', NULL, 0, 22, 1, 0, '2026-03-30 01:19:57', NULL, NULL, NULL, 0),
(100, '211100', 'شركة باير الطبية', 'Liabilities', NULL, 0, 22, 1, 0, '2026-03-30 01:28:18', NULL, NULL, NULL, 0),
(101, '212100', 'صيدلية النور', 'Assets', NULL, 1, 24, 1, 0, '2026-03-30 01:28:18', NULL, NULL, NULL, 0),
(102, '212101', 'ابو يمان ', 'Liabilities', NULL, 0, 24, 1, NULL, NULL, NULL, NULL, NULL, 0);

-- --------------------------------------------------------

--
-- بنية الجدول `barcodegenerator`
--

CREATE TABLE `barcodegenerator` (
  `Id` int(11) NOT NULL,
  `BranchId` int(11) NOT NULL DEFAULT 1,
  `DrugId` int(11) NOT NULL,
  `BatchNumber` varchar(50) NOT NULL,
  `ExpiryDate` date NOT NULL,
  `CurrentPrice` decimal(18,4) NOT NULL,
  `QuantityToPrint` int(11) NOT NULL DEFAULT 1,
  `GeneratedCode` varchar(255) NOT NULL COMMENT 'الكود الديناميكي المركب',
  `IsPrinted` tinyint(1) NOT NULL DEFAULT 0,
  `CreatedAt` datetime NOT NULL,
  `UserId` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- إرجاع أو استيراد بيانات الجدول `barcodegenerator`
--

INSERT INTO `barcodegenerator` (`Id`, `BranchId`, `DrugId`, `BatchNumber`, `ExpiryDate`, `CurrentPrice`, `QuantityToPrint`, `GeneratedCode`, `IsPrinted`, `CreatedAt`, `UserId`) VALUES
(1, 1, 7, '12453', '2028-06-08', 600.0000, 33, '7-12453-2806-600', 0, '2026-03-17 23:36:37', 1),
(2, 1, 4, '212', '2027-06-17', 1400.0000, 125, '4-212-2706-1400', 0, '2026-03-18 23:22:06', 1),
(3, 1, 4, '212', '2027-06-17', 1400.0000, 125, '4-212-2706-1400', 0, '2026-03-18 23:25:24', 1),
(5, 1, 7, '4444', '2027-03-31', 300.0000, 9, '7-4444-2703-300', 0, '2026-03-23 16:42:31', 1),
(8, 1, 1, '213', '2027-11-08', -0.0100, 320, '1-213-2711-0', 0, '2026-03-31 21:37:10', 1),
(9, 1, 3, '230', '2027-04-12', 600.0000, 24, '3-230-2704-600', 0, '2026-04-01 08:17:13', 1),
(10, 1, 3, '8598', '2027-05-04', 1400.0000, 48, '3-8598-2705-1400', 0, '2026-04-05 00:31:59', 1),
(11, 1, 3, '8598', '2028-02-01', 1400.0000, 40, '3-8598-2802-1400', 0, '2026-04-05 00:42:47', 1);

-- --------------------------------------------------------

--
-- بنية الجدول `branches`
--

CREATE TABLE `branches` (
  `BranchID` int(11) NOT NULL,
  `BranchCode` varchar(20) NOT NULL,
  `BranchName` varchar(150) NOT NULL,
  `Location` varchar(200) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  `DefaultCashAccountId` int(11) DEFAULT NULL COMMENT 'حساب الصندوق الافتراضي',
  `DefaultSalesAccountId` int(11) DEFAULT NULL COMMENT 'حساب إيرادات المبيعات',
  `DefaultCOGSAccountId` int(11) DEFAULT NULL COMMENT 'حساب تكلفة البضاعة المباعة',
  `DefaultInventoryAccountId` int(11) DEFAULT NULL COMMENT 'حساب مخزون الأدوية',
  `DefaultCurrencyId` int(11) DEFAULT NULL COMMENT 'العملة الافتراضية للفرع'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `branches`
--

INSERT INTO `branches` (`BranchID`, `BranchCode`, `BranchName`, `Location`, `IsActive`, `DefaultCashAccountId`, `DefaultSalesAccountId`, `DefaultCOGSAccountId`, `DefaultInventoryAccountId`, `DefaultCurrencyId`) VALUES
(1, 'BR-01', 'الفرع الرئيسي', 'المركز الرئيسي', 1, NULL, NULL, NULL, NULL, 1),
(2, 'BR-02', 'فرع تالين فارما 3', 'شارع صنعاء', 1, NULL, NULL, NULL, NULL, NULL);

-- --------------------------------------------------------

--
-- بنية الجدول `branchinventory`
--

CREATE TABLE `branchinventory` (
  `BranchID` int(11) NOT NULL,
  `DrugID` int(11) NOT NULL,
  `ShelfId` int(11) DEFAULT NULL,
  `StockQuantity` int(11) NOT NULL DEFAULT 0,
  `MinimumStockLevel` int(11) NOT NULL DEFAULT 0,
  `ABCCategory` char(1) DEFAULT 'C',
  `AverageCost` decimal(18,4) DEFAULT 0.0000,
  `CurrentSellingPrice` decimal(18,4) DEFAULT 0.0000
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `branchinventory`
--

INSERT INTO `branchinventory` (`BranchID`, `DrugID`, `ShelfId`, `StockQuantity`, `MinimumStockLevel`, `ABCCategory`, `AverageCost`, `CurrentSellingPrice`) VALUES
(1, 1, NULL, 390, 10, 'B', 170.0000, -0.0100),
(1, 3, NULL, 60, 10, 'C', 225.0000, 1400.0000),
(1, 4, NULL, 75, 10, 'B', 40.0000, 1400.0000),
(1, 7, NULL, 40, 10, 'C', 79.5833, 300.0000),
(1, 8, NULL, 100, 20, 'C', 500.0000, 600.0000),
(1, 9, NULL, 200, 50, 'C', 150.0000, 200.0000),
(1, 10, NULL, 100, 15, 'C', 400.0000, 500.0000),
(1, 11, NULL, 250, 30, 'C', 100.0000, 150.0000),
(1, 12, NULL, 120, 25, 'C', 500.0000, 650.0000),
(1, 100, NULL, 72, 0, 'B', 1000.0000, 1500.0000);

-- --------------------------------------------------------

--
-- بنية الجدول `companysettings`
--

CREATE TABLE `companysettings` (
  `Id` int(11) NOT NULL,
  `CompanyName` varchar(200) NOT NULL,
  `CompanyLogoPath` varchar(500) DEFAULT NULL,
  `Address` varchar(500) DEFAULT NULL,
  `Email` varchar(100) DEFAULT NULL,
  `Phone` varchar(50) DEFAULT NULL,
  `TaxNumber` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- إرجاع أو استيراد بيانات الجدول `companysettings`
--

INSERT INTO `companysettings` (`Id`, `CompanyName`, `CompanyLogoPath`, `Address`, `Email`, `Phone`, `TaxNumber`) VALUES
(1, 'تالين فارما', 'logo_20260405042927.png', NULL, NULL, NULL, NULL);

-- --------------------------------------------------------

--
-- بنية الجدول `currencies`
--

CREATE TABLE `currencies` (
  `CurrencyId` int(11) NOT NULL,
  `CurrencyCode` varchar(10) NOT NULL COMMENT 'مثل: YER, USD, SAR',
  `CurrencyName` varchar(50) NOT NULL COMMENT 'الاسم بالعربي',
  `ExchangeRate` decimal(18,4) NOT NULL DEFAULT 1.0000 COMMENT 'سعر الصرف مقابل العملة المحلية',
  `IsBaseCurrency` tinyint(1) NOT NULL DEFAULT 0 COMMENT '1 = هذه هي العملة المحلية الأساسية للنظام',
  `IsActive` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `currencies`
--

INSERT INTO `currencies` (`CurrencyId`, `CurrencyCode`, `CurrencyName`, `ExchangeRate`, `IsBaseCurrency`, `IsActive`) VALUES
(1, 'YER', 'ريال يمني', 1.0000, 1, 1);

-- --------------------------------------------------------

--
-- بنية الجدول `customers`
--

CREATE TABLE `customers` (
  `CustomerID` int(11) NOT NULL,
  `BranchId` int(11) NOT NULL DEFAULT 1,
  `FullName` varchar(150) NOT NULL,
  `Phone` varchar(50) DEFAULT NULL,
  `Address` text DEFAULT NULL,
  `CreditLimit` decimal(18,2) DEFAULT 0.00,
  `AccountID` int(11) NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  `CreatedAt` datetime DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `customers`
--

INSERT INTO `customers` (`CustomerID`, `BranchId`, `FullName`, `Phone`, `Address`, `CreditLimit`, `AccountID`, `IsActive`, `CreatedAt`) VALUES
(100, 1, 'صيدلية النور', '770000200', NULL, 0.00, 101, 1, '2026-03-30 01:28:18');

-- --------------------------------------------------------

--
-- بنية الجدول `drugcategories`
--

CREATE TABLE `drugcategories` (
  `CategoryId` int(11) NOT NULL,
  `CategoryName` varchar(100) NOT NULL,
  `Description` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `drugcategories`
--

INSERT INTO `drugcategories` (`CategoryId`, `CategoryName`, `Description`) VALUES
(1, 'مضادات حيوية', 'أدوية لعلاج الالتهابات البكتيرية'),
(2, 'فيتامينات ومكملات', 'فيتامينات متعددة ومعادن لرفع المناعة'),
(3, 'أدوية ضغط الدم', 'علاجات لارتفاع وانخفاض ضغط الدم'),
(4, 'أدوية السكري', 'علاجات لتنظيم مستوى السكر في الدم'),
(5, 'أدوية الجهاز الهضمي', 'علاجات للمعدة والقولون والحموضة'),
(100, 'أدوية القلب والشرايين', 'أدوية متعلقة بضغط الدم والقلب');

-- --------------------------------------------------------

--
-- بنية الجدول `drugs`
--

CREATE TABLE `drugs` (
  `DrugID` int(11) NOT NULL,
  `DrugName` varchar(150) NOT NULL,
  `Manufacturer` varchar(150) DEFAULT NULL,
  `GroupId` int(11) DEFAULT NULL,
  `Barcode` varchar(50) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  `SaremaCategory` varchar(10) DEFAULT 'S',
  `CategoryName` varchar(100) DEFAULT NULL,
  `CategoryId` int(11) DEFAULT NULL,
  `ImagePath` varchar(255) DEFAULT NULL,
  `MainUnit` varchar(50) NOT NULL DEFAULT 'علبة' COMMENT 'وحدة الشراء الكبرى',
  `UnitId` int(11) DEFAULT NULL,
  `SubUnit` varchar(50) NOT NULL DEFAULT 'حبة' COMMENT 'وحدة البيع الصغرى',
  `ConversionFactor` int(11) NOT NULL DEFAULT 1 COMMENT 'العبوة / معامل التحويل',
  `IsDeleted` tinyint(1) DEFAULT 0,
  `CreatedAt` datetime DEFAULT current_timestamp(),
  `CreatedBy` int(11) DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `UpdatedBy` int(11) DEFAULT NULL,
  `IsLifeSaving` tinyint(1) DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `drugs`
--

INSERT INTO `drugs` (`DrugID`, `DrugName`, `Manufacturer`, `GroupId`, `Barcode`, `IsActive`, `SaremaCategory`, `CategoryName`, `CategoryId`, `ImagePath`, `MainUnit`, `UnitId`, `SubUnit`, `ConversionFactor`, `IsDeleted`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsLifeSaving`) VALUES
(1, 'بنادول', 'kkm', NULL, '756313', 1, NULL, NULL, NULL, NULL, 'باكت', NULL, 'حبة', 10, NULL, '2026-03-15 04:52:35', 1, NULL, NULL, 0),
(2, 'فولتارين', 'mnl', NULL, '123334', 1, NULL, NULL, NULL, NULL, 'باكت', NULL, 'أمبولة', 8, NULL, '2026-03-15 04:54:17', 1, NULL, NULL, 0),
(3, 'امول ', 'fdg', NULL, '12356', 1, NULL, NULL, NULL, NULL, 'باكت', NULL, 'شريط', 4, NULL, '2026-03-17 04:46:54', 1, NULL, NULL, 0),
(4, 'سولبادين', 'mmm', 1, 'A-000-0001', 1, NULL, NULL, NULL, NULL, 'باكت', NULL, 'حبة', 25, NULL, '2026-03-17 22:33:58', 1, NULL, NULL, 0),
(5, 'فلازول', 'dd', 1, 'A-000-0002', 1, NULL, NULL, NULL, NULL, 'عبوة', NULL, 'عبوة', 1, NULL, '2026-03-17 23:00:32', 1, NULL, NULL, 0),
(6, 'سولبافيز', 'mnl', 1, 'A-000-0003', 1, NULL, NULL, NULL, NULL, 'باكت', NULL, 'حبة', 5, NULL, '2026-03-17 23:09:50', 1, NULL, NULL, 0),
(7, 'داينكسيت', 'cvg', 1, 'A-000-0004', 1, NULL, NULL, NULL, NULL, 'باكت', NULL, 'شريط', 3, NULL, '2026-03-17 23:36:26', 1, NULL, NULL, 0),
(8, 'أموكسيل 500 مجم', 'جلاكسو', 2, 'BAR-1001', 1, 'S', NULL, 1, NULL, 'باكت', NULL, 'شريط', 10, 0, '2026-03-30 01:19:57', NULL, NULL, NULL, 1),
(9, 'سي فيت 1000', 'نوفارتس', 3, 'BAR-1002', 1, 'S', NULL, 2, NULL, 'عبوة', NULL, 'حبة', 30, 0, '2026-03-30 01:19:57', NULL, NULL, NULL, 0),
(10, 'كونكور 5 مجم', 'ميرك', 4, 'BAR-1003', 1, 'S', NULL, 3, NULL, 'باكت', NULL, 'شريط', 30, 0, '2026-03-30 01:19:57', NULL, NULL, NULL, 1),
(11, 'جلوكوفاج 500', 'ميرك', 5, 'BAR-1004', 1, 'S', NULL, 4, NULL, 'باكت', NULL, 'شريط', 50, 0, '2026-03-30 01:19:57', NULL, NULL, NULL, 1),
(12, 'نكسيوم 40 مجم', 'استرازينيكا', 6, 'BAR-1005', 1, 'S', NULL, 5, NULL, 'باكت', NULL, 'شريط', 14, 0, '2026-03-30 01:19:57', NULL, NULL, NULL, 0),
(100, 'كونكور 5 ملجم', 'Merck', 100, '123456789100', 1, 'S', NULL, 100, NULL, 'باكت', NULL, 'شريط', 30, 0, '2026-03-30 01:28:18', NULL, NULL, NULL, 0),
(101, 'بنادول', 'yk', 100, 'G-100-0002', 1, NULL, NULL, NULL, NULL, 'باكت', NULL, 'شريط', 6, NULL, '2026-03-30 01:36:32', 1, '2026-04-05 04:05:28', 1, 0);

-- --------------------------------------------------------

--
-- بنية الجدول `drugtransferdetails`
--

CREATE TABLE `drugtransferdetails` (
  `DetailID` int(11) NOT NULL,
  `TransferID` int(11) NOT NULL,
  `DrugID` int(11) NOT NULL,
  `Quantity` int(11) NOT NULL,
  `UnitCost` decimal(18,4) NOT NULL DEFAULT 0.0000
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- بنية الجدول `drugtransfers`
--

CREATE TABLE `drugtransfers` (
  `TransferID` int(11) NOT NULL,
  `FromBranchID` int(11) NOT NULL,
  `ToBranchID` int(11) NOT NULL,
  `TransferDate` datetime NOT NULL DEFAULT current_timestamp(),
  `ReceiveDate` datetime DEFAULT NULL,
  `Status` varchar(20) NOT NULL DEFAULT 'Pending',
  `CreatedBy` int(11) NOT NULL,
  `ReceivedBy` int(11) DEFAULT NULL,
  `Notes` varchar(250) DEFAULT NULL,
  `JournalId` int(11) DEFAULT NULL,
  `ReceiptJournalId` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- بنية الجدول `drug_batches`
--

CREATE TABLE `drug_batches` (
  `BatchId` int(11) NOT NULL,
  `DrugId` int(11) NOT NULL,
  `BatchNumber` varchar(100) NOT NULL,
  `ProductionDate` date DEFAULT NULL,
  `ExpiryDate` date NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `drug_batches`
--

INSERT INTO `drug_batches` (`BatchId`, `DrugId`, `BatchNumber`, `ProductionDate`, `ExpiryDate`) VALUES
(1, 1, '123', NULL, '2029-07-15'),
(2, 7, '12453', NULL, '2028-06-08'),
(3, 4, '212', NULL, '2027-06-17'),
(5, 7, '4444', NULL, '2027-03-31'),
(8, 100, 'BATCH-C001', NULL, '2028-12-31'),
(9, 1, '213', NULL, '2027-11-08'),
(10, 3, '230', NULL, '2027-04-12'),
(11, 3, '8598', NULL, '2027-05-04');

-- --------------------------------------------------------

--
-- بنية الجدول `employees`
--

CREATE TABLE `employees` (
  `EmployeeID` int(11) NOT NULL,
  `BranchID` int(11) NOT NULL,
  `FullName` varchar(150) NOT NULL,
  `Position` varchar(100) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  `Salary` decimal(18,2) DEFAULT 0.00,
  `Phone` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `employees`
--

INSERT INTO `employees` (`EmployeeID`, `BranchID`, `FullName`, `Position`, `IsActive`, `Salary`, `Phone`) VALUES
(1, 1, 'احمد محمد احمد', 'صيدلي', 1, 50000.00, NULL),
(2, 1, 'احمد محمد علي ', 'صيدلي', 1, 50000.00, NULL),
(3, 2, 'احمد صالح علي ', 'صيدلي', 1, 500000.00, NULL),
(4, 2, 'احمد صالح علي ', 'صيدلي', 1, 500000.00, NULL);

-- --------------------------------------------------------

--
-- بنية الجدول `forecasts`
--

CREATE TABLE `forecasts` (
  `ForecastID` int(11) NOT NULL,
  `BranchID` int(11) NOT NULL,
  `DrugID` int(11) NOT NULL,
  `ForecastDate` date NOT NULL,
  `PredictedDemand` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- بنية الجدول `fundtransfers`
--

CREATE TABLE `fundtransfers` (
  `TransferID` int(11) NOT NULL,
  `BranchId` int(11) NOT NULL DEFAULT 1,
  `FromAccountID` int(11) NOT NULL,
  `ToAccountID` int(11) NOT NULL,
  `Amount` decimal(18,2) NOT NULL,
  `TransferDate` datetime NOT NULL DEFAULT current_timestamp(),
  `ReferenceNo` varchar(50) DEFAULT NULL,
  `Notes` varchar(250) DEFAULT NULL,
  `CreatedBy` int(11) NOT NULL,
  `JournalId` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- بنية الجدول `itemgroups`
--

CREATE TABLE `itemgroups` (
  `GroupId` int(11) NOT NULL,
  `GroupCode` varchar(50) DEFAULT NULL COMMENT 'رمز المجموعة (مثال: A, B, C)',
  `GroupName` varchar(150) NOT NULL COMMENT 'اسم المجموعة أو المادة الفعالة (مثال: باراسيتامول 500)',
  `Description` varchar(255) DEFAULT NULL COMMENT 'ملاحظات ووصف',
  `Notes` varchar(255) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1 COMMENT 'حالة التفعيل 1=نشط، 0=موقوف'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- إرجاع أو استيراد بيانات الجدول `itemgroups`
--

INSERT INTO `itemgroups` (`GroupId`, `GroupCode`, `GroupName`, `Description`, `Notes`, `IsActive`) VALUES
(1, 'A-000', 'مسكنات الالم ', NULL, NULL, 1),
(2, 'G-002', 'مجموعة البنسلين', 'مضادات حيوية بنسلينية', NULL, 1),
(3, 'G-003', 'فيتامين سي', 'مكملات فيتامين سي لرفع المناعة', NULL, 1),
(4, 'G-004', 'مثبطات بيتا', 'تستخدم لعلاج ارتفاع ضغط الدم', NULL, 1),
(5, 'G-005', 'ميتفورمين', 'منظمات سكر الدم', NULL, 1),
(6, 'G-006', 'مضادات الحموضة', 'أدوية حموضة المعدة (PPI)', NULL, 1),
(100, 'G-100', 'مثبطات بيتا (Beta Blockers)', NULL, NULL, 1);

-- --------------------------------------------------------

--
-- بنية الجدول `journaldetails`
--

CREATE TABLE `journaldetails` (
  `DetailID` int(11) NOT NULL,
  `JournalID` int(11) NOT NULL,
  `AccountID` int(11) NOT NULL,
  `Debit` decimal(18,2) NOT NULL DEFAULT 0.00,
  `Credit` decimal(18,2) NOT NULL DEFAULT 0.00
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `journaldetails`
--

INSERT INTO `journaldetails` (`DetailID`, `JournalID`, `AccountID`, `Debit`, `Credit`) VALUES
(1, 1, 20, 80000.00, 0.00),
(2, 1, 21, 20000.00, 0.00),
(3, 1, 8, 0.00, 100000.00),
(4, 2, 21, 0.00, 10000.00),
(5, 2, 10, 10000.00, 0.00),
(6, 3, 21, 0.00, 2500.00),
(7, 3, 20, 2750.00, 0.00),
(8, 3, 13, 0.00, 250.00),
(12, 5, 26, 0.00, 5000.00),
(13, 5, 20, 5000.00, 0.00),
(14, 6, 12, 0.00, 1200.00),
(15, 6, 15, 166.67, 0.00),
(16, 6, 27, 1200.00, 0.00),
(17, 6, 20, 0.00, 166.67),
(18, 7, 21, 0.00, 600.00),
(19, 7, 20, 600.00, 0.00),
(22, 9, 12, 0.00, 1200.00),
(23, 9, 15, 100.00, 0.00),
(24, 9, 27, 1200.00, 0.00),
(25, 9, 20, 0.00, 100.00),
(26, 100, 20, 100000.00, 0.00),
(27, 100, 100, 0.00, 100000.00),
(28, 101, 100, 10000.00, 0.00),
(29, 101, 20, 0.00, 10000.00),
(30, 102, 27, 30000.00, 0.00),
(31, 102, 12, 0.00, 30000.00),
(32, 102, 14, 20000.00, 0.00),
(33, 102, 20, 0.00, 20000.00),
(34, 103, 12, 3000.00, 0.00),
(35, 103, 27, 0.00, 3000.00),
(36, 103, 20, 2000.00, 0.00),
(37, 103, 14, 0.00, 2000.00),
(38, 104, 29, 0.00, 1200.00),
(39, 104, 15, 100.00, 0.00),
(40, 104, 27, 1200.00, 0.00),
(41, 104, 20, 0.00, 100.00),
(42, 105, 26, 0.00, 60000.00),
(43, 105, 20, 60000.00, 0.00),
(44, 106, 20, 0.00, 170.00),
(45, 106, 15, 170.00, 0.00),
(46, 107, 29, 0.00, 1400.00),
(47, 107, 15, 40.00, 0.00),
(48, 107, 27, 1400.00, 0.00),
(49, 107, 20, 0.00, 40.00),
(50, 108, 27, 0.00, 2400.00),
(51, 108, 20, 2400.00, 0.00),
(52, 109, 21, 1200.00, 0.00),
(53, 109, 25, 0.00, 1200.00),
(54, 110, 29, 0.00, 1400.00),
(55, 110, 15, 40.00, 0.00),
(56, 110, 27, 1400.00, 0.00),
(57, 110, 20, 0.00, 40.00),
(58, 111, 27, 0.00, 14400.00),
(59, 111, 20, 14400.00, 0.00),
(60, 112, 21, 0.00, 2000.00),
(61, 112, 25, 2000.00, 0.00),
(62, 113, 27, 1400.00, 0.00),
(63, 113, 15, 225.00, 0.00),
(64, 113, 12, 0.00, 1400.00),
(65, 113, 20, 0.00, 225.00);

-- --------------------------------------------------------

--
-- بنية الجدول `journalentries`
--

CREATE TABLE `journalentries` (
  `JournalID` int(11) NOT NULL,
  `BranchID` int(11) NOT NULL,
  `JournalDate` datetime NOT NULL DEFAULT current_timestamp(),
  `Description` varchar(500) DEFAULT NULL,
  `ReferenceType` varchar(50) DEFAULT NULL COMMENT 'Receipt, Payment, Sale, Purchase',
  `IsPosted` tinyint(1) NOT NULL DEFAULT 0,
  `CreatedBy` int(11) NOT NULL,
  `ReferenceNo` varchar(100) DEFAULT NULL,
  `PayeePayerName` varchar(200) DEFAULT NULL,
  `IsDeleted` tinyint(1) DEFAULT 0,
  `UpdatedAt` datetime DEFAULT NULL,
  `UpdatedBy` int(11) DEFAULT NULL,
  `DeletedAt` datetime DEFAULT NULL,
  `DeletedBy` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `journalentries`
--

INSERT INTO `journalentries` (`JournalID`, `BranchID`, `JournalDate`, `Description`, `ReferenceType`, `IsPosted`, `CreatedBy`, `ReferenceNo`, `PayeePayerName`, `IsDeleted`, `UpdatedAt`, `UpdatedBy`, `DeletedAt`, `DeletedBy`) VALUES
(1, 1, '2026-03-16 22:47:45', 'اثبات راس المال الافتتاحي ', 'Manual', 1, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
(2, 1, '2026-03-16 23:43:25', 'شراء نقدي #75563 - الشرق الاوسط', 'Purchase Invoice', 1, 1, '75563', NULL, NULL, NULL, NULL, NULL, NULL),
(3, 1, '2026-03-17 23:36:37', 'شراء نقدي #75563 - الشرق الاوسط (متضمنة بونص)', 'Purchase Invoice', 1, 1, '75563', NULL, NULL, NULL, NULL, NULL, NULL),
(5, 1, '2026-03-18 23:22:05', 'فاتورة مشتريات #987665 - الشرق الاوسط ', 'Purchase Invoice', 1, 1, '987665', NULL, NULL, NULL, NULL, NULL, NULL),
(6, 1, '2026-03-21 00:04:24', 'مبيعات POS فاتورة #2', 'SalesInvoice', 1, 1, '2', NULL, NULL, NULL, NULL, NULL, NULL),
(7, 1, '2026-03-23 16:42:31', 'فاتورة مشتريات #45666 - المورد: الشرق الاوسط', 'PurchaseInvoice', 1, 1, '45666', NULL, NULL, NULL, NULL, NULL, NULL),
(9, 1, '2026-03-23 22:10:27', 'مبيعات POS فاتورة #3', 'SalesInvoice', 1, 1, '3', NULL, NULL, NULL, NULL, NULL, NULL),
(100, 1, '2026-03-30 01:28:18', 'إثبات فاتورة مشتريات آجل رقم INV-BUY-100', 'Purchase Invoice', 1, 1, 'INV-BUY-100', NULL, 0, NULL, NULL, NULL, NULL),
(101, 1, '2026-03-30 01:28:18', 'إثبات مرتجع مشتريات للمورد', 'Purchase Return', 1, 1, 'RET-BUY-100', NULL, 0, NULL, NULL, NULL, NULL),
(102, 1, '2026-03-30 01:28:18', 'إثبات فاتورة مبيعات نقدية رقم 100 وتكلفتها', 'SalesInvoice', 1, 1, '100', NULL, 0, NULL, NULL, NULL, NULL),
(103, 1, '2026-03-30 01:28:18', 'إثبات مرتجع مبيعات نقدي للفاتورة 100', 'SalesReturn', 1, 1, '101', NULL, 0, NULL, NULL, NULL, NULL),
(104, 1, '2026-03-31 03:52:04', 'مبيعات POS فاتورة #102', 'SalesInvoice', 1, 1, '102', NULL, NULL, NULL, NULL, NULL, NULL),
(105, 1, '2026-03-31 21:37:10', 'فاتورة مشتريات #121212121 - المورد: الشرق الاوسط', 'PurchaseInvoice', 1, 1, '121212121', NULL, NULL, NULL, NULL, NULL, NULL),
(106, 1, '2026-03-31 21:40:06', 'مبيعات POS فاتورة #103', 'SalesInvoice', 1, 1, '103', NULL, NULL, NULL, NULL, NULL, NULL),
(107, 1, '2026-04-01 07:12:11', 'مبيعات POS فاتورة #104', 'SalesInvoice', 1, 1, '104', NULL, NULL, NULL, NULL, NULL, NULL),
(108, 1, '2026-04-01 08:17:13', 'فاتورة مشتريات #1234454 - المورد: شركة ابن سينا', 'PurchaseInvoice', 1, 1, '1234454', NULL, NULL, NULL, NULL, NULL, NULL),
(109, 1, '2026-04-03 00:00:00', 'قبض من: احمد محمد  - من ', 'Receipt', 1, 1, NULL, 'احمد محمد ', NULL, NULL, NULL, NULL, NULL),
(110, 1, '2026-04-05 00:07:19', 'مبيعات POS فاتورة #105', 'SalesInvoice', 1, 1, '105', NULL, NULL, NULL, NULL, NULL, NULL),
(111, 1, '2026-04-05 00:31:59', 'فاتورة مشتريات #Inty-oiy5 - المورد: الشرق الاوسط', 'PurchaseInvoice', 1, 1, 'Inty-oiy5', NULL, NULL, NULL, NULL, NULL, NULL),
(112, 1, '2026-04-05 00:00:00', 'صرف لـ: احمد محمد  - كهرباء', 'Payment', 1, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
(113, 1, '2026-04-08 00:56:43', 'مبيعات POS فاتورة #106', 'SalesInvoice', 1, 1, '106', NULL, NULL, NULL, NULL, NULL, NULL);

-- --------------------------------------------------------

--
-- بنية الجدول `legacy_shelves_backup`
--

CREATE TABLE `legacy_shelves_backup` (
  `ShelfId` int(11) NOT NULL,
  `WarehouseId` int(11) NOT NULL,
  `GroupId` int(11) DEFAULT NULL COMMENT 'المجموعة الدوائية المخصصة لهذا الرف',
  `ShelfCode` varchar(50) NOT NULL COMMENT 'كود الرف مثل A1, B2',
  `Description` varchar(200) DEFAULT NULL,
  `IsActive` tinyint(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- بنية الجدول `legacy_warehouses_backup`
--

CREATE TABLE `legacy_warehouses_backup` (
  `WarehouseId` int(11) NOT NULL,
  `BranchId` int(11) NOT NULL,
  `WarehouseName` varchar(100) NOT NULL,
  `IsActive` tinyint(1) DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- بنية الجدول `purchasedetails`
--

CREATE TABLE `purchasedetails` (
  `DetailID` int(11) NOT NULL,
  `PurchaseID` int(11) NOT NULL,
  `DrugID` int(11) NOT NULL,
  `Quantity` int(11) NOT NULL,
  `RemainingQuantity` int(11) NOT NULL DEFAULT 0,
  `BonusQuantity` int(11) NOT NULL DEFAULT 0,
  `CostPrice` decimal(18,2) NOT NULL,
  `SellingPrice` decimal(18,2) NOT NULL DEFAULT 0.00,
  `BatchNumber` varchar(50) DEFAULT NULL,
  `ExpiryDate` date NOT NULL DEFAULT '2026-01-01',
  `SubTotal` decimal(18,2) NOT NULL DEFAULT 0.00
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `purchasedetails`
--

INSERT INTO `purchasedetails` (`DetailID`, `PurchaseID`, `DrugID`, `Quantity`, `RemainingQuantity`, `BonusQuantity`, `CostPrice`, `SellingPrice`, `BatchNumber`, `ExpiryDate`, `SubTotal`) VALUES
(1, 1, 1, 10, 10, 0, 1000.00, 1200.00, '123', '2029-07-15', 10000.00),
(2, 2, 7, 10, 11, 1, 250.00, 600.00, '12453', '2028-06-08', 2500.00),
(4, 3, 4, 5, 0, 0, 1000.00, 1400.00, '212', '2027-06-17', 5000.00),
(15, 10, 7, 3, 3, 0, 200.00, 300.00, '4444', '2027-03-31', 600.00),
(18, 11, 8, 100, 100, 0, 500.00, 600.00, 'B-AMX-01', '2028-01-01', 50000.00),
(19, 12, 9, 200, 200, 0, 150.00, 200.00, 'B-CVT-01', '2029-05-01', 30000.00),
(20, 13, 10, 100, 100, 0, 400.00, 500.00, 'B-CON-01', '2027-11-01', 40000.00),
(21, 14, 11, 250, 250, 0, 100.00, 150.00, 'B-GLU-01', '2028-06-01', 25000.00),
(22, 15, 12, 120, 120, 0, 500.00, 650.00, 'B-NEX-01', '2029-02-01', 60000.00),
(23, 100, 100, 100, 100, 0, 1000.00, 1500.00, 'BATCH-C001', '2028-12-31', 100000.00),
(24, 101, 100, 10, 0, 0, 1000.00, 0.00, NULL, '2026-01-01', 10000.00),
(25, 102, 1, 30, 32, 2, 2000.00, -0.01, '213', '2027-11-08', 60000.00),
(26, 103, 3, 6, 6, 0, 400.00, 600.00, '230', '2027-04-12', 2400.00),
(28, 104, 3, 10, 10, 0, 1200.00, 1400.00, '8598', '2028-02-01', 12000.00);

-- --------------------------------------------------------

--
-- بنية الجدول `purchaseplandetails`
--

CREATE TABLE `purchaseplandetails` (
  `DetailId` int(11) NOT NULL,
  `PlanId` int(11) NOT NULL,
  `DrugId` int(11) NOT NULL,
  `CurrentStock` int(11) NOT NULL,
  `ABCCategory` varchar(10) DEFAULT NULL,
  `ForecastedDemand` decimal(18,4) NOT NULL,
  `ForecastAccuracy` decimal(18,4) NOT NULL,
  `ProposedQuantity` int(11) NOT NULL,
  `ApprovedQuantity` int(11) NOT NULL,
  `UnitCostEstimate` decimal(18,4) NOT NULL,
  `TotalCost` decimal(18,4) NOT NULL,
  `IsLifeSaving` tinyint(1) NOT NULL,
  `Status` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- بنية الجدول `purchaseplans`
--

CREATE TABLE `purchaseplans` (
  `PlanId` int(11) NOT NULL,
  `BranchId` int(11) NOT NULL,
  `CreatedBy` int(11) NOT NULL,
  `PlanDate` datetime NOT NULL,
  `Status` varchar(50) DEFAULT NULL,
  `Notes` varchar(500) DEFAULT NULL,
  `EstimatedTotalCost` decimal(18,4) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- بنية الجدول `purchases`
--

CREATE TABLE `purchases` (
  `PurchaseID` int(11) NOT NULL,
  `InvoiceNumber` varchar(50) NOT NULL COMMENT 'رقم فاتورة المورد الخارجية',
  `SupplierId` int(11) NOT NULL,
  `BranchID` int(11) NOT NULL,
  `PurchaseDate` datetime NOT NULL DEFAULT current_timestamp(),
  `UserID` int(11) NOT NULL,
  `TotalAmount` decimal(18,2) NOT NULL DEFAULT 0.00,
  `Discount` decimal(18,2) NOT NULL DEFAULT 0.00,
  `TaxAmount` decimal(18,2) NOT NULL DEFAULT 0.00,
  `NetAmount` decimal(18,2) NOT NULL DEFAULT 0.00,
  `PaymentStatus` varchar(20) NOT NULL DEFAULT 'Unpaid',
  `Notes` text DEFAULT NULL,
  `CreatedAt` datetime DEFAULT current_timestamp(),
  `IsDeleted` tinyint(1) DEFAULT 0,
  `UpdatedAt` datetime DEFAULT NULL,
  `UpdatedBy` int(11) DEFAULT NULL,
  `DeletedAt` datetime DEFAULT NULL,
  `DeletedBy` int(11) DEFAULT NULL,
  `AmountPaid` decimal(18,2) NOT NULL DEFAULT 0.00 COMMENT 'المبلغ المدفوع فوراً',
  `RemainingAmount` decimal(18,2) NOT NULL DEFAULT 0.00 COMMENT 'المبلغ المتبقي كدين على المورد',
  `IsReturn` tinyint(1) NOT NULL DEFAULT 0,
  `ParentPurchaseId` int(11) DEFAULT NULL,
  `InvoiceImagePath` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `purchases`
--

INSERT INTO `purchases` (`PurchaseID`, `InvoiceNumber`, `SupplierId`, `BranchID`, `PurchaseDate`, `UserID`, `TotalAmount`, `Discount`, `TaxAmount`, `NetAmount`, `PaymentStatus`, `Notes`, `CreatedAt`, `IsDeleted`, `UpdatedAt`, `UpdatedBy`, `DeletedAt`, `DeletedBy`, `AmountPaid`, `RemainingAmount`, `IsReturn`, `ParentPurchaseId`, `InvoiceImagePath`) VALUES
(1, '75563', 1, 1, '2026-03-16 23:43:25', 1, 10000.00, 0.00, 0.00, 10000.00, 'Paid', NULL, '2026-03-16 23:43:25', NULL, NULL, NULL, NULL, NULL, 0.00, 0.00, 0, NULL, NULL),
(2, '75563', 1, 1, '2026-03-17 23:36:37', 1, 2500.00, 0.00, 0.00, 2500.00, 'Paid', NULL, '2026-03-17 23:36:36', NULL, NULL, NULL, NULL, NULL, 0.00, 0.00, 0, NULL, NULL),
(3, '987665', 1, 1, '2026-03-18 23:25:24', 1, 5000.00, 0.00, 0.00, 5000.00, 'Unpaid', NULL, '2026-03-18 23:22:05', NULL, NULL, NULL, NULL, NULL, 0.00, 5000.00, 0, NULL, NULL),
(10, '45666', 1, 1, '2026-03-23 16:42:30', 1, 600.00, 0.00, 0.00, 600.00, 'Paid', NULL, '2026-03-23 16:42:30', NULL, NULL, NULL, NULL, NULL, 600.00, 0.00, 0, NULL, NULL),
(11, 'INV-2026-001', 2, 1, '2026-03-30 01:19:57', 1, 50000.00, 0.00, 0.00, 50000.00, 'Unpaid', NULL, '2026-03-30 01:19:57', 0, NULL, NULL, NULL, NULL, 0.00, 50000.00, 0, NULL, NULL),
(12, 'INV-2026-002', 3, 1, '2026-03-30 01:19:57', 1, 30000.00, 0.00, 0.00, 30000.00, 'Paid', NULL, '2026-03-30 01:19:57', 0, NULL, NULL, NULL, NULL, 30000.00, 0.00, 0, NULL, NULL),
(13, 'INV-2026-003', 4, 1, '2026-03-30 01:19:57', 1, 40000.00, 0.00, 0.00, 40000.00, 'Unpaid', NULL, '2026-03-30 01:19:57', 0, NULL, NULL, NULL, NULL, 0.00, 40000.00, 0, NULL, NULL),
(14, 'INV-2026-004', 5, 1, '2026-03-30 01:19:57', 1, 25000.00, 0.00, 0.00, 25000.00, 'Unpaid', NULL, '2026-03-30 01:19:57', 0, NULL, NULL, NULL, NULL, 0.00, 25000.00, 0, NULL, NULL),
(15, 'INV-2026-005', 6, 1, '2026-03-30 01:19:57', 1, 60000.00, 0.00, 0.00, 60000.00, 'Paid', NULL, '2026-03-30 01:19:57', 0, NULL, NULL, NULL, NULL, 60000.00, 0.00, 0, NULL, NULL),
(100, 'INV-BUY-100', 100, 1, '2026-03-30 01:28:18', 1, 100000.00, 0.00, 0.00, 100000.00, 'Unpaid', NULL, '2026-03-30 01:28:18', 0, NULL, NULL, NULL, NULL, 0.00, 100000.00, 0, NULL, NULL),
(101, 'RET-BUY-100', 100, 1, '2026-03-30 01:28:18', 1, 10000.00, 0.00, 0.00, 10000.00, 'Unpaid', NULL, '2026-03-30 01:28:18', 0, NULL, NULL, NULL, NULL, 0.00, 0.00, 1, 100, NULL),
(102, '121212121', 1, 1, '2026-03-31 21:37:09', 1, 60000.00, 0.00, 0.00, 60000.00, 'Unpaid', NULL, '2026-03-31 21:37:09', NULL, NULL, NULL, NULL, NULL, 0.00, 60000.00, 0, NULL, NULL),
(103, '1234454', 2, 1, '2026-04-01 08:17:13', 1, 2400.00, 0.00, 0.00, 2400.00, 'Paid', NULL, '2026-04-01 08:17:13', NULL, NULL, NULL, NULL, NULL, 2400.00, 0.00, 0, NULL, NULL),
(104, 'Inty-oiy5', 1, 1, '2026-04-05 00:31:59', 1, 12000.00, 0.00, 0.00, 12000.00, 'Paid', NULL, '2026-04-05 00:31:59', NULL, '2026-04-05 00:42:47', 1, NULL, NULL, 12000.00, 0.00, 0, NULL, NULL);

-- --------------------------------------------------------

--
-- بنية الجدول `saledetails`
--

CREATE TABLE `saledetails` (
  `SaleDetailID` int(11) NOT NULL,
  `SaleID` int(11) NOT NULL,
  `DrugID` int(11) NOT NULL,
  `Quantity` int(11) NOT NULL,
  `UnitPrice` decimal(18,2) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `saledetails`
--

INSERT INTO `saledetails` (`SaleDetailID`, `SaleID`, `DrugID`, `Quantity`, `UnitPrice`) VALUES
(2, 2, 7, 2, 600.00),
(3, 3, 1, 10, 120.00),
(4, 100, 100, 20, 1500.00),
(5, 101, 100, 2, 1500.00),
(6, 102, 1, 10, 120.00),
(7, 103, 1, 10, 0.00),
(8, 104, 4, 25, 56.00),
(9, 105, 4, 25, 56.00),
(10, 106, 3, 4, 350.00);

-- --------------------------------------------------------

--
-- بنية الجدول `sales`
--

CREATE TABLE `sales` (
  `SaleID` int(11) NOT NULL,
  `BranchID` int(11) NOT NULL,
  `SaleDate` datetime NOT NULL DEFAULT current_timestamp(),
  `UserID` int(11) NOT NULL,
  `CustomerID` int(11) DEFAULT NULL,
  `TotalAmount` decimal(18,2) NOT NULL DEFAULT 0.00,
  `Discount` decimal(18,2) DEFAULT 0.00,
  `TaxAmount` decimal(18,2) DEFAULT 0.00,
  `NetAmount` decimal(18,2) DEFAULT 0.00,
  `IsReturn` tinyint(1) DEFAULT 0 COMMENT '0=Sale, 1=Return',
  `ParentSaleId` int(11) DEFAULT NULL COMMENT 'ID of original invoice if this is a return',
  `IsDeleted` tinyint(1) DEFAULT 0,
  `UpdatedAt` datetime DEFAULT NULL,
  `UpdatedBy` int(11) DEFAULT NULL,
  `DeletedAt` datetime DEFAULT NULL,
  `DeletedBy` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `sales`
--

INSERT INTO `sales` (`SaleID`, `BranchID`, `SaleDate`, `UserID`, `CustomerID`, `TotalAmount`, `Discount`, `TaxAmount`, `NetAmount`, `IsReturn`, `ParentSaleId`, `IsDeleted`, `UpdatedAt`, `UpdatedBy`, `DeletedAt`, `DeletedBy`) VALUES
(2, 1, '2026-03-21 00:04:24', 1, NULL, 1200.00, 0.00, 0.00, 1200.00, 0, NULL, NULL, NULL, NULL, NULL, NULL),
(3, 1, '2026-03-23 22:10:26', 1, NULL, 1200.00, 0.00, 0.00, 1200.00, 0, NULL, NULL, NULL, NULL, NULL, NULL),
(100, 1, '2026-03-30 01:28:18', 1, 100, 30000.00, 0.00, 0.00, 30000.00, 0, NULL, 0, NULL, NULL, NULL, NULL),
(101, 1, '2026-03-30 01:28:18', 1, 100, 3000.00, 0.00, 0.00, 3000.00, 1, 100, 0, NULL, NULL, NULL, NULL),
(102, 1, '2026-03-31 03:52:03', 1, NULL, 1200.00, 0.00, 0.00, 1200.00, 0, NULL, NULL, NULL, NULL, NULL, NULL),
(103, 1, '2026-03-31 21:40:05', 1, 100, -0.01, 0.00, 0.00, -0.01, 0, NULL, NULL, NULL, NULL, NULL, NULL),
(104, 1, '2026-04-01 07:12:11', 1, NULL, 1400.00, 0.00, 0.00, 1400.00, 0, NULL, NULL, NULL, NULL, NULL, NULL),
(105, 1, '2026-04-05 00:07:19', 1, NULL, 1400.00, 0.00, 0.00, 1400.00, 0, NULL, NULL, NULL, NULL, NULL, NULL),
(106, 1, '2026-04-08 00:56:43', 1, NULL, 1400.00, 0.00, 0.00, 1400.00, 0, NULL, NULL, NULL, NULL, NULL, NULL);

-- --------------------------------------------------------

--
-- بنية الجدول `sale_payments`
--

CREATE TABLE `sale_payments` (
  `PaymentId` int(11) NOT NULL,
  `SaleId` int(11) NOT NULL,
  `PaymentMethod` varchar(50) NOT NULL COMMENT 'Cash, Bank, Credit',
  `AccountId` int(11) DEFAULT NULL COMMENT 'حساب الصندوق أو البنك الذي استلم المبلغ',
  `Amount` decimal(18,4) NOT NULL DEFAULT 0.0000
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `sale_payments`
--

INSERT INTO `sale_payments` (`PaymentId`, `SaleId`, `PaymentMethod`, `AccountId`, `Amount`) VALUES
(2, 2, 'Cash', 27, 1200.0000),
(3, 3, 'Cash', 27, 1200.0000),
(4, 100, 'Cash', 27, 30000.0000),
(5, 101, 'Cash', 27, -3000.0000),
(6, 102, 'Cash', 27, 1200.0000),
(7, 104, 'Cash', 27, 1400.0000),
(8, 105, 'Cash', 27, 1400.0000),
(9, 106, 'Cash', 27, 1400.0000);

-- --------------------------------------------------------

--
-- بنية الجدول `screenpermissions`
--

CREATE TABLE `screenpermissions` (
  `PermissionID` int(11) NOT NULL,
  `RoleID` int(11) NOT NULL,
  `ScreenID` int(11) NOT NULL,
  `CanView` tinyint(1) NOT NULL DEFAULT 0,
  `CanAdd` tinyint(1) NOT NULL DEFAULT 0,
  `CanEdit` tinyint(1) NOT NULL DEFAULT 0,
  `CanDelete` tinyint(1) NOT NULL DEFAULT 0,
  `CanPrint` tinyint(1) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `screenpermissions`
--

INSERT INTO `screenpermissions` (`PermissionID`, `RoleID`, `ScreenID`, `CanView`, `CanAdd`, `CanEdit`, `CanDelete`, `CanPrint`) VALUES
(69, 6, 7, 1, 1, 1, 1, 1),
(70, 6, 10, 1, 1, 1, 1, 1),
(71, 6, 15, 1, 1, 1, 1, 1),
(72, 6, 4, 1, 1, 1, 1, 1),
(73, 6, 18, 1, 1, 1, 1, 1),
(74, 6, 1, 1, 1, 1, 1, 1),
(75, 6, 11, 1, 1, 1, 1, 1),
(76, 6, 19, 1, 1, 1, 1, 1),
(77, 6, 8, 1, 1, 1, 1, 1),
(78, 6, 17, 1, 1, 1, 1, 1),
(79, 6, 21, 1, 1, 1, 1, 1),
(80, 6, 20, 1, 1, 1, 1, 1),
(81, 6, 5, 1, 1, 1, 1, 1),
(82, 6, 13, 1, 1, 1, 1, 1),
(83, 6, 3, 1, 1, 1, 1, 1),
(84, 6, 2, 1, 1, 1, 1, 1),
(85, 6, 22, 1, 1, 1, 1, 1),
(86, 6, 6, 1, 1, 1, 1, 1),
(87, 6, 14, 1, 1, 1, 1, 1),
(88, 6, 9, 1, 1, 1, 1, 1),
(89, 6, 12, 1, 1, 1, 1, 1),
(90, 6, 16, 1, 1, 1, 1, 1),
(145, 3, 25, 0, 0, 0, 0, 0),
(146, 3, 26, 0, 0, 0, 0, 0),
(147, 3, 24, 0, 0, 0, 0, 0),
(148, 3, 21, 1, 1, 1, 0, 0),
(149, 3, 2, 1, 1, 1, 1, 0),
(150, 3, 3, 1, 1, 1, 0, 0),
(151, 3, 4, 1, 1, 1, 0, 0),
(152, 3, 27, 0, 0, 0, 0, 0),
(153, 3, 5, 1, 1, 1, 0, 0),
(154, 3, 6, 1, 1, 1, 0, 0),
(155, 3, 7, 1, 1, 1, 0, 0),
(156, 3, 8, 1, 1, 1, 0, 0),
(157, 3, 9, 1, 1, 1, 0, 0),
(158, 3, 10, 1, 1, 1, 0, 0),
(159, 3, 22, 1, 1, 1, 0, 0),
(160, 3, 16, 1, 1, 1, 0, 0),
(161, 3, 11, 1, 1, 1, 0, 0),
(162, 3, 12, 1, 1, 1, 0, 0),
(163, 3, 13, 1, 1, 1, 0, 0),
(164, 3, 14, 1, 1, 1, 0, 0),
(165, 3, 15, 1, 1, 1, 0, 0),
(166, 3, 29, 0, 0, 0, 0, 0),
(167, 3, 17, 1, 1, 1, 0, 0),
(168, 3, 18, 1, 1, 1, 0, 0),
(169, 3, 19, 1, 1, 1, 0, 0),
(170, 3, 20, 1, 1, 1, 0, 0),
(171, 3, 28, 0, 0, 0, 0, 0),
(172, 3, 1, 1, 1, 1, 1, 0),
(263, 3, 35, 1, 0, 0, 0, 0),
(267, 3, 35, 1, 0, 0, 0, 0),
(275, 4, 10, 1, 0, 0, 0, 1),
(276, 5, 10, 1, 0, 0, 0, 1),
(278, 4, 2, 1, 0, 0, 0, 1),
(279, 5, 2, 1, 0, 0, 0, 1),
(343, 1, 7, 1, 1, 1, 1, 1),
(344, 1, 10, 1, 1, 1, 1, 1),
(345, 1, 28, 1, 1, 1, 1, 1),
(346, 1, 37, 1, 1, 1, 1, 1),
(347, 1, 35, 1, 1, 1, 1, 1),
(348, 1, 15, 1, 1, 1, 1, 1),
(349, 1, 53, 1, 1, 1, 1, 1),
(350, 1, 33, 1, 1, 1, 1, 1),
(351, 1, 4, 1, 1, 1, 1, 1),
(352, 1, 18, 1, 1, 1, 1, 1),
(353, 1, 1, 1, 1, 1, 1, 1),
(354, 1, 43, 1, 1, 1, 1, 1),
(355, 1, 11, 1, 1, 1, 1, 1),
(356, 1, 29, 1, 1, 1, 1, 1),
(357, 1, 42, 1, 1, 1, 1, 1),
(358, 1, 19, 1, 1, 1, 1, 1),
(359, 1, 24, 1, 1, 1, 1, 1),
(360, 1, 25, 1, 1, 1, 1, 1),
(361, 1, 51, 1, 1, 1, 1, 1),
(362, 1, 40, 1, 1, 1, 1, 1),
(363, 1, 8, 1, 1, 1, 1, 1),
(364, 1, 17, 1, 1, 1, 1, 1),
(365, 1, 21, 1, 1, 1, 1, 1),
(366, 1, 55, 1, 1, 1, 1, 1),
(367, 1, 20, 1, 1, 1, 1, 1),
(368, 1, 5, 1, 1, 1, 1, 1),
(369, 1, 56, 1, 1, 1, 1, 1),
(370, 1, 41, 1, 1, 1, 1, 1),
(371, 1, 52, 1, 1, 1, 1, 1),
(372, 1, 13, 1, 1, 1, 1, 1),
(373, 1, 3, 1, 1, 1, 1, 1),
(374, 1, 27, 1, 1, 1, 1, 1),
(375, 1, 54, 1, 1, 1, 1, 1),
(376, 1, 50, 1, 1, 1, 1, 1),
(377, 1, 2, 1, 1, 1, 1, 1),
(378, 1, 26, 1, 1, 1, 1, 1),
(379, 1, 22, 1, 1, 1, 1, 1),
(380, 1, 6, 1, 1, 1, 1, 1),
(381, 1, 14, 1, 1, 1, 1, 1),
(382, 1, 9, 1, 1, 1, 1, 1),
(383, 1, 12, 1, 1, 1, 1, 1),
(384, 1, 16, 1, 1, 1, 1, 1),
(385, 1, 34, 1, 1, 1, 1, 1);

-- --------------------------------------------------------

--
-- بنية الجدول `seasonaldata`
--

CREATE TABLE `seasonaldata` (
  `SeasonalID` int(11) NOT NULL,
  `BranchID` int(11) NOT NULL,
  `DrugID` int(11) NOT NULL,
  `SeasonName` varchar(50) NOT NULL,
  `Year` int(11) NOT NULL,
  `SeasonalFactor` decimal(5,2) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- بنية الجدول `shelves`
--

CREATE TABLE `shelves` (
  `ShelfId` int(11) NOT NULL,
  `WarehouseId` int(11) NOT NULL,
  `GroupId` int(11) DEFAULT NULL COMMENT 'المجموعة العلاجية المخصصة',
  `ShelfName` varchar(100) NOT NULL COMMENT 'اسم الرف',
  `Notes` varchar(255) DEFAULT NULL COMMENT 'ملاحظات',
  `IsActive` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- إرجاع أو استيراد بيانات الجدول `shelves`
--

INSERT INTO `shelves` (`ShelfId`, `WarehouseId`, `GroupId`, `ShelfName`, `Notes`, `IsActive`) VALUES
(1, 1, 1, 'A001', 'البوابة رقم 1 على اليمين ', 1);

-- --------------------------------------------------------

--
-- بنية الجدول `stockauditdetails`
--

CREATE TABLE `stockauditdetails` (
  `DetailID` int(11) NOT NULL,
  `AuditID` int(11) NOT NULL,
  `DrugID` int(11) NOT NULL,
  `SystemQty` int(11) NOT NULL,
  `PhysicalQty` int(11) NOT NULL,
  `Difference` int(11) NOT NULL,
  `UnitCost` decimal(18,2) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- بنية الجدول `stockaudits`
--

CREATE TABLE `stockaudits` (
  `AuditID` int(11) NOT NULL,
  `BranchID` int(11) NOT NULL,
  `AuditDate` datetime NOT NULL DEFAULT current_timestamp(),
  `UserID` int(11) NOT NULL,
  `Notes` varchar(500) DEFAULT NULL,
  `Status` varchar(20) DEFAULT 'Completed'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `stockaudits`
--

INSERT INTO `stockaudits` (`AuditID`, `BranchID`, `AuditDate`, `UserID`, `Notes`, `Status`) VALUES
(1, 1, '2026-03-30 20:29:22', 1, NULL, 'Completed'),
(2, 1, '2026-04-04 11:07:57', 1, NULL, 'Completed');

-- --------------------------------------------------------

--
-- بنية الجدول `stockmovements`
--

CREATE TABLE `stockmovements` (
  `MovementID` int(11) NOT NULL,
  `BranchID` int(11) NOT NULL,
  `DrugID` int(11) NOT NULL,
  `MovementDate` datetime NOT NULL DEFAULT current_timestamp(),
  `MovementType` varchar(50) NOT NULL,
  `Quantity` int(11) NOT NULL,
  `ReferenceID` int(11) DEFAULT NULL,
  `UserID` int(11) NOT NULL,
  `Notes` varchar(250) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `stockmovements`
--

INSERT INTO `stockmovements` (`MovementID`, `BranchID`, `DrugID`, `MovementDate`, `MovementType`, `Quantity`, `ReferenceID`, `UserID`, `Notes`) VALUES
(1, 1, 1, '2026-03-16 23:43:25', 'Purchase In', 100, 1, 1, 'توريد بضاعة - فاتورة رقم 75563'),
(2, 1, 7, '2026-03-17 23:36:37', 'Purchase In', 33, 2, 1, 'توريد بضاعة - فاتورة رقم 75563'),
(4, 1, 4, '2026-03-18 23:22:06', 'Purchase In', 125, 3, 1, 'توريد - فاتورة 987665'),
(5, 1, 7, '2026-03-21 00:04:24', 'Sale Out', -2, NULL, 1, 'مبيعات POS'),
(7, 1, 7, '2026-03-23 16:42:31', 'Purchase In', 9, 10, 1, 'توريد - فاتورة 45666'),
(10, 1, 1, '2026-03-23 22:10:27', 'Sale Out', -10, NULL, 1, 'مبيعات POS'),
(11, 1, 100, '2026-03-30 01:28:18', 'Purchase In', 100, 100, 1, 'توريد مشتريات فاتورة INV-BUY-100'),
(12, 1, 100, '2026-03-30 01:28:18', 'Purchase Return Out', -10, 101, 1, 'إرجاع للمورد لفاتورة RET-BUY-100'),
(13, 1, 100, '2026-03-30 01:28:18', 'Sale Out', -20, 100, 1, 'مبيعات فاتورة رقم 100'),
(14, 1, 100, '2026-03-30 01:28:18', 'Sale Return In', 2, 101, 1, 'مرتجع مبيعات للفاتورة 101'),
(15, 1, 1, '2026-03-31 03:52:04', 'Sale Out', -10, NULL, 1, 'مبيعات POS'),
(16, 1, 1, '2026-03-31 21:37:10', 'Purchase In', 320, 102, 1, 'توريد - فاتورة 121212121'),
(17, 1, 1, '2026-03-31 21:40:05', 'Sale Out', -10, NULL, 1, 'مبيعات POS'),
(18, 1, 4, '2026-04-01 07:12:11', 'Sale Out', -25, NULL, 1, 'مبيعات POS'),
(19, 1, 3, '2026-04-01 08:17:13', 'Purchase In', 24, 103, 1, 'توريد - فاتورة 1234454'),
(20, 1, 4, '2026-04-05 00:07:19', 'Sale Out', -25, NULL, 1, 'مبيعات POS'),
(21, 1, 3, '2026-04-05 00:31:59', 'Purchase In', 48, 104, 1, 'توريد - فاتورة Inty-oiy5'),
(22, 1, 3, '2026-04-08 00:56:43', 'Sale Out', -4, NULL, 1, 'مبيعات POS');

-- --------------------------------------------------------

--
-- بنية الجدول `suppliers`
--

CREATE TABLE `suppliers` (
  `SupplierID` int(11) NOT NULL,
  `BranchId` int(11) NOT NULL DEFAULT 1,
  `SupplierName` varchar(150) NOT NULL,
  `ContactPerson` varchar(100) DEFAULT NULL,
  `Phone` varchar(20) DEFAULT NULL,
  `Address` text DEFAULT NULL,
  `AccountID` int(11) NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  `CreatedAt` datetime DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `suppliers`
--

INSERT INTO `suppliers` (`SupplierID`, `BranchId`, `SupplierName`, `ContactPerson`, `Phone`, `Address`, `AccountID`, `IsActive`, `CreatedAt`) VALUES
(1, 1, 'الشرق الاوسط', 'القطريفي 2', '773240500', 'ذمار', 26, 1, '2026-03-15 05:16:11'),
(2, 1, 'شركة ابن سينا', 'محمد أحمد', '771000001', NULL, 30, 1, '2026-03-30 01:19:57'),
(3, 1, 'شركة الرازي', 'خالد علي', '771000002', NULL, 31, 1, '2026-03-30 01:19:57'),
(4, 1, 'مؤسسة الأدوية', 'صالح يحيى', '771000003', NULL, 32, 1, '2026-03-30 01:19:57'),
(5, 1, 'فارماكير', 'عبدالله حسين', '771000004', NULL, 33, 1, '2026-03-30 01:19:57'),
(6, 1, 'جلوبال ميد', 'ياسر محمود', '771000005', NULL, 34, 1, '2026-03-30 01:19:57'),
(100, 1, 'شركة باير الطبية', NULL, '770000100', NULL, 100, 1, '2026-03-30 01:28:18');

-- --------------------------------------------------------

--
-- بنية الجدول `systemlogs`
--

CREATE TABLE `systemlogs` (
  `LogId` int(11) NOT NULL,
  `UserId` int(11) NOT NULL,
  `Action` varchar(50) NOT NULL,
  `ScreenName` varchar(100) DEFAULT NULL,
  `Details` text DEFAULT NULL,
  `CreatedAt` datetime DEFAULT current_timestamp(),
  `IPAddress` varchar(45) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- إرجاع أو استيراد بيانات الجدول `systemlogs`
--

INSERT INTO `systemlogs` (`LogId`, `UserId`, `Action`, `ScreenName`, `Details`, `CreatedAt`, `IPAddress`) VALUES
(1, 1, 'Add', 'JournalEntries', '[فرع 1] - تم تسجيل قيد محاسبي مُرحل رقم 1 بقيمة 100000.00 لفرع 1', '2026-03-16 22:47:45', '::1'),
(2, 1, 'Add', 'Purchases', '[فرع 1] - تسجيل فاتورة توريد للفاتورة 75563', '2026-03-16 23:43:26', '::1'),
(3, 1, 'Add', 'Drugs', '[فرع 1] - إضافة دواء جديد للدليل المركزي: امول ', '2026-03-17 04:46:54', '::1'),
(5, 1, 'Add', 'Warehouses', '[فرع 1] - إنشاء مستودع جديد: المعرض الرئسي', '2026-03-17 22:16:27', '::1'),
(6, 1, 'Add', 'Drugs', '[فرع 1] - إضافة دواء جديد: سولبادين بكود A-000-0001', '2026-03-17 22:33:58', '::1'),
(7, 1, 'Add', 'Drugs', '[فرع 1] - إضافة دواء جديد: فلازول بكود A-000-0002', '2026-03-17 23:00:32', '::1'),
(8, 1, 'Add', 'Drugs', '[فرع 1] - إضافة دواء جديد: سولبافيز بكود A-000-0003', '2026-03-17 23:09:51', '::1'),
(9, 1, 'Add', 'Drugs', '[فرع 1] - إضافة دواء جديد: داينكسيت بكود A-000-0004', '2026-03-17 23:36:26', '::1'),
(10, 1, 'Add', 'Purchases', '[فرع 1] - تسجيل فاتورة توريد للفاتورة 75563', '2026-03-17 23:36:37', '::1'),
(12, 1, 'Add', 'Purchases', '[فرع 1] - فاتورة توريد #987665 (مدفوع: 0)', '2026-03-18 23:22:06', '::1'),
(13, 1, 'Add', 'Accounting', '[فرع 1] - تم إنشاء حساب جديد: الفرع  الرئسي صندوق رقم 1 بكود (113)', '2026-03-18 23:33:32', '::1'),
(14, 1, 'Add', 'Accounting', '[فرع 1] - تم إنشاء حساب جديد: ايراد مشتريات  بكود (43)', '2026-03-18 23:35:34', '::1'),
(15, 1, 'Add', 'Accounting', '[فرع 1] - تم إنشاء حساب جديد: ايرد مبيعات  بكود (44)', '2026-03-18 23:36:43', '::1'),
(16, 1, 'Update', 'Settings', '[فرع 1] - تحديث خريطة التوجيه المحاسبي الديناميكي للفرع: الفرع الرئيسي', '2026-03-19 01:14:20', '::1'),
(17, 1, 'Update', 'Settings', '[فرع 1] - تحديث خريطة التوجيه المحاسبي الديناميكي للفرع: الفرع الرئيسي', '2026-03-19 01:14:47', '::1'),
(21, 1, 'Update', 'Settings', '[فرع 1] - تحديث خريطة التوجيه المحاسبي الديناميكي للفرع: الفرع الرئيسي', '2026-03-21 00:00:54', '::1'),
(22, 1, 'Add', 'Sales', '[فرع 1] - إصدار فاتورة مبيعات POS رقم 2', '2026-03-21 00:04:25', '::1'),
(24, 1, 'Add', 'Purchases', '[فرع 1] - فاتورة توريد #45666 (مدفوع: 600)', '2026-03-23 16:42:31', '::1'),
(25, 1, 'Add', 'Sales', '[فرع 1] - إصدار فاتورة مبيعات POS رقم 3', '2026-03-23 22:10:27', '::1'),
(26, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-03-23 23:57:25', '::1'),
(30, 1, 'Add', 'Drugs', '[فرع 1] - إضافة دواء جديد: تااالال بكود G-100-0002', '2026-03-30 01:36:32', '::1'),
(34, 1, 'Add', 'Inventory', '[فرع 1] - إتمام عملية جرد مخزني رقم #1 للفرع 1. صافي التسوية: 0', '2026-03-30 20:29:22', '::1'),
(39, 1, 'Backup', 'Admin', '[فرع 1] - تم إنشاء نسخة احتياطية من قاعدة البيانات (dblast) بنجاح. حجم الملف: 95 KB', '2026-03-31 01:51:46', '::1'),
(40, 1, 'Backup', 'Admin', '[فرع 1] - تم إنشاء نسخة احتياطية (بيانات فقط) من قاعدة البيانات (dblast) بنجاح. حجم: 50 KB', '2026-03-31 01:58:57', '::1'),
(41, 1, 'Restore', 'Admin', '[فرع 1] - تم استرجاع البيانات من الملف (PharmaSmart_Backup_20260331_015857.sql). عدد الأوامر المنفذة: 132', '2026-03-31 01:59:22', '::1'),
(43, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-03-31 03:40:58', '::1'),
(45, 1, 'Update', 'Settings', '[فرع 1] - تحديث خريطة التوجيه المحاسبي الديناميكي للفرع: الفرع الرئيسي', '2026-03-31 03:51:12', '::1'),
(46, 1, 'Add', 'Sales', '[فرع 1] - إصدار فاتورة مبيعات POS رقم 102', '2026-03-31 03:52:04', '::1'),
(47, 1, 'Backup', 'Admin', '[فرع 1] - تم إنشاء نسخة احتياطية (بيانات فقط) من قاعدة البيانات (dblast) بنجاح. حجم: 52 KB', '2026-03-31 05:50:51', '::1'),
(50, 1, 'Add', 'Purchases', '[فرع 1] - فاتورة توريد #121212121 (مدفوع: 0)', '2026-03-31 21:37:10', '::1'),
(51, 1, 'Add', 'Sales', '[فرع 1] - إصدار فاتورة مبيعات POS رقم 103', '2026-03-31 21:40:06', '::1'),
(52, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-01 07:08:24', '::1'),
(54, 1, 'Add', 'Sales', '[فرع 1] - إصدار فاتورة مبيعات POS رقم 104', '2026-04-01 07:12:11', '::1'),
(55, 1, 'Add', 'Purchases', '[فرع 1] - فاتورة توريد #1234454 (مدفوع: 2400)', '2026-04-01 08:17:14', '::1'),
(58, 1, 'Add', 'Users', '[فرع 1] - تم إنشاء حساب مستخدم جديد باسم: a0 وتعيينه للفرع 1', '2026-04-02 00:26:49', '::1'),
(59, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 00:26:53', '::1'),
(61, 8, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 01:04:15', '::1'),
(63, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 01:04:30', '::1'),
(65, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 01:04:56', '::1'),
(67, 8, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 01:33:33', '::1'),
(69, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 01:38:18', '::1'),
(71, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 01:43:24', '::1'),
(73, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 01:43:34', '::1'),
(75, 8, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 08:16:53', '::1'),
(76, 8, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 08:16:53', '::1'),
(78, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 10:03:06', '::1'),
(80, 8, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 10:28:21', '::1'),
(82, 8, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 10:57:08', '::1'),
(84, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 10:58:10', '::1'),
(86, 8, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 19:06:04', '::1'),
(88, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 19:07:50', '::1'),
(92, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 19:24:12', '::1'),
(94, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 19:54:32', '::1'),
(96, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-02 23:36:05', '::1'),
(98, 8, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-03 00:41:36', '::1'),
(100, 1, 'Add', 'Vouchers', '[فرع 1] - إنشاء سند قبض رقم #109 بمبلغ 1,200.00 من/إلى احمد محمد ', '2026-04-03 01:37:17', '::1'),
(101, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-03 05:13:00', '::1'),
(103, 8, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-03 05:23:11', '::1'),
(107, 1, 'Update', 'Settings', '[فرع 1] - تحديث خريطة التوجيه المحاسبي الديناميكي للفرع: الفرع الرئيسي', '2026-04-03 16:46:58', '::1'),
(110, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-04 01:36:10', '::1'),
(112, 8, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-04 02:14:42', '::1'),
(114, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-04 04:30:11', '::1'),
(117, 1, 'Add', 'Inventory', '[فرع 1] - إتمام عملية جرد مخزني رقم #2 للفرع 1. صافي التسوية: 0', '2026-04-04 11:07:57', '::1'),
(120, 8, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-04 23:38:43', '::1'),
(122, 1, 'Update', 'Settings', '[فرع 1] - تحديث خريطة التوجيه المحاسبي الديناميكي للفرع: الفرع الرئيسي', '2026-04-04 23:46:22', '::1'),
(123, 1, 'Add', 'Sales', '[فرع 1] - إصدار فاتورة مبيعات POS رقم 105', '2026-04-05 00:07:20', '::1'),
(124, 1, 'Add', 'Purchases', '[فرع 1] - فاتورة توريد #Inty-oiy5 (مدفوع: 14400)', '2026-04-05 00:31:59', '::1'),
(125, 1, 'Edit', 'Purchases', '[فرع 1] - تعديل فاتورة مشتريات #Inty-oiy5', '2026-04-05 00:42:47', '::1'),
(127, 1, 'Add', 'Accounting', '[فرع 1] - تم إنشاء حساب جديد: ابو يمان  بكود (212101)', '2026-04-05 01:08:38', '::1'),
(128, 1, 'Add', 'Vouchers', '[فرع 1] - إنشاء سند صرف رقم #112 بمبلغ 2,000.00 من/إلى احمد محمد ', '2026-04-05 01:16:38', '::1'),
(129, 1, 'Edit', 'Drugs', '[فرع 1] - تعديل بيانات الصنف (Master Data): بنادول', '2026-04-05 04:05:28', '::1'),
(130, 1, 'CreateSettings', 'Admin', '[فرع 1] - تم تهيئة إعدادات المؤسسة الافتراضية: تالين فارما', '2026-04-05 04:29:20', '::1'),
(131, 1, 'UpdateSettings', 'Admin', '[فرع 1] - تم تحديث إعدادات المؤسسة: تالين فارما', '2026-04-05 04:29:27', '::1'),
(132, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-05 05:34:20', '::1'),
(134, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-05 06:52:01', '::1'),
(136, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-05 06:57:18', '::1'),
(138, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-05 07:18:13', '::1'),
(140, 1, 'Update', 'Settings', '[فرع 1] - تحديث خريطة التوجيه المحاسبي الديناميكي للفرع: الفرع الرئيسي', '2026-04-05 10:54:44', '::1'),
(143, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-06 05:49:22', '::1'),
(145, 1, 'UpdateSettings', 'Admin', '[فرع 1] - تم تحديث إعدادات المؤسسة: تالين فارما', '2026-04-06 13:16:07', '::1'),
(146, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-06 13:33:20', '::1'),
(149, 1, 'Logout', 'Account', '[فرع 1] - قام المستخدم بتسجيل الخروج بنجاح.', '2026-04-08 00:54:36', '::1'),
(151, 1, 'Add', 'Sales', '[فرع 1] - إصدار فاتورة مبيعات POS رقم 106', '2026-04-08 00:56:43', '::1');

-- --------------------------------------------------------

--
-- بنية الجدول `systemscreens`
--

CREATE TABLE `systemscreens` (
  `ScreenID` int(11) NOT NULL,
  `ScreenName` varchar(100) NOT NULL,
  `ScreenArabicName` varchar(100) NOT NULL,
  `ScreenCategory` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `systemscreens`
--

INSERT INTO `systemscreens` (`ScreenID`, `ScreenName`, `ScreenArabicName`, `ScreenCategory`) VALUES
(1, 'Drugs', 'الأدوية والمخزون', 'الأدوية والمخزون'),
(2, 'ShortageForecast', 'التنبؤ الذكي بالنواقص', 'الأدوية والمخزون'),
(3, 'Sales', 'المبيعات ونقاط البيع', 'المبيعات والعملاء'),
(4, 'Customers', 'إدارة العملاء', 'المبيعات والعملاء'),
(5, 'Purchases', 'فواتير المشتريات', 'المشتريات والموردين'),
(6, 'Suppliers', 'إدارة الموردين', 'المشتريات والموردين'),
(7, 'Accounting', 'الدليل المحاسبي', 'المالية والحسابات'),
(8, 'JournalEntries', 'القيود اليومية والسندات', 'المالية والحسابات'),
(9, 'TrialBalance', 'ميزان المراجعة', 'المالية والحسابات'),
(10, 'AccountReports', 'مركز التقارير (دخل، سيولة، عمولات)', 'المالية والحسابات'),
(11, 'Employees', 'سجل الموظفين', 'الإدارة والرقابة'),
(12, 'Users', 'إدارة المستخدمين', 'الإدارة والرقابة'),
(13, 'Roles', 'مصفوفة الصلاحيات', 'الإدارة والرقابة'),
(14, 'SystemLogs', 'سجلات الرقابة', 'الإدارة والرقابة'),
(15, 'Branches', 'إدارة الفروع', 'الإدارة والرقابة'),
(16, 'Vouchers', 'سندات القبض والصرف', 'المالية والحسابات'),
(17, 'Ledger', 'كشف حساب تفصيلي', 'التقارير المالية'),
(18, 'DailyCashFlow', 'حركة الصندوق اليومية', 'التقارير المالية'),
(19, 'IncomeStatement', 'قائمة الدخل العالمية', 'التقارير المالية'),
(20, 'ProfitAndLoss', 'تقرير الأرباح والخسائر', 'التقارير المالية'),
(21, 'PharmacistSales', 'إنتاجية وعمولات الصيادلة', 'ذكاء الأعمال'),
(22, 'StockExpiry', 'رقابة صلاحية الأصناف', 'ذكاء الأعمال'),
(24, 'Inventory', 'جرد المخزون الداخلي', 'المخزون والأدوية'),
(25, 'Inventory.Shortages', 'نواقص الأدوية', 'المخزون والأدوية'),
(26, 'StockAudit', 'جرد وتسوية المخزون', 'المخزون والأدوية'),
(27, 'SalesReturn', 'مرتجع المبيعات', 'المبيعات والعملاء'),
(28, 'BankAccounts', 'الحسابات البنكية', 'المالية والحسابات'),
(29, 'FinancialSettings', 'الإعدادات المالية', 'الإدارة والرقابة'),
(33, 'Currencies', 'إدارة العملات وأسعار الصرف', 'المالية والحسابات'),
(34, 'Warehouses', 'المستودعات والرفوف', 'الأدوية والمخزون'),
(35, 'Batches', 'إدارة التشغيلات (Batches)', 'الأدوية والمخزون'),
(37, 'Barcode', 'توليد وطباعة الباركود', 'الأدوية والمخزون'),
(40, 'ItemGroups', 'المجموعات العلاجية', 'الأدوية والمخزون'),
(41, 'PurchasesReturn', 'مرتجع المشتريات', 'المشتريات والموردين'),
(42, 'FundTransfers', 'التحويلات المالية الداخلية', 'المالية والحسابات'),
(43, 'DrugTransfers', 'التحويلات المخزنية', 'المخزون'),
(50, 'Settings', 'إعدادات النظام والتوجيه المحاسبي', 'الإدارة والرقابة'),
(51, 'InventoryIntelligence', 'تحليل ABC والذكاء المخزني', 'ذكاء الأعمال'),
(52, 'ReportCenter', 'مركز التقارير الشامل', 'التقارير المالية'),
(53, 'BranchReports', 'تقارير الفروع المقارنة', 'التقارير المالية'),
(54, 'SecuritySettings', 'إعدادات الأمان والتشفير', 'الإدارة والرقابة'),
(55, 'POS', 'نقطة البيع السريعة', 'المبيعات والعملاء'),
(56, 'PurchasesInvoices', 'سجل فواتير المشتريات', 'المشتريات والموردين');

-- --------------------------------------------------------

--
-- بنية الجدول `units`
--

CREATE TABLE `units` (
  `UnitId` int(11) NOT NULL,
  `UnitName` varchar(50) NOT NULL,
  `ConversionFactor` decimal(10,2) DEFAULT 1.00
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `units`
--

INSERT INTO `units` (`UnitId`, `UnitName`, `ConversionFactor`) VALUES
(100, 'كرتون', 50.00),
(101, 'شريط', 10.00);

-- --------------------------------------------------------

--
-- بنية الجدول `userroles`
--

CREATE TABLE `userroles` (
  `RoleID` int(11) NOT NULL,
  `RoleName` varchar(50) NOT NULL,
  `RoleArabicName` varchar(100) DEFAULT NULL,
  `RoleDescription` varchar(200) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `userroles`
--

INSERT INTO `userroles` (`RoleID`, `RoleName`, `RoleArabicName`, `RoleDescription`, `IsActive`) VALUES
(1, 'SuperAdmin', 'المدير العام', 'هذا هو الحساب الرئيسي والوحيد للإدارة العليا، يمتلك كافة صلاحيات النظام.', 1),
(3, 'Pharmacist', 'صيدلاني مسؤول', 'صلاحيات إدارة المخزون، صرف الأدوية، والتقارير الطبية', 1),
(4, 'Cashier', 'كاشير مبيعات', 'صلاحية البيع المباشر (POS) ومرتجعات المبيعات فقط', 1),
(5, 'Accountant', 'محاسب مالي', 'الوصول للتقارير المالية، السندات، والميزانية', 1),
(6, 'Storekeeper', 'أمين مخزن', 'إدارة التوريدات، جرد المخزون، وحركات الأصناف', 1);

-- --------------------------------------------------------

--
-- بنية الجدول `users`
--

CREATE TABLE `users` (
  `UserID` int(11) NOT NULL,
  `Username` varchar(100) NOT NULL,
  `PasswordHash` varchar(200) NOT NULL,
  `RoleID` int(11) NOT NULL,
  `EmployeeID` int(11) DEFAULT NULL,
  `DefaultBranchID` int(11) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- إرجاع أو استيراد بيانات الجدول `users`
--

INSERT INTO `users` (`UserID`, `Username`, `PasswordHash`, `RoleID`, `EmployeeID`, `DefaultBranchID`, `IsActive`) VALUES
(1, 'admin', '1234', 1, NULL, 1, 1),
(3, 'ali', '123', 1, NULL, 1, 1),
(4, 'mohammed', '123', 4, 1, 1, 1),
(5, 'user3', '123', 1, NULL, 1, 1),
(6, 'user2', '123', 1, 4, 2, 1),
(7, 'mm', '123', 3, 3, 2, 1),
(8, 'a0', 'a0', 3, NULL, 1, 1);

-- --------------------------------------------------------

--
-- بنية الجدول `vouchers`
--

CREATE TABLE `vouchers` (
  `VoucherID` int(11) NOT NULL,
  `BranchID` int(11) NOT NULL,
  `VoucherType` varchar(20) NOT NULL COMMENT 'Receipt (قبض), Payment (صرف)',
  `VoucherDate` datetime NOT NULL DEFAULT current_timestamp(),
  `Amount` decimal(18,2) NOT NULL,
  `FromAccountID` int(11) NOT NULL,
  `ToAccountID` int(11) NOT NULL,
  `Description` text DEFAULT NULL,
  `CreatedBy` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- بنية الجدول `warehouses`
--

CREATE TABLE `warehouses` (
  `WarehouseId` int(11) NOT NULL,
  `BranchId` int(11) NOT NULL,
  `WarehouseName` varchar(150) NOT NULL COMMENT 'اسم المستودع',
  `Location` varchar(255) DEFAULT NULL COMMENT 'موقع المستودع',
  `IsActive` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- إرجاع أو استيراد بيانات الجدول `warehouses`
--

INSERT INTO `warehouses` (`WarehouseId`, `BranchId`, `WarehouseName`, `Location`, `IsActive`) VALUES
(1, 1, 'المعرض الرئسي', 'مبنى الصيدلية', 1);

-- --------------------------------------------------------

--
-- بنية الجدول `__efmigrationshistory`
--

CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(95) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- إرجاع أو استيراد بيانات الجدول `__efmigrationshistory`
--

INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`) VALUES
('20260405011914_AddCompanySettings', '3.1.32'),
('20260406090521_SyncModelsWithDatabase', '3.1.32');

--
-- Indexes for dumped tables
--

--
-- Indexes for table `accountingtemplatelines`
--
ALTER TABLE `accountingtemplatelines`
  ADD PRIMARY KEY (`LineId`),
  ADD KEY `IX_AccountingTemplateLines_TemplateId` (`TemplateId`);

--
-- Indexes for table `accountingtemplates`
--
ALTER TABLE `accountingtemplates`
  ADD PRIMARY KEY (`TemplateId`);

--
-- Indexes for table `accountmappings`
--
ALTER TABLE `accountmappings`
  ADD PRIMARY KEY (`MappingId`),
  ADD KEY `IX_AccountMappings_AccountId` (`AccountId`);

--
-- Indexes for table `accounts`
--
ALTER TABLE `accounts`
  ADD PRIMARY KEY (`AccountID`),
  ADD UNIQUE KEY `AccountCode` (`AccountCode`),
  ADD KEY `FK_Accounts_Branches` (`BranchId`);

--
-- Indexes for table `barcodegenerator`
--
ALTER TABLE `barcodegenerator`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `fk_barcode_drug` (`DrugId`),
  ADD KEY `fk_barcode_branch` (`BranchId`);

--
-- Indexes for table `branches`
--
ALTER TABLE `branches`
  ADD PRIMARY KEY (`BranchID`),
  ADD UNIQUE KEY `BranchCode` (`BranchCode`),
  ADD KEY `fk_branch_cash` (`DefaultCashAccountId`),
  ADD KEY `fk_branch_sales` (`DefaultSalesAccountId`),
  ADD KEY `fk_branch_cogs` (`DefaultCOGSAccountId`),
  ADD KEY `fk_branch_inv` (`DefaultInventoryAccountId`),
  ADD KEY `fk_branch_currency` (`DefaultCurrencyId`);

--
-- Indexes for table `branchinventory`
--
ALTER TABLE `branchinventory`
  ADD PRIMARY KEY (`BranchID`,`DrugID`),
  ADD KEY `fk_branchinventory_drug` (`DrugID`),
  ADD KEY `fk_inventory_shelf` (`ShelfId`);

--
-- Indexes for table `companysettings`
--
ALTER TABLE `companysettings`
  ADD PRIMARY KEY (`Id`);

--
-- Indexes for table `currencies`
--
ALTER TABLE `currencies`
  ADD PRIMARY KEY (`CurrencyId`),
  ADD UNIQUE KEY `CurrencyCode_UNIQUE` (`CurrencyCode`);

--
-- Indexes for table `customers`
--
ALTER TABLE `customers`
  ADD PRIMARY KEY (`CustomerID`),
  ADD UNIQUE KEY `idx_unique_customer_name` (`FullName`),
  ADD KEY `AccountID` (`AccountID`),
  ADD KEY `fk_customers_branch` (`BranchId`);

--
-- Indexes for table `drugcategories`
--
ALTER TABLE `drugcategories`
  ADD PRIMARY KEY (`CategoryId`),
  ADD UNIQUE KEY `CategoryName_UNIQUE` (`CategoryName`);

--
-- Indexes for table `drugs`
--
ALTER TABLE `drugs`
  ADD PRIMARY KEY (`DrugID`),
  ADD UNIQUE KEY `idx_unique_barcode` (`Barcode`),
  ADD KEY `fk_drugs_category` (`CategoryId`),
  ADD KEY `fk_drugs_unit` (`UnitId`),
  ADD KEY `fk_drugs_itemgroup` (`GroupId`);

--
-- Indexes for table `drugtransferdetails`
--
ALTER TABLE `drugtransferdetails`
  ADD PRIMARY KEY (`DetailID`),
  ADD KEY `TransferID` (`TransferID`),
  ADD KEY `DrugID` (`DrugID`);

--
-- Indexes for table `drugtransfers`
--
ALTER TABLE `drugtransfers`
  ADD PRIMARY KEY (`TransferID`),
  ADD KEY `FromBranchID` (`FromBranchID`),
  ADD KEY `ToBranchID` (`ToBranchID`),
  ADD KEY `CreatedBy` (`CreatedBy`),
  ADD KEY `fk_dt_receivedby` (`ReceivedBy`),
  ADD KEY `fk_dt_journal` (`JournalId`),
  ADD KEY `fk_dt_receiptjournal` (`ReceiptJournalId`);

--
-- Indexes for table `drug_batches`
--
ALTER TABLE `drug_batches`
  ADD PRIMARY KEY (`BatchId`),
  ADD UNIQUE KEY `uq_drug_batch` (`DrugId`,`BatchNumber`);

--
-- Indexes for table `employees`
--
ALTER TABLE `employees`
  ADD PRIMARY KEY (`EmployeeID`),
  ADD KEY `BranchID` (`BranchID`);

--
-- Indexes for table `forecasts`
--
ALTER TABLE `forecasts`
  ADD PRIMARY KEY (`ForecastID`),
  ADD KEY `BranchID` (`BranchID`),
  ADD KEY `DrugID` (`DrugID`);

--
-- Indexes for table `fundtransfers`
--
ALTER TABLE `fundtransfers`
  ADD PRIMARY KEY (`TransferID`),
  ADD KEY `FromAccountID` (`FromAccountID`),
  ADD KEY `ToAccountID` (`ToAccountID`),
  ADD KEY `CreatedBy` (`CreatedBy`),
  ADD KEY `fk_fund_branch` (`BranchId`),
  ADD KEY `fk_fund_journal` (`JournalId`);

--
-- Indexes for table `itemgroups`
--
ALTER TABLE `itemgroups`
  ADD PRIMARY KEY (`GroupId`);

--
-- Indexes for table `journaldetails`
--
ALTER TABLE `journaldetails`
  ADD PRIMARY KEY (`DetailID`),
  ADD KEY `fk_journaldetails_journal` (`JournalID`),
  ADD KEY `idx_account_balancing` (`AccountID`,`Debit`,`Credit`);

--
-- Indexes for table `journalentries`
--
ALTER TABLE `journalentries`
  ADD PRIMARY KEY (`JournalID`),
  ADD KEY `CreatedBy` (`CreatedBy`),
  ADD KEY `idx_journal_date` (`JournalDate`),
  ADD KEY `fk_journalentries_branch` (`BranchID`);

--
-- Indexes for table `legacy_shelves_backup`
--
ALTER TABLE `legacy_shelves_backup`
  ADD PRIMARY KEY (`ShelfId`),
  ADD KEY `fk_shelf_warehouse` (`WarehouseId`),
  ADD KEY `fk_shelf_itemgroup` (`GroupId`);

--
-- Indexes for table `legacy_warehouses_backup`
--
ALTER TABLE `legacy_warehouses_backup`
  ADD PRIMARY KEY (`WarehouseId`),
  ADD KEY `fk_warehouse_branch` (`BranchId`);

--
-- Indexes for table `purchasedetails`
--
ALTER TABLE `purchasedetails`
  ADD PRIMARY KEY (`DetailID`),
  ADD KEY `idx_expiry_date` (`ExpiryDate`),
  ADD KEY `fk_purchasedetails_purchase` (`PurchaseID`),
  ADD KEY `fk_purchasedetails_drug` (`DrugID`);

--
-- Indexes for table `purchaseplandetails`
--
ALTER TABLE `purchaseplandetails`
  ADD PRIMARY KEY (`DetailId`),
  ADD KEY `IX_purchaseplandetails_DrugId` (`DrugId`),
  ADD KEY `IX_purchaseplandetails_PlanId` (`PlanId`);

--
-- Indexes for table `purchaseplans`
--
ALTER TABLE `purchaseplans`
  ADD PRIMARY KEY (`PlanId`),
  ADD KEY `IX_purchaseplans_BranchId` (`BranchId`),
  ADD KEY `IX_purchaseplans_CreatedBy` (`CreatedBy`);

--
-- Indexes for table `purchases`
--
ALTER TABLE `purchases`
  ADD PRIMARY KEY (`PurchaseID`),
  ADD KEY `UserID` (`UserID`),
  ADD KEY `fk_purchases_branch` (`BranchID`),
  ADD KEY `fk_purchases_supplier` (`SupplierId`),
  ADD KEY `fk_purchases_parent` (`ParentPurchaseId`);

--
-- Indexes for table `saledetails`
--
ALTER TABLE `saledetails`
  ADD PRIMARY KEY (`SaleDetailID`),
  ADD KEY `fk_saledetails_sale` (`SaleID`),
  ADD KEY `fk_saledetails_drug` (`DrugID`);

--
-- Indexes for table `sales`
--
ALTER TABLE `sales`
  ADD PRIMARY KEY (`SaleID`),
  ADD KEY `CustomerID` (`CustomerID`),
  ADD KEY `idx_sale_date` (`SaleDate`),
  ADD KEY `fk_sales_branch` (`BranchID`),
  ADD KEY `fk_sales_user` (`UserID`);

--
-- Indexes for table `sale_payments`
--
ALTER TABLE `sale_payments`
  ADD PRIMARY KEY (`PaymentId`),
  ADD KEY `fk_sale_payments_sale` (`SaleId`),
  ADD KEY `fk_sale_payments_account` (`AccountId`);

--
-- Indexes for table `screenpermissions`
--
ALTER TABLE `screenpermissions`
  ADD PRIMARY KEY (`PermissionID`),
  ADD KEY `RoleID` (`RoleID`),
  ADD KEY `ScreenID` (`ScreenID`);

--
-- Indexes for table `seasonaldata`
--
ALTER TABLE `seasonaldata`
  ADD PRIMARY KEY (`SeasonalID`),
  ADD KEY `BranchID` (`BranchID`),
  ADD KEY `DrugID` (`DrugID`);

--
-- Indexes for table `shelves`
--
ALTER TABLE `shelves`
  ADD PRIMARY KEY (`ShelfId`),
  ADD KEY `fk_shelf_warehouse_new` (`WarehouseId`),
  ADD KEY `fk_shelf_itemgroup_new` (`GroupId`);

--
-- Indexes for table `stockauditdetails`
--
ALTER TABLE `stockauditdetails`
  ADD PRIMARY KEY (`DetailID`),
  ADD KEY `fk_audit_details_main` (`AuditID`),
  ADD KEY `fk_audit_details_drug` (`DrugID`);

--
-- Indexes for table `stockaudits`
--
ALTER TABLE `stockaudits`
  ADD PRIMARY KEY (`AuditID`),
  ADD KEY `fk_audits_branch` (`BranchID`),
  ADD KEY `fk_audits_user` (`UserID`);

--
-- Indexes for table `stockmovements`
--
ALTER TABLE `stockmovements`
  ADD PRIMARY KEY (`MovementID`),
  ADD KEY `BranchID` (`BranchID`),
  ADD KEY `DrugID` (`DrugID`),
  ADD KEY `UserID` (`UserID`);

--
-- Indexes for table `suppliers`
--
ALTER TABLE `suppliers`
  ADD PRIMARY KEY (`SupplierID`),
  ADD UNIQUE KEY `idx_unique_supplier_name` (`SupplierName`),
  ADD KEY `AccountID` (`AccountID`),
  ADD KEY `fk_suppliers_branch` (`BranchId`);

--
-- Indexes for table `systemlogs`
--
ALTER TABLE `systemlogs`
  ADD PRIMARY KEY (`LogId`),
  ADD KEY `UserId` (`UserId`);

--
-- Indexes for table `systemscreens`
--
ALTER TABLE `systemscreens`
  ADD PRIMARY KEY (`ScreenID`),
  ADD UNIQUE KEY `ScreenName` (`ScreenName`);

--
-- Indexes for table `units`
--
ALTER TABLE `units`
  ADD PRIMARY KEY (`UnitId`),
  ADD UNIQUE KEY `UnitName_UNIQUE` (`UnitName`);

--
-- Indexes for table `userroles`
--
ALTER TABLE `userroles`
  ADD PRIMARY KEY (`RoleID`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`UserID`),
  ADD UNIQUE KEY `Username` (`Username`),
  ADD KEY `RoleID` (`RoleID`),
  ADD KEY `EmployeeID` (`EmployeeID`),
  ADD KEY `DefaultBranchID` (`DefaultBranchID`);

--
-- Indexes for table `vouchers`
--
ALTER TABLE `vouchers`
  ADD PRIMARY KEY (`VoucherID`),
  ADD KEY `BranchID` (`BranchID`),
  ADD KEY `CreatedBy` (`CreatedBy`);

--
-- Indexes for table `warehouses`
--
ALTER TABLE `warehouses`
  ADD PRIMARY KEY (`WarehouseId`),
  ADD KEY `fk_warehouse_branch_new` (`BranchId`);

--
-- Indexes for table `__efmigrationshistory`
--
ALTER TABLE `__efmigrationshistory`
  ADD PRIMARY KEY (`MigrationId`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `accountingtemplatelines`
--
ALTER TABLE `accountingtemplatelines`
  MODIFY `LineId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=26;

--
-- AUTO_INCREMENT for table `accountingtemplates`
--
ALTER TABLE `accountingtemplates`
  MODIFY `TemplateId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT for table `accountmappings`
--
ALTER TABLE `accountmappings`
  MODIFY `MappingId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT for table `accounts`
--
ALTER TABLE `accounts`
  MODIFY `AccountID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=103;

--
-- AUTO_INCREMENT for table `barcodegenerator`
--
ALTER TABLE `barcodegenerator`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=12;

--
-- AUTO_INCREMENT for table `branches`
--
ALTER TABLE `branches`
  MODIFY `BranchID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `companysettings`
--
ALTER TABLE `companysettings`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT for table `currencies`
--
ALTER TABLE `currencies`
  MODIFY `CurrencyId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT for table `customers`
--
ALTER TABLE `customers`
  MODIFY `CustomerID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=101;

--
-- AUTO_INCREMENT for table `drugcategories`
--
ALTER TABLE `drugcategories`
  MODIFY `CategoryId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=101;

--
-- AUTO_INCREMENT for table `drugs`
--
ALTER TABLE `drugs`
  MODIFY `DrugID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=102;

--
-- AUTO_INCREMENT for table `drugtransferdetails`
--
ALTER TABLE `drugtransferdetails`
  MODIFY `DetailID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `drugtransfers`
--
ALTER TABLE `drugtransfers`
  MODIFY `TransferID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `drug_batches`
--
ALTER TABLE `drug_batches`
  MODIFY `BatchId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=12;

--
-- AUTO_INCREMENT for table `employees`
--
ALTER TABLE `employees`
  MODIFY `EmployeeID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT for table `forecasts`
--
ALTER TABLE `forecasts`
  MODIFY `ForecastID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `fundtransfers`
--
ALTER TABLE `fundtransfers`
  MODIFY `TransferID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `itemgroups`
--
ALTER TABLE `itemgroups`
  MODIFY `GroupId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=101;

--
-- AUTO_INCREMENT for table `journaldetails`
--
ALTER TABLE `journaldetails`
  MODIFY `DetailID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=66;

--
-- AUTO_INCREMENT for table `journalentries`
--
ALTER TABLE `journalentries`
  MODIFY `JournalID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=114;

--
-- AUTO_INCREMENT for table `legacy_shelves_backup`
--
ALTER TABLE `legacy_shelves_backup`
  MODIFY `ShelfId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `legacy_warehouses_backup`
--
ALTER TABLE `legacy_warehouses_backup`
  MODIFY `WarehouseId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `purchasedetails`
--
ALTER TABLE `purchasedetails`
  MODIFY `DetailID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=29;

--
-- AUTO_INCREMENT for table `purchaseplandetails`
--
ALTER TABLE `purchaseplandetails`
  MODIFY `DetailId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `purchaseplans`
--
ALTER TABLE `purchaseplans`
  MODIFY `PlanId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `purchases`
--
ALTER TABLE `purchases`
  MODIFY `PurchaseID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=105;

--
-- AUTO_INCREMENT for table `saledetails`
--
ALTER TABLE `saledetails`
  MODIFY `SaleDetailID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;

--
-- AUTO_INCREMENT for table `sales`
--
ALTER TABLE `sales`
  MODIFY `SaleID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=107;

--
-- AUTO_INCREMENT for table `sale_payments`
--
ALTER TABLE `sale_payments`
  MODIFY `PaymentId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=10;

--
-- AUTO_INCREMENT for table `screenpermissions`
--
ALTER TABLE `screenpermissions`
  MODIFY `PermissionID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=406;

--
-- AUTO_INCREMENT for table `seasonaldata`
--
ALTER TABLE `seasonaldata`
  MODIFY `SeasonalID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `shelves`
--
ALTER TABLE `shelves`
  MODIFY `ShelfId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT for table `stockauditdetails`
--
ALTER TABLE `stockauditdetails`
  MODIFY `DetailID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `stockaudits`
--
ALTER TABLE `stockaudits`
  MODIFY `AuditID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `stockmovements`
--
ALTER TABLE `stockmovements`
  MODIFY `MovementID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=23;

--
-- AUTO_INCREMENT for table `suppliers`
--
ALTER TABLE `suppliers`
  MODIFY `SupplierID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=101;

--
-- AUTO_INCREMENT for table `systemlogs`
--
ALTER TABLE `systemlogs`
  MODIFY `LogId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=152;

--
-- AUTO_INCREMENT for table `systemscreens`
--
ALTER TABLE `systemscreens`
  MODIFY `ScreenID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=58;

--
-- AUTO_INCREMENT for table `units`
--
ALTER TABLE `units`
  MODIFY `UnitId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=102;

--
-- AUTO_INCREMENT for table `userroles`
--
ALTER TABLE `userroles`
  MODIFY `RoleID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=7;

--
-- AUTO_INCREMENT for table `users`
--
ALTER TABLE `users`
  MODIFY `UserID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=9;

--
-- AUTO_INCREMENT for table `vouchers`
--
ALTER TABLE `vouchers`
  MODIFY `VoucherID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `warehouses`
--
ALTER TABLE `warehouses`
  MODIFY `WarehouseId` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- قيود الجداول المُلقاة.
--

--
-- قيود الجداول `accountingtemplatelines`
--
ALTER TABLE `accountingtemplatelines`
  ADD CONSTRAINT `FK_AccountingTemplateLines_AccountingTemplates_TemplateId` FOREIGN KEY (`TemplateId`) REFERENCES `accountingtemplates` (`TemplateId`) ON DELETE CASCADE;

--
-- قيود الجداول `accountmappings`
--
ALTER TABLE `accountmappings`
  ADD CONSTRAINT `FK_AccountMappings_Accounts_AccountId` FOREIGN KEY (`AccountId`) REFERENCES `accounts` (`AccountID`) ON DELETE CASCADE;

--
-- قيود الجداول `accounts`
--
ALTER TABLE `accounts`
  ADD CONSTRAINT `FK_Accounts_Branches` FOREIGN KEY (`BranchId`) REFERENCES `branches` (`BranchID`) ON DELETE SET NULL ON UPDATE CASCADE;

--
-- قيود الجداول `barcodegenerator`
--
ALTER TABLE `barcodegenerator`
  ADD CONSTRAINT `fk_barcode_branch` FOREIGN KEY (`BranchId`) REFERENCES `branches` (`BranchID`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_barcode_drug` FOREIGN KEY (`DrugId`) REFERENCES `drugs` (`DrugID`) ON DELETE CASCADE;

--
-- قيود الجداول `branches`
--
ALTER TABLE `branches`
  ADD CONSTRAINT `fk_branch_cash` FOREIGN KEY (`DefaultCashAccountId`) REFERENCES `accounts` (`AccountID`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_branch_cogs` FOREIGN KEY (`DefaultCOGSAccountId`) REFERENCES `accounts` (`AccountID`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_branch_currency` FOREIGN KEY (`DefaultCurrencyId`) REFERENCES `currencies` (`CurrencyId`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_branch_inv` FOREIGN KEY (`DefaultInventoryAccountId`) REFERENCES `accounts` (`AccountID`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_branch_sales` FOREIGN KEY (`DefaultSalesAccountId`) REFERENCES `accounts` (`AccountID`) ON DELETE SET NULL;

--
-- قيود الجداول `branchinventory`
--
ALTER TABLE `branchinventory`
  ADD CONSTRAINT `fk_branchinventory_branch` FOREIGN KEY (`BranchID`) REFERENCES `branches` (`BranchID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_branchinventory_drug` FOREIGN KEY (`DrugID`) REFERENCES `drugs` (`DrugID`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_inventory_shelf` FOREIGN KEY (`ShelfId`) REFERENCES `legacy_shelves_backup` (`ShelfId`) ON DELETE SET NULL ON UPDATE CASCADE;

--
-- قيود الجداول `customers`
--
ALTER TABLE `customers`
  ADD CONSTRAINT `customers_ibfk_1` FOREIGN KEY (`AccountID`) REFERENCES `accounts` (`AccountID`),
  ADD CONSTRAINT `fk_customers_branch` FOREIGN KEY (`BranchId`) REFERENCES `branches` (`BranchID`);

--
-- قيود الجداول `drugs`
--
ALTER TABLE `drugs`
  ADD CONSTRAINT `fk_drugs_category` FOREIGN KEY (`CategoryId`) REFERENCES `drugcategories` (`CategoryId`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_drugs_itemgroup` FOREIGN KEY (`GroupId`) REFERENCES `itemgroups` (`GroupId`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_drugs_unit` FOREIGN KEY (`UnitId`) REFERENCES `units` (`UnitId`) ON DELETE SET NULL ON UPDATE CASCADE;

--
-- قيود الجداول `drugtransferdetails`
--
ALTER TABLE `drugtransferdetails`
  ADD CONSTRAINT `drugtransferdetails_ibfk_1` FOREIGN KEY (`TransferID`) REFERENCES `drugtransfers` (`TransferID`) ON DELETE CASCADE,
  ADD CONSTRAINT `drugtransferdetails_ibfk_2` FOREIGN KEY (`DrugID`) REFERENCES `drugs` (`DrugID`);

--
-- قيود الجداول `drugtransfers`
--
ALTER TABLE `drugtransfers`
  ADD CONSTRAINT `drugtransfers_ibfk_1` FOREIGN KEY (`FromBranchID`) REFERENCES `branches` (`BranchID`),
  ADD CONSTRAINT `drugtransfers_ibfk_2` FOREIGN KEY (`ToBranchID`) REFERENCES `branches` (`BranchID`),
  ADD CONSTRAINT `drugtransfers_ibfk_3` FOREIGN KEY (`CreatedBy`) REFERENCES `users` (`UserID`),
  ADD CONSTRAINT `fk_dt_journal` FOREIGN KEY (`JournalId`) REFERENCES `journalentries` (`JournalID`),
  ADD CONSTRAINT `fk_dt_receiptjournal` FOREIGN KEY (`ReceiptJournalId`) REFERENCES `journalentries` (`JournalID`),
  ADD CONSTRAINT `fk_dt_receivedby` FOREIGN KEY (`ReceivedBy`) REFERENCES `users` (`UserID`);

--
-- قيود الجداول `drug_batches`
--
ALTER TABLE `drug_batches`
  ADD CONSTRAINT `fk_batch_drug` FOREIGN KEY (`DrugId`) REFERENCES `drugs` (`DrugID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- قيود الجداول `employees`
--
ALTER TABLE `employees`
  ADD CONSTRAINT `employees_ibfk_1` FOREIGN KEY (`BranchID`) REFERENCES `branches` (`BranchID`);

--
-- قيود الجداول `forecasts`
--
ALTER TABLE `forecasts`
  ADD CONSTRAINT `forecasts_ibfk_1` FOREIGN KEY (`BranchID`) REFERENCES `branches` (`BranchID`) ON DELETE CASCADE,
  ADD CONSTRAINT `forecasts_ibfk_2` FOREIGN KEY (`DrugID`) REFERENCES `drugs` (`DrugID`) ON DELETE CASCADE;

--
-- قيود الجداول `fundtransfers`
--
ALTER TABLE `fundtransfers`
  ADD CONSTRAINT `fk_fund_branch` FOREIGN KEY (`BranchId`) REFERENCES `branches` (`BranchID`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_fund_journal` FOREIGN KEY (`JournalId`) REFERENCES `journalentries` (`JournalID`) ON DELETE SET NULL,
  ADD CONSTRAINT `fundtransfers_ibfk_1` FOREIGN KEY (`FromAccountID`) REFERENCES `accounts` (`AccountID`),
  ADD CONSTRAINT `fundtransfers_ibfk_2` FOREIGN KEY (`ToAccountID`) REFERENCES `accounts` (`AccountID`),
  ADD CONSTRAINT `fundtransfers_ibfk_3` FOREIGN KEY (`CreatedBy`) REFERENCES `users` (`UserID`);

--
-- قيود الجداول `journaldetails`
--
ALTER TABLE `journaldetails`
  ADD CONSTRAINT `fk_journaldetails_account` FOREIGN KEY (`AccountID`) REFERENCES `accounts` (`AccountID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_journaldetails_journal` FOREIGN KEY (`JournalID`) REFERENCES `journalentries` (`JournalID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- قيود الجداول `journalentries`
--
ALTER TABLE `journalentries`
  ADD CONSTRAINT `fk_journalentries_branch` FOREIGN KEY (`BranchID`) REFERENCES `branches` (`BranchID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `journalentries_ibfk_1` FOREIGN KEY (`BranchID`) REFERENCES `branches` (`BranchID`),
  ADD CONSTRAINT `journalentries_ibfk_2` FOREIGN KEY (`CreatedBy`) REFERENCES `users` (`UserID`);

--
-- قيود الجداول `legacy_shelves_backup`
--
ALTER TABLE `legacy_shelves_backup`
  ADD CONSTRAINT `fk_shelf_itemgroup` FOREIGN KEY (`GroupId`) REFERENCES `itemgroups` (`GroupId`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_shelf_warehouse` FOREIGN KEY (`WarehouseId`) REFERENCES `legacy_warehouses_backup` (`WarehouseId`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- قيود الجداول `legacy_warehouses_backup`
--
ALTER TABLE `legacy_warehouses_backup`
  ADD CONSTRAINT `fk_warehouse_branch` FOREIGN KEY (`BranchId`) REFERENCES `branches` (`BranchID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- قيود الجداول `purchasedetails`
--
ALTER TABLE `purchasedetails`
  ADD CONSTRAINT `fk_purchasedetails_drug` FOREIGN KEY (`DrugID`) REFERENCES `drugs` (`DrugID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_purchasedetails_purchase` FOREIGN KEY (`PurchaseID`) REFERENCES `purchases` (`PurchaseID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- قيود الجداول `purchaseplandetails`
--
ALTER TABLE `purchaseplandetails`
  ADD CONSTRAINT `FK_purchaseplandetails_drugs_DrugId` FOREIGN KEY (`DrugId`) REFERENCES `drugs` (`DrugID`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_purchaseplandetails_purchaseplans_PlanId` FOREIGN KEY (`PlanId`) REFERENCES `purchaseplans` (`PlanId`) ON DELETE CASCADE;

--
-- قيود الجداول `purchaseplans`
--
ALTER TABLE `purchaseplans`
  ADD CONSTRAINT `FK_purchaseplans_branches_BranchId` FOREIGN KEY (`BranchId`) REFERENCES `branches` (`BranchID`) ON DELETE CASCADE,
  ADD CONSTRAINT `FK_purchaseplans_users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `users` (`UserID`) ON DELETE CASCADE;

--
-- قيود الجداول `purchases`
--
ALTER TABLE `purchases`
  ADD CONSTRAINT `fk_purchases_branch` FOREIGN KEY (`BranchID`) REFERENCES `branches` (`BranchID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_purchases_parent` FOREIGN KEY (`ParentPurchaseId`) REFERENCES `purchases` (`PurchaseID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_purchases_supplier` FOREIGN KEY (`SupplierId`) REFERENCES `suppliers` (`SupplierID`) ON UPDATE CASCADE;

--
-- قيود الجداول `saledetails`
--
ALTER TABLE `saledetails`
  ADD CONSTRAINT `fk_saledetails_drug` FOREIGN KEY (`DrugID`) REFERENCES `drugs` (`DrugID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_saledetails_sale` FOREIGN KEY (`SaleID`) REFERENCES `sales` (`SaleID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- قيود الجداول `sales`
--
ALTER TABLE `sales`
  ADD CONSTRAINT `fk_sales_branch` FOREIGN KEY (`BranchID`) REFERENCES `branches` (`BranchID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_sales_user` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON UPDATE CASCADE,
  ADD CONSTRAINT `sales_ibfk_3` FOREIGN KEY (`CustomerID`) REFERENCES `customers` (`CustomerID`);

--
-- قيود الجداول `sale_payments`
--
ALTER TABLE `sale_payments`
  ADD CONSTRAINT `fk_sale_payments_account` FOREIGN KEY (`AccountId`) REFERENCES `accounts` (`AccountID`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_sale_payments_sale` FOREIGN KEY (`SaleId`) REFERENCES `sales` (`SaleID`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- قيود الجداول `screenpermissions`
--
ALTER TABLE `screenpermissions`
  ADD CONSTRAINT `screenpermissions_ibfk_1` FOREIGN KEY (`RoleID`) REFERENCES `userroles` (`RoleID`) ON DELETE CASCADE,
  ADD CONSTRAINT `screenpermissions_ibfk_2` FOREIGN KEY (`ScreenID`) REFERENCES `systemscreens` (`ScreenID`) ON DELETE CASCADE;

--
-- قيود الجداول `seasonaldata`
--
ALTER TABLE `seasonaldata`
  ADD CONSTRAINT `seasonaldata_ibfk_1` FOREIGN KEY (`BranchID`) REFERENCES `branches` (`BranchID`) ON DELETE CASCADE,
  ADD CONSTRAINT `seasonaldata_ibfk_2` FOREIGN KEY (`DrugID`) REFERENCES `drugs` (`DrugID`) ON DELETE CASCADE;

--
-- قيود الجداول `shelves`
--
ALTER TABLE `shelves`
  ADD CONSTRAINT `fk_shelf_itemgroup_new` FOREIGN KEY (`GroupId`) REFERENCES `itemgroups` (`GroupId`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_shelf_warehouse_new` FOREIGN KEY (`WarehouseId`) REFERENCES `warehouses` (`WarehouseId`) ON DELETE CASCADE;

--
-- قيود الجداول `stockauditdetails`
--
ALTER TABLE `stockauditdetails`
  ADD CONSTRAINT `fk_audit_details_drug` FOREIGN KEY (`DrugID`) REFERENCES `drugs` (`DrugID`),
  ADD CONSTRAINT `fk_audit_details_main` FOREIGN KEY (`AuditID`) REFERENCES `stockaudits` (`AuditID`) ON DELETE CASCADE;

--
-- قيود الجداول `stockaudits`
--
ALTER TABLE `stockaudits`
  ADD CONSTRAINT `fk_audits_branch` FOREIGN KEY (`BranchID`) REFERENCES `branches` (`BranchID`),
  ADD CONSTRAINT `fk_audits_user` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`);

--
-- قيود الجداول `stockmovements`
--
ALTER TABLE `stockmovements`
  ADD CONSTRAINT `stockmovements_ibfk_1` FOREIGN KEY (`BranchID`) REFERENCES `branches` (`BranchID`) ON DELETE CASCADE,
  ADD CONSTRAINT `stockmovements_ibfk_2` FOREIGN KEY (`DrugID`) REFERENCES `drugs` (`DrugID`) ON DELETE CASCADE,
  ADD CONSTRAINT `stockmovements_ibfk_3` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`);

--
-- قيود الجداول `suppliers`
--
ALTER TABLE `suppliers`
  ADD CONSTRAINT `fk_suppliers_branch` FOREIGN KEY (`BranchId`) REFERENCES `branches` (`BranchID`),
  ADD CONSTRAINT `suppliers_ibfk_1` FOREIGN KEY (`AccountID`) REFERENCES `accounts` (`AccountID`);

--
-- قيود الجداول `systemlogs`
--
ALTER TABLE `systemlogs`
  ADD CONSTRAINT `systemlogs_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserID`);

--
-- قيود الجداول `users`
--
ALTER TABLE `users`
  ADD CONSTRAINT `users_ibfk_1` FOREIGN KEY (`RoleID`) REFERENCES `userroles` (`RoleID`),
  ADD CONSTRAINT `users_ibfk_2` FOREIGN KEY (`EmployeeID`) REFERENCES `employees` (`EmployeeID`),
  ADD CONSTRAINT `users_ibfk_3` FOREIGN KEY (`DefaultBranchID`) REFERENCES `branches` (`BranchID`);

--
-- قيود الجداول `warehouses`
--
ALTER TABLE `warehouses`
  ADD CONSTRAINT `fk_warehouse_branch_new` FOREIGN KEY (`BranchId`) REFERENCES `branches` (`BranchID`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
