using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SensorHub.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueApiKeyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_ApiKeyHash",
                table: "Devices");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_ApiKeyHash",
                table: "Devices",
                column: "ApiKeyHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_ApiKeyHash",
                table: "Devices");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_ApiKeyHash",
                table: "Devices",
                column: "ApiKeyHash");
        }
    }
}
