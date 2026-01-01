using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CHBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserContractorRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContractorId",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ContractorId",
                table: "AspNetUsers",
                column: "ContractorId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Contractors_ContractorId",
                table: "AspNetUsers",
                column: "ContractorId",
                principalTable: "Contractors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Contractors_ContractorId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ContractorId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ContractorId",
                table: "AspNetUsers");
        }
    }
}
