using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketPulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlertDeliveryUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "AlertDeliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AlertDeliveries_AnomalyId_ConfigurationId",
                table: "AlertDeliveries",
                columns: new[] { "AnomalyId", "ConfigurationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AlertDeliveries_AnomalyId_ConfigurationId",
                table: "AlertDeliveries");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "AlertDeliveries");
        }
    }
}
