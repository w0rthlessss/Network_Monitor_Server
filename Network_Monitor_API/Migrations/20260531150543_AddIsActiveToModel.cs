using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Network_Monitor_API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Models",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Models");
        }
    }
}
