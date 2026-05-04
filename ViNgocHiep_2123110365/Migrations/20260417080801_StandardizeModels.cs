using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViNgocHiep_2123110365.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SavedAt",
                table: "Favorites",
                newName: "CreatedAt"
            );

            migrationBuilder.RenameColumn(
                name: "EditedAt",
                table: "BookHistories",
                newName: "CreatedAt"
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Categories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Categories",
                type: "datetime2",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreatedAt", table: "Categories");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Categories");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Categories");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Favorites",
                newName: "SavedAt"
            );

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "BookHistories",
                newName: "EditedAt"
            );
        }
    }
}
