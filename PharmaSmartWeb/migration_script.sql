ALTER TABLE `purchases` ADD `InvoiceImagePath` varchar(500) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `drugs` ADD `IsLifeSaving` tinyint(1) NULL;

ALTER TABLE `CompanySettings` ADD `Address` varchar(500) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `CompanySettings` ADD `Email` varchar(100) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `CompanySettings` ADD `Phone` varchar(50) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `CompanySettings` ADD `TaxNumber` varchar(100) CHARACTER SET utf8mb4 NULL;

CREATE TABLE `purchaseplans` (
    `PlanId` int(11) NOT NULL AUTO_INCREMENT,
    `BranchId` int(11) NOT NULL,
    `CreatedBy` int(11) NOT NULL,
    `PlanDate` datetime NOT NULL,
    `Status` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Notes` varchar(500) CHARACTER SET utf8mb4 NULL,
    `EstimatedTotalCost` decimal(18,4) NOT NULL,
    CONSTRAINT `PK_purchaseplans` PRIMARY KEY (`PlanId`),
    CONSTRAINT `FK_purchaseplans_branches_BranchId` FOREIGN KEY (`BranchId`) REFERENCES `branches` (`BranchID`) ON DELETE CASCADE,
    CONSTRAINT `FK_purchaseplans_users_CreatedBy` FOREIGN KEY (`CreatedBy`) REFERENCES `users` (`UserID`) ON DELETE CASCADE
);

CREATE TABLE `purchaseplandetails` (
    `DetailId` int(11) NOT NULL AUTO_INCREMENT,
    `PlanId` int(11) NOT NULL,
    `DrugId` int(11) NOT NULL,
    `CurrentStock` int NOT NULL,
    `ABCCategory` varchar(10) CHARACTER SET utf8mb4 NULL,
    `ForecastedDemand` decimal(18,4) NOT NULL,
    `ForecastAccuracy` decimal(18,4) NOT NULL,
    `ProposedQuantity` int NOT NULL,
    `ApprovedQuantity` int NOT NULL,
    `UnitCostEstimate` decimal(18,4) NOT NULL,
    `TotalCost` decimal(18,4) NOT NULL,
    `IsLifeSaving` tinyint(1) NOT NULL,
    `Status` varchar(100) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_purchaseplandetails` PRIMARY KEY (`DetailId`),
    CONSTRAINT `FK_purchaseplandetails_drugs_DrugId` FOREIGN KEY (`DrugId`) REFERENCES `drugs` (`DrugID`) ON DELETE CASCADE,
    CONSTRAINT `FK_purchaseplandetails_purchaseplans_PlanId` FOREIGN KEY (`PlanId`) REFERENCES `purchaseplans` (`PlanId`) ON DELETE CASCADE
);

CREATE INDEX `IX_purchaseplandetails_DrugId` ON `purchaseplandetails` (`DrugId`);

CREATE INDEX `IX_purchaseplandetails_PlanId` ON `purchaseplandetails` (`PlanId`);

CREATE INDEX `IX_purchaseplans_BranchId` ON `purchaseplans` (`BranchId`);

CREATE INDEX `IX_purchaseplans_CreatedBy` ON `purchaseplans` (`CreatedBy`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260406090521_SyncModelsWithDatabase', '3.1.32');

