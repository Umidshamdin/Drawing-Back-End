using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Drawing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinalBackendCompletionv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BuyerUserId_RequestId",
                table: "Orders",
                columns: new[] { "BuyerUserId", "RequestId" },
                unique: true,
                filter: "\"RequestId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_BuyerUserId_RequestId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Orders");
        }
    }
}
