using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hse.Ratings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitAll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Programs_ProgramId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Programs_Faculties_FacultyId",
                table: "Programs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Programs",
                table: "Programs");

            migrationBuilder.RenameTable(
                name: "Programs",
                newName: "StudyPrograms");

            migrationBuilder.RenameIndex(
                name: "IX_Programs_FacultyId",
                table: "StudyPrograms",
                newName: "IX_StudyPrograms_FacultyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StudyPrograms",
                table: "StudyPrograms",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_StudyPrograms_ProgramId",
                table: "Courses",
                column: "ProgramId",
                principalTable: "StudyPrograms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudyPrograms_Faculties_FacultyId",
                table: "StudyPrograms",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_StudyPrograms_ProgramId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_StudyPrograms_Faculties_FacultyId",
                table: "StudyPrograms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StudyPrograms",
                table: "StudyPrograms");

            migrationBuilder.RenameTable(
                name: "StudyPrograms",
                newName: "Programs");

            migrationBuilder.RenameIndex(
                name: "IX_StudyPrograms_FacultyId",
                table: "Programs",
                newName: "IX_Programs_FacultyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Programs",
                table: "Programs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Programs_ProgramId",
                table: "Courses",
                column: "ProgramId",
                principalTable: "Programs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Programs_Faculties_FacultyId",
                table: "Programs",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
