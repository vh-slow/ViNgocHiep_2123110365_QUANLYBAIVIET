using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViNgocHiep_2123110365.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Comments",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Books",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.CreateTable(
                name: "BookHistories",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    OldContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EditedByUserId = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookHistories_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_BookHistories_BookId",
                table: "BookHistories",
                column: "BookId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BookHistories");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Users");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Comments");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Books");
        }
    }
}
