using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PharmaSmartWeb.Migrations
{
    public partial class SyncModelsWithDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceImagePath",
                table: "purchases",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLifeSaving",
                table: "drugs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "CompanySettings",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "CompanySettings",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "CompanySettings",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "CompanySettings",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "purchaseplans",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BranchId = table.Column<int>(type: "int(11)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int(11)", nullable: false),
                    PlanDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Status = table.Column<string>(maxLength: 50, nullable: true),
                    Notes = table.Column<string>(maxLength: 500, nullable: true),
                    EstimatedTotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchaseplans", x => x.PlanId);
                    table.ForeignKey(
                        name: "FK_purchaseplans_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "BranchID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchaseplans_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchaseplandetails",
                columns: table => new
                {
                    DetailId = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlanId = table.Column<int>(type: "int(11)", nullable: false),
                    DrugId = table.Column<int>(type: "int(11)", nullable: false),
                    CurrentStock = table.Column<int>(nullable: false),
                    ABCCategory = table.Column<string>(maxLength: 10, nullable: true),
                    ForecastedDemand = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ForecastAccuracy = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ProposedQuantity = table.Column<int>(nullable: false),
                    ApprovedQuantity = table.Column<int>(nullable: false),
                    UnitCostEstimate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsLifeSaving = table.Column<bool>(nullable: false),
                    Status = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchaseplandetails", x => x.DetailId);
                    table.ForeignKey(
                        name: "FK_purchaseplandetails_drugs_DrugId",
                        column: x => x.DrugId,
                        principalTable: "drugs",
                        principalColumn: "DrugID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchaseplandetails_purchaseplans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "purchaseplans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchaseplandetails_DrugId",
                table: "purchaseplandetails",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseplandetails_PlanId",
                table: "purchaseplandetails",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseplans_BranchId",
                table: "purchaseplans",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseplans_CreatedBy",
                table: "purchaseplans",
                column: "CreatedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchaseplandetails");

            migrationBuilder.DropTable(
                name: "purchaseplans");

            migrationBuilder.DropColumn(
                name: "InvoiceImagePath",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "IsLifeSaving",
                table: "drugs");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "CompanySettings");
        }
    }
}
