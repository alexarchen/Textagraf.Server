using Microsoft.EntityFrameworkCore.Migrations;

namespace SearchServer.Migrations
{
    public partial class uniquegroupname : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UniqueName",
                table: "Group",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Group_UniqueName",
                table: "Group",
                column: "UniqueName",
                unique: true,
                filter: "[UniqueName] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Group_UniqueName",
                table: "Group");

            migrationBuilder.DropColumn(
                name: "UniqueName",
                table: "Group");
        }
    }
}
