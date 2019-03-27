using Microsoft.EntityFrameworkCore.Migrations;

namespace SearchServer.Migrations
{
    public partial class docpagesandtitlepage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Group",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Pages",
                table: "Document",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<byte>(
                name: "TitlePage",
                table: "Document",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Document",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "AspNetUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Url",
                table: "Group");

            migrationBuilder.DropColumn(
                name: "Pages",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "TitlePage",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "AspNetUsers");
        }
    }
}
