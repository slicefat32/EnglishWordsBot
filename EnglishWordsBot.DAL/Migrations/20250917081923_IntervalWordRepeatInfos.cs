using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishWordsBot.DAL.Migrations
{
    /// <inheritdoc />
    public partial class IntervalWordRepeatInfos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntervalWordRepeatInfos",
                columns: table => new
                {
                    WordInfoId = table.Column<int>(type: "int", nullable: false),
                    Repeatednterval = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntervalWordRepeatInfos", x => x.WordInfoId);
                    table.ForeignKey(
                        name: "FK_IntervalWordRepeatInfos_WordsInfo_WordInfoId",
                        column: x => x.WordInfoId,
                        principalTable: "WordsInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntervalWordRepeatInfos_Repeatednterval",
                table: "IntervalWordRepeatInfos",
                column: "Repeatednterval");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntervalWordRepeatInfos");
        }
    }
}
