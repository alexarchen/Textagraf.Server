using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SearchServer.Migrations
{
    public partial class payedsubscribescreated : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Group_AspNetUsers_CreatorId",
                table: "Group");

            migrationBuilder.AddColumn<long>(
                name: "Size",
                table: "Document",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "PayedSubscribe",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    DocumentId = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    StartTime = table.Column<DateTime>(nullable: false),
                    EndTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayedSubscribe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayedSubscribe_Document_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayedSubscribe_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayedSubscribe_DocumentId",
                table: "PayedSubscribe",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayedSubscribe_UserId",
                table: "PayedSubscribe",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Group_AspNetUsers_CreatorId",
                table: "Group",
                column: "CreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Group_AspNetUsers_CreatorId",
                table: "Group");

            migrationBuilder.DropTable(
                name: "PayedSubscribe");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Document");

            migrationBuilder.AddForeignKey(
                name: "FK_Group_AspNetUsers_CreatorId",
                table: "Group",
                column: "CreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
