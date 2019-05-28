using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SearchServer.Migrations
{
    public partial class bookmarkdatetime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_AspNetUsers_ToUserId",
                table: "Comment");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTime",
                table: "Bookmark",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_AspNetUsers_ToUserId",
                table: "Comment",
                column: "ToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_AspNetUsers_ToUserId",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "DateTime",
                table: "Bookmark");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_AspNetUsers_ToUserId",
                table: "Comment",
                column: "ToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
