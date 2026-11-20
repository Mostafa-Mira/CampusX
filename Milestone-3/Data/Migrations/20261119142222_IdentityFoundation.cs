using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusX.Data.Migrations
{
    /// <inheritdoc />
    public partial class IdentityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fail before changing schema if automatic backfill cannot preserve unambiguous logins.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [ApplicationUsers] WHERE [Email] IS NULL OR LTRIM(RTRIM([Email])) = N'')
                    THROW 51010, 'Identity migration requires a nonblank personal email for each existing account.', 1;
                IF EXISTS (SELECT 1 FROM [ApplicationUsers] WHERE [Email] COLLATE Latin1_General_100_BIN2 LIKE N'%[^ -~]%')
                    THROW 51011, 'Non-ASCII emails require a reviewed .NET normalization backfill before Identity migration.', 1;
                IF EXISTS (SELECT UPPER(LTRIM(RTRIM([Email])) COLLATE Latin1_General_100_BIN2) FROM [ApplicationUsers]
                           GROUP BY UPPER(LTRIM(RTRIM([Email])) COLLATE Latin1_General_100_BIN2) HAVING COUNT(*) > 1)
                    THROW 51012, 'Resolve normalized personal-email collisions before Identity migration.', 1;
                IF EXISTS (SELECT 1 FROM [ApplicationUsers] WHERE [Role] NOT IN (0, 1))
                    THROW 51013, 'Review unknown account roles before Identity migration.', 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "ApplicationUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "ApplicationUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "ApplicationUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LockoutEnabled",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "ApplicationUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "ApplicationUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserName",
                table: "ApplicationUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "ApplicationUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UniversityEmail",
                table: "ApplicationUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UniversityEmailConfirmed",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "ApplicationUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Preserve Id, Email, PasswordHash and Role; only initialize the new Identity metadata.
            // UniversityEmail remains NULL and UniversityEmailConfirmed remains false for every old account.
            migrationBuilder.Sql("""
                UPDATE [ApplicationUsers]
                SET [UserName] = LOWER(CONVERT(nvarchar(36), [Id])),
                    [NormalizedUserName] = UPPER(CONVERT(nvarchar(36), [Id])),
                    [NormalizedEmail] = UPPER(LTRIM(RTRIM([Email])) COLLATE Latin1_General_100_BIN2),
                    [SecurityStamp] = CONVERT(nvarchar(36), NEWID()),
                    [ConcurrencyStamp] = CONVERT(nvarchar(36), NEWID());
                """);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "ApplicationUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "ApplicationUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [ApplicationUsers] WHERE [PasswordHash] IS NULL)
                    THROW 51014, 'Passwordless accounts require a reviewed rollback plan; do not replace hashes with blank passwords.', 1;
                IF EXISTS (SELECT 1 FROM [AspNetUserLogins]) OR EXISTS (SELECT 1 FROM [AspNetUserClaims])
                   OR EXISTS (SELECT 1 FROM [AspNetUserTokens])
                   OR EXISTS (SELECT 1 FROM [ApplicationUsers] WHERE [UniversityEmail] IS NOT NULL OR [UniversityEmailConfirmed] = 1)
                    THROW 51015, 'Identity or university data exists; review and preserve it before rolling back.', 1;
                """);

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "ApplicationUsers");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "LockoutEnabled",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "NormalizedUserName",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmed",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "UniversityEmail",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "UniversityEmailConfirmed",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "ApplicationUsers");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "ApplicationUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
