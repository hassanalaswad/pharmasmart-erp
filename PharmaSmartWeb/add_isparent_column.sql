ALTER TABLE `accounts`
ADD COLUMN `IsParent` tinyint(1) NOT NULL DEFAULT 0;

-- Optional: Update existing records to be IsParent = 1 if they have sub-accounts automatically
UPDATE `accounts` a
SET `IsParent` = 1

WHERE EXISTS (
    SELECT 1 FROM (SELECT * FROM `accounts`) sub WHERE sub.`ParentAccountID` = a.`AccountID`
);
