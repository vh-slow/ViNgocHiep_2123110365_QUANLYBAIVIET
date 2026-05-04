using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViNgocHiep_2123110365.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "Users",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "datetime2",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Status", table: "Users");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Users");
        }
    }
}
