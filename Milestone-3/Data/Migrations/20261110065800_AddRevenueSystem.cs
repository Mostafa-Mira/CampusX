using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusX.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRevenueSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductPromotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPromotions", x => x.Id);
                    table.CheckConstraint("CK_ProductPromotions_Expiry", "[ExpiresAt] > [ActivatedAt]");
                    table.ForeignKey(
                        name: "FK_ProductPromotions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromotionCreditPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Package = table.Column<int>(type: "int", nullable: false),
                    CreditsPurchased = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProviderReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCreditPurchases", x => x.Id);
                    table.CheckConstraint("CK_PromotionCreditPurchases_Credits_Positive", "[CreditsPurchased] > 0");
                    table.ForeignKey(
                        name: "FK_PromotionCreditPurchases_ApplicationUsers_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPromotions_ProductId_ExpiresAt",
                table: "ProductPromotions",
                columns: new[] { "ProductId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCreditPurchases_BuyerId",
                table: "PromotionCreditPurchases",
                column: "BuyerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductPromotions");

            migrationBuilder.DropTable(
                name: "PromotionCreditPurchases");
        }
    }
}
