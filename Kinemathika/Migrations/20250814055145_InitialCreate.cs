using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinemathika.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Classrooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classrooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Worksheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConceptId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorksheetId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worksheets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttemptRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    session_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    concept_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    worksheet_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    problem_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ended_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ended_status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    first_attempt_correct = table.Column<bool>(type: "bit", nullable: false),
                    attempts_to_correct = table.Column<int>(type: "int", nullable: false),
                    level_attempt_accuracy = table.Column<float>(type: "real", nullable: false),
                    time_to_correct_ms = table.Column<int>(type: "int", nullable: false),
                    mastery_valid = table.Column<bool>(type: "bit", nullable: false),
                    StudentDbId = table.Column<int>(type: "int", nullable: true),
                    StudentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttemptRecords_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    ClassroomId = table.Column<int>(type: "int", nullable: false),
                    StudentDbId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => new { x.ClassroomId, x.StudentDbId });
                    table.ForeignKey(
                        name: "FK_Enrollments_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrollments_Students_StudentDbId",
                        column: x => x.StudentDbId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Problems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProblemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorksheetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Problems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Problems_Worksheets_WorksheetId",
                        column: x => x.WorksheetId,
                        principalTable: "Worksheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttemptRecords_concept_id",
                table: "AttemptRecords",
                column: "concept_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptRecords_ended_at",
                table: "AttemptRecords",
                column: "ended_at");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptRecords_StudentId",
                table: "AttemptRecords",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentDbId",
                table: "Enrollments",
                column: "StudentDbId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_ProblemId",
                table: "Problems",
                column: "ProblemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Problems_WorksheetId",
                table: "Problems",
                column: "WorksheetId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentId",
                table: "Students",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Worksheets_ConceptId_WorksheetId",
                table: "Worksheets",
                columns: new[] { "ConceptId", "WorksheetId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttemptRecords");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "Problems");

            migrationBuilder.DropTable(
                name: "Classrooms");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Worksheets");
        }
    }
}
