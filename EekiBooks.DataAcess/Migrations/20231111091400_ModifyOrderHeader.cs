using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EekiBooks.DataAcess.Migrations
{
    /// <inheritdoc />
    public partial class ModifyOrderHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentIntent",
                table: "OrderHeaders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentIntent",
                table: "OrderHeaders");
        }
    }
}
