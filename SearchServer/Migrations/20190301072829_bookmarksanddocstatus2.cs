using Microsoft.EntityFrameworkCore.Migrations;

namespace SearchServer.Migrations
{
    public partial class bookmarksanddocstatus2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookmark_DocumentId",
                table: "Bookmark");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmark_DocumentId_UserId",
                table: "Bookmark",
                columns: new[] { "DocumentId", "UserId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookmark_DocumentId_UserId",
                table: "Bookmark");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmark_DocumentId",
                table: "Bookmark",
                column: "DocumentId");
        }
    }
}
