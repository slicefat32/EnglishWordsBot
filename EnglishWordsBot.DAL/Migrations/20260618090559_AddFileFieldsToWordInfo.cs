using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishWordsBot.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddFileFieldsToWordInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FileData",
                table: "WordsInfo",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "WordsInfo",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileData",
                table: "WordsInfo");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "WordsInfo");
        }
    }
}
