using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hse.Ratings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixM2M : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseTeachers_Courses_CourseId",
                table: "CourseTeachers");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseTeachers_Teachers_TeacherId",
                table: "CourseTeachers");

            migrationBuilder.RenameColumn(
                name: "Loyalty",
                table: "TeacherReviews",
                newName: "Leniency");

            migrationBuilder.RenameColumn(
                name: "TeacherId",
                table: "CourseTeachers",
                newName: "TeachersId");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "CourseTeachers",
                newName: "CoursesId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseTeachers_TeacherId",
                table: "CourseTeachers",
                newName: "IX_CourseTeachers_TeachersId");

            migrationBuilder.RenameColumn(
                name: "Loyalty",
                table: "CourseReviews",
                newName: "Leniency");

            migrationBuilder.RenameColumn(
                name: "Interesting",
                table: "CourseReviews",
                newName: "Interest");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "TeacherReviews",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "TeacherReviews",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "CourseReviews",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "CourseReviews",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseTeachers_Courses_CoursesId",
                table: "CourseTeachers",
                column: "CoursesId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseTeachers_Teachers_TeachersId",
                table: "CourseTeachers",
                column: "TeachersId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseTeachers_Courses_CoursesId",
                table: "CourseTeachers");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseTeachers_Teachers_TeachersId",
                table: "CourseTeachers");

            migrationBuilder.RenameColumn(
                name: "Leniency",
                table: "TeacherReviews",
                newName: "Loyalty");

            migrationBuilder.RenameColumn(
                name: "TeachersId",
                table: "CourseTeachers",
                newName: "TeacherId");

            migrationBuilder.RenameColumn(
                name: "CoursesId",
                table: "CourseTeachers",
                newName: "CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseTeachers_TeachersId",
                table: "CourseTeachers",
                newName: "IX_CourseTeachers_TeacherId");

            migrationBuilder.RenameColumn(
                name: "Leniency",
                table: "CourseReviews",
                newName: "Loyalty");

            migrationBuilder.RenameColumn(
                name: "Interest",
                table: "CourseReviews",
                newName: "Interesting");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "TeacherReviews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "TeacherReviews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "CourseReviews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "CourseReviews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseTeachers_Courses_CourseId",
                table: "CourseTeachers",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseTeachers_Teachers_TeacherId",
                table: "CourseTeachers",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
