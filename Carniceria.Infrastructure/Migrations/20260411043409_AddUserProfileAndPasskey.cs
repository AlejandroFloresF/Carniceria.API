using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Carniceria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileAndPasskey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "AppUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "AppUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "AppUsers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OtpExpiry",
                table: "AppUsers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtpPurpose",
                table: "AppUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PasskeyCredentialId",
                table: "AppUsers",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PasskeyPublicKey",
                table: "AppUsers",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PasskeySignCount",
                table: "AppUsers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetExpiry",
                table: "AppUsers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "AppUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingEmail",
                table: "AppUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoBase64",
                table: "AppUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Email",
                table: "AppUsers",
                column: "Email",
                unique: true,
                filter: "\"Email\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppUsers_Email",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "OtpExpiry",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "OtpPurpose",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "PasskeyCredentialId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "PasskeyPublicKey",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "PasskeySignCount",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "PasswordResetExpiry",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "PendingEmail",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoBase64",
                table: "AppUsers");
        }
    }
}
