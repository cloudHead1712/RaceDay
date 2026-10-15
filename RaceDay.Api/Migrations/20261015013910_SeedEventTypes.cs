using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedEventTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "EventTypes",
                columns: new[] { "EventTypeId", "Name" },
                values: new object[,]
                {
                    { 1, "Run" },
                    { 2, "Walk" },
                    { 3, "Cycle" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EventTypes",
                keyColumn: "EventTypeId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "EventTypes",
                keyColumn: "EventTypeId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "EventTypes",
                keyColumn: "EventTypeId",
                keyValue: 3);
        }
    }
}
