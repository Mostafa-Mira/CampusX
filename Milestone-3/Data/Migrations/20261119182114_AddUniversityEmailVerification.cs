using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusX.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniversityEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerifiedUniversityEmail",
                table: "ApplicationUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UniversityEmailVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniversityEmailVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UniversityEmailVerifications_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UniversityVerificationAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniversityVerificationAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UniversityVerificationAttempts_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_VerifiedUniversityEmail",
                table: "ApplicationUsers",
                column: "VerifiedUniversityEmail",
                unique: true,
                filter: "[VerifiedUniversityEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UniversityEmailVerifications_Recipient_CreatedAt",
                table: "UniversityEmailVerifications",
                columns: new[] { "Recipient", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UniversityEmailVerifications_UserId",
                table: "UniversityEmailVerifications",
                column: "UserId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_UniversityEmailVerifications_UserId_CreatedAt",
                table: "UniversityEmailVerifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UniversityVerificationAttempts_Recipient_AttemptedAt",
                table: "UniversityVerificationAttempts",
                columns: new[] { "Recipient", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UniversityVerificationAttempts_UserId_AttemptedAt",
                table: "UniversityVerificationAttempts",
                columns: new[] { "UserId", "AttemptedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UniversityEmailVerifications");

            migrationBuilder.DropTable(
                name: "UniversityVerificationAttempts");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUsers_VerifiedUniversityEmail",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "VerifiedUniversityEmail",
                table: "ApplicationUsers");
        }
    }
}
