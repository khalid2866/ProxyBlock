using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProxyBlock.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJoinCode_AddPendingEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sections_JoinCode",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "JoinCode",
                table: "Sections");

            migrationBuilder.CreateTable(
                name: "PendingEnrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseSectionId = table.Column<int>(type: "int", nullable: false),
                    RollNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingEnrollments_Sections_CourseSectionId",
                        column: x => x.CourseSectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingEnrollments_CourseSectionId",
                table: "PendingEnrollments",
                column: "CourseSectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingEnrollments");

            migrationBuilder.AddColumn<string>(
                name: "JoinCode",
                table: "Sections",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_JoinCode",
                table: "Sections",
                column: "JoinCode",
                unique: true);
        }
    }
}
