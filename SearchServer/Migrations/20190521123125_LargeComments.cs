using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SearchServer.Migrations
{
    public partial class LargeComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Comment",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.DropPrimaryKey("PK_Comment", "Comment");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "Comment",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<long>(
                name: "ParentId",
                table: "Comment",
                nullable: true,
                defaultValue: 0L);

            migrationBuilder.AddPrimaryKey("PK_Comment", "Comment", "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_ParentId",
                table: "Comment",
                column: "ParentId");

           
            migrationBuilder.DropIndex(
                name: "IX_Comment_ParentId",
                table: "Comment");

         
            migrationBuilder.AddColumn<int>(
                name: "ToUserId",
                table: "Comment",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comment_ToUserId",
                table: "Comment",
                column: "ToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_AspNetUsers_ToUserId",
                table: "Comment",
                column: "ToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict,
                onUpdate: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropPrimaryKey("PK_Comment", "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Comment_ParentId",
                table: "Comment");

            migrationBuilder.DropIndex(
                name: "IX_Comment_ParentId",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Comment");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Comment",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Comment",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey("PK_Comment", "Comment", "Id");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_AspNetUsers_ToUserId",
                table: "Comment");


            migrationBuilder.DropIndex(
                name: "IX_Comment_ToUserId",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "ToUserId",
                table: "Comment");

        
        }
    }
}
