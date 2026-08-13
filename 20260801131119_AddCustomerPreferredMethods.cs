using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniSmartstoreMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPreferredMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredPaymentMethod",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredShippingMethodId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PreferredShippingMethodId",
                table: "AspNetUsers",
                column: "PreferredShippingMethodId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_ShippingMethods_PreferredShippingMethodId",
                table: "AspNetUsers",
                column: "PreferredShippingMethodId",
                principalTable: "ShippingMethods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_ShippingMethods_PreferredShippingMethodId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PreferredShippingMethodId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredPaymentMethod",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredShippingMethodId",
                table: "AspNetUsers");
        }
    }
}
