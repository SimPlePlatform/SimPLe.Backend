using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimPle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileSocialIdentityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileType",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "Player");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileType",
                table: "users");
        }
    }
}
