using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaSmartWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingScreen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "warehouses",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "VoucherID",
                table: "vouchers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "users",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "RoleID",
                table: "userroles",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ScreenID",
                table: "systemscreens",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "LogId",
                table: "systemlogs",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "SupplierId",
                table: "suppliers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "MovementID",
                table: "stockmovements",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "stockaudits",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "AuditId",
                table: "stockaudits",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "DetailId",
                table: "stockauditdetails",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ShelfId",
                table: "shelves",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "SeasonalID",
                table: "seasonaldata",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "PermissionID",
                table: "screenpermissions",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "SaleID",
                table: "sales",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "SaleDetailID",
                table: "saledetails",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentId",
                table: "sale_payments",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.UpdateData(
                table: "purchases",
                keyColumn: "PaymentStatus",
                keyValue: null,
                column: "PaymentStatus",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "purchases",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "PurchaseID",
                table: "purchases",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "PlanId",
                table: "purchaseplans",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "DetailId",
                table: "purchaseplandetails",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "DetailID",
                table: "purchasedetails",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "journalentries",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "JournalID",
                table: "journalentries",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "DetailID",
                table: "journaldetails",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "itemgroups",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "TransferID",
                table: "fundtransfers",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ForecastID",
                table: "forecasts",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeID",
                table: "employees",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "TransferID",
                table: "drugtransfers",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "DetailID",
                table: "drugtransferdetails",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "DrugID",
                table: "drugs",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "BatchId",
                table: "drug_batches",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "CustomerID",
                table: "customers",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "currencies",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "CompanySettings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "BranchID",
                table: "branches",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "branch_accounts",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "barcodegenerator",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "AccountID",
                table: "accounts",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "MappingId",
                table: "AccountMappings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.UpdateData(
                table: "AccountingTemplates",
                keyColumn: "TemplateName",
                keyValue: null,
                column: "TemplateName",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "TemplateName",
                table: "AccountingTemplates",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext CHARACTER SET utf8mb4",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "TemplateId",
                table: "AccountingTemplates",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "LineId",
                table: "AccountingTemplateLines",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "warehouses",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "vouchers",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "VoucherID",
                table: "vouchers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "users",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "RoleID",
                table: "userroles",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ScreenID",
                table: "systemscreens",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Details",
                table: "systemlogs",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "LogId",
                table: "systemlogs",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "suppliers",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "SupplierId",
                table: "suppliers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "MovementID",
                table: "stockmovements",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "stockaudits",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "AuditId",
                table: "stockaudits",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "DetailId",
                table: "stockauditdetails",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ShelfId",
                table: "shelves",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "SeasonalID",
                table: "seasonaldata",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "PermissionID",
                table: "screenpermissions",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "SaleID",
                table: "sales",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "SaleDetailID",
                table: "saledetails",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentId",
                table: "sale_payments",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "purchases",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "purchases",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "PurchaseID",
                table: "purchases",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "PlanId",
                table: "purchaseplans",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "DetailId",
                table: "purchaseplandetails",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "DetailID",
                table: "purchasedetails",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "journalentries",
                type: "varchar(500)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "JournalID",
                table: "journalentries",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "DetailID",
                table: "journaldetails",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "itemgroups",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "TransferID",
                table: "fundtransfers",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ForecastID",
                table: "forecasts",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeID",
                table: "employees",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "TransferID",
                table: "drugtransfers",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "DetailID",
                table: "drugtransferdetails",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "DrugID",
                table: "drugs",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "BatchId",
                table: "drug_batches",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "customers",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerID",
                table: "customers",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "currencies",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "CompanySettings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "BranchID",
                table: "branches",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "branch_accounts",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "barcodegenerator",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "AccountID",
                table: "accounts",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "MappingId",
                table: "AccountMappings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateName",
                table: "AccountingTemplates",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "TemplateId",
                table: "AccountingTemplates",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<int>(
                name: "LineId",
                table: "AccountingTemplateLines",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
        }
    }
}
