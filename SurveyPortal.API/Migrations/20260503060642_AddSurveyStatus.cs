using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SurveyResponses");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Surveys",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Surveys");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Surveys",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Surveys",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "SurveyResponses",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
