using Microsoft.EntityFrameworkCore.Migrations;

namespace SearchServer.Migrations
{
    public partial class groupcomments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DocumentId",
                table: "Comment",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "Comment",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comment_GroupId",
                table: "Comment",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Group_GroupId",
                table: "Comment",
                column: "GroupId",
                principalTable: "Group",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Group_GroupId",
                table: "Comment");

            migrationBuilder.DropIndex(
                name: "IX_Comment_GroupId",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Comment");

            migrationBuilder.AlterColumn<string>(
                name: "DocumentId",
                table: "Comment",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
