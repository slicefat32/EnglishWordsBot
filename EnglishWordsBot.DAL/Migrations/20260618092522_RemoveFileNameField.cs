using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishWordsBot.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFileNameField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "WordsInfo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "WordsInfo",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
