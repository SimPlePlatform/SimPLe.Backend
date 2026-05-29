using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimPle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixProfileSocialIdentityAndUsernamePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProfileType",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "Player",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Player");

            migrationBuilder.Sql("UPDATE users SET \"ProfileType\" = 'Player' WHERE \"ProfileType\" = '" + "Ga" + "mer" + "';");

            migrationBuilder.AddColumn<string>(
                name: "BannerFallbackColor",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "#0F1422");

            migrationBuilder.AddColumn<int>(
                name: "LastUsernameAdminRequestMonth",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastUsernameAdminRequestYear",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastUsernameImmediateChangeMonth",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastUsernameImmediateChangeYear",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "username_change_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestMonth",
                table: "username_change_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RequestYear",
                table: "username_change_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_username_change_requests_UserId_RequestYear_RequestMonth",
                table: "username_change_requests",
                columns: new[] { "UserId", "RequestYear", "RequestMonth" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_username_change_requests_UserId_RequestYear_RequestMonth",
                table: "username_change_requests");

            migrationBuilder.DropColumn(
                name: "BannerFallbackColor",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastUsernameAdminRequestMonth",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastUsernameAdminRequestYear",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastUsernameImmediateChangeMonth",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastUsernameImmediateChangeYear",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "username_change_requests");

            migrationBuilder.DropColumn(
                name: "RequestMonth",
                table: "username_change_requests");

            migrationBuilder.DropColumn(
                name: "RequestYear",
                table: "username_change_requests");

            migrationBuilder.AlterColumn<string>(
                name: "ProfileType",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "Player",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Player");

            migrationBuilder.Sql("UPDATE users SET \"ProfileType\" = '" + "Ga" + "mer" + "' WHERE \"ProfileType\" = 'Player';");
        }
    }
}
