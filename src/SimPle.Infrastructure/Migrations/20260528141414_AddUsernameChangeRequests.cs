using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimPle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernameChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "username_change_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedUsername = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NormalizedRequestedUsername = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_username_change_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_username_change_requests_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_username_change_requests_UserId",
                table: "username_change_requests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_username_change_requests_UserId_Status",
                table: "username_change_requests",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "username_change_requests");
        }
    }
}
