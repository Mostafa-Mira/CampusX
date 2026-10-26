using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusX.Data.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The old schema did not store passwords. Existing accounts need a
            // deliberate password-reset plan rather than a usable blank hash.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [ApplicationUsers])
                    THROW 51000, 'Existing accounts need a password migration plan before this migration.', 1;
                IF EXISTS (SELECT 1 FROM [Products] WHERE [Price] < 0 OR [Status] NOT IN (0, 1))
                    THROW 51001, 'Products contain invalid prices or legacy statuses.', 1;
                IF EXISTS (SELECT 1 FROM [Reports]
                    WHERE ([TargetProductId] IS NULL AND [TargetUserId] IS NULL)
                       OR ([TargetProductId] IS NOT NULL AND [TargetUserId] IS NOT NULL))
                    THROW 51002, 'Reports must target exactly one product or user.', 1;
                IF EXISTS (SELECT 1 FROM [ContactInquiries] AS i
                    JOIN [Products] AS p ON p.[Id] = i.[ProductId]
                    WHERE i.[SellerId] <> p.[SellerId])
                    THROW 51003, 'Contact inquiries contain a seller different from the product seller.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_ContactInquiries_ApplicationUsers_SellerId",
                table: "ContactInquiries");

            migrationBuilder.DropForeignKey(
                name: "FK_ContactInquiries_Products_ProductId",
                table: "ContactInquiries");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_ApplicationUsers_SellerId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Products_TargetProductId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_ContactInquiries_SellerId",
                table: "ContactInquiries");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "ContactInquiries");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Products",
                newName: "Name");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ApplicationUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "ApplicationUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reports_ExactlyOneTarget",
                table: "Reports",
                sql: "([TargetProductId] IS NOT NULL AND [TargetUserId] IS NULL) OR ([TargetProductId] IS NULL AND [TargetUserId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Price_NonNegative",
                table: "Products",
                sql: "[Price] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_Email",
                table: "ApplicationUsers",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContactInquiries_Products_ProductId",
                table: "ContactInquiries",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ApplicationUsers_SellerId",
                table: "Products",
                column: "SellerId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Products_TargetProductId",
                table: "Reports",
                column: "TargetProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactInquiries_Products_ProductId",
                table: "ContactInquiries");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_ApplicationUsers_SellerId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Products_TargetProductId",
                table: "Reports");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reports_ExactlyOneTarget",
                table: "Reports");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Price_NonNegative",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUsers_Email",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "ApplicationUsers");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Products",
                newName: "Title");

            migrationBuilder.AddColumn<Guid>(
                name: "SellerId",
                table: "ContactInquiries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ApplicationUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.Sql("""
                UPDATE i
                SET [SellerId] = p.[SellerId]
                FROM [ContactInquiries] AS i
                INNER JOIN [Products] AS p ON p.[Id] = i.[ProductId];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ContactInquiries_SellerId",
                table: "ContactInquiries",
                column: "SellerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactInquiries_ApplicationUsers_SellerId",
                table: "ContactInquiries",
                column: "SellerId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContactInquiries_Products_ProductId",
                table: "ContactInquiries",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ApplicationUsers_SellerId",
                table: "Products",
                column: "SellerId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Products_TargetProductId",
                table: "Reports",
                column: "TargetProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
