using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BBAPP.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanRequestStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "21dc7eac-9407-4632-b174-c78c2db4a5bb");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a879f886-13fb-4aab-84af-b58e8af3d751");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "b7de78ec-4f37-4ed6-a555-dfd6fbb7e5f0");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "1dddb366-3973-4d18-83dd-ffd986160b89", null, "Admin", "ADMIN" },
                    { "386e7427-fff7-4095-a005-96982f60982d", null, "Bibliotecario", "BIBLIOTECARIO" },
                    { "4a269c7b-dc11-4e16-b8d2-8e7ec98f19fe", null, "Usuario", "USUARIO" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1dddb366-3973-4d18-83dd-ffd986160b89");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "386e7427-fff7-4095-a005-96982f60982d");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4a269c7b-dc11-4e16-b8d2-8e7ec98f19fe");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "21dc7eac-9407-4632-b174-c78c2db4a5bb", null, "Admin", "ADMIN" },
                    { "a879f886-13fb-4aab-84af-b58e8af3d751", null, "Bibliotecario", "BIBLIOTECARIO" },
                    { "b7de78ec-4f37-4ed6-a555-dfd6fbb7e5f0", null, "Usuario", "USUARIO" }
                });
        }
    }
}
