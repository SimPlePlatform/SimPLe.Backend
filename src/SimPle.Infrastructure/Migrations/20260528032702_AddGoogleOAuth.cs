using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimPle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleOAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_GoogleId",
                table: "users",
                column: "GoogleId",
                unique: true,
                filter: "\"GoogleId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_GoogleId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "users");
        }
    }
}
