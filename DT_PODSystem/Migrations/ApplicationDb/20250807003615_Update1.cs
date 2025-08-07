using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DT_PODSystem.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class Update1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "PdfTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9094));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9099));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9103));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9106));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9110));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9449));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9454));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9458));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9463));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9466));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9578));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9582));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9586));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9589));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9593));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9598));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9601));

            migrationBuilder.UpdateData(
                table: "GeneralDirectorates",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9384));

            migrationBuilder.UpdateData(
                table: "GeneralDirectorates",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9389));

            migrationBuilder.UpdateData(
                table: "GeneralDirectorates",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9392));

            migrationBuilder.UpdateData(
                table: "GeneralDirectorates",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9395));

            migrationBuilder.UpdateData(
                table: "GeneralDirectorates",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9398));

            migrationBuilder.UpdateData(
                table: "PODAttachments",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovalDate", "CreatedDate", "DocumentDate" },
                values: new object[] { new DateTime(2025, 6, 10, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(68), new DateTime(2025, 6, 8, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(70), new DateTime(2025, 6, 8, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(63) });

            migrationBuilder.UpdateData(
                table: "PODAttachments",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedDate", "DocumentDate" },
                values: new object[] { new DateTime(2025, 7, 23, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(78), new DateTime(2025, 7, 23, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(75) });

            migrationBuilder.UpdateData(
                table: "PODs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovalDate", "CreatedDate", "LastProcessedDate" },
                values: new object[] { new DateTime(2025, 7, 8, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9853), new DateTime(2025, 6, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9865), new DateTime(2025, 8, 2, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9863) });

            migrationBuilder.UpdateData(
                table: "PODs",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApprovalDate", "CreatedDate", "LastProcessedDate" },
                values: new object[] { new DateTime(2025, 6, 23, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9881), new DateTime(2025, 5, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9889), new DateTime(2025, 8, 5, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9887) });

            migrationBuilder.UpdateData(
                table: "PODs",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 23, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9902));

            migrationBuilder.UpdateData(
                table: "PODs",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 31, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9924));

            migrationBuilder.UpdateData(
                table: "PODs",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ApprovalDate", "CreatedDate", "LastProcessedDate" },
                values: new object[] { new DateTime(2025, 7, 18, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9945), new DateTime(2025, 7, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9952), new DateTime(2025, 7, 28, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9950) });

            migrationBuilder.UpdateData(
                table: "PdfTemplates",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovalDate", "CreatedDate", "LastProcessedDate", "Title" },
                values: new object[] { new DateTime(2025, 7, 8, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(128), new DateTime(2025, 7, 8, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(133), new DateTime(2025, 8, 2, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(130), "Untitled Template" });

            migrationBuilder.UpdateData(
                table: "PdfTemplates",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApprovalDate", "CreatedDate", "LastProcessedDate", "Title" },
                values: new object[] { new DateTime(2025, 6, 23, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(220), new DateTime(2025, 6, 23, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(225), new DateTime(2025, 8, 5, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(222), "Untitled Template" });

            migrationBuilder.UpdateData(
                table: "PdfTemplates",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedDate", "Title" },
                values: new object[] { new DateTime(2025, 7, 28, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(231), "Untitled Template" });

            migrationBuilder.UpdateData(
                table: "TemplateAttachments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 6, 23, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(285));

            migrationBuilder.UpdateData(
                table: "UploadedFiles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedDate", "ProcessedDate" },
                values: new object[] { new DateTime(2025, 6, 8, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(5), new DateTime(2025, 6, 8, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(1) });

            migrationBuilder.UpdateData(
                table: "UploadedFiles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedDate", "ProcessedDate" },
                values: new object[] { new DateTime(2025, 6, 23, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(15), new DateTime(2025, 6, 23, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(12) });

            migrationBuilder.UpdateData(
                table: "UploadedFiles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedDate", "ProcessedDate" },
                values: new object[] { new DateTime(2025, 7, 23, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(23), new DateTime(2025, 7, 23, 0, 36, 13, 750, DateTimeKind.Utc).AddTicks(19) });

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovalDate", "CreatedDate" },
                values: new object[] { new DateTime(2025, 2, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9659), new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9676) });

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApprovalDate", "CreatedDate" },
                values: new object[] { new DateTime(2025, 4, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9682), new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9684) });

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ApprovalDate", "CreatedDate" },
                values: new object[] { new DateTime(2025, 6, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9689), new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9691) });

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9696));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ApprovalDate", "CreatedDate" },
                values: new object[] { new DateTime(2025, 7, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9699), new DateTime(2025, 8, 7, 0, 36, 13, 749, DateTimeKind.Utc).AddTicks(9701) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "PdfTemplates");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 745, DateTimeKind.Utc).AddTicks(9786));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 745, DateTimeKind.Utc).AddTicks(9789));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 745, DateTimeKind.Utc).AddTicks(9791));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 745, DateTimeKind.Utc).AddTicks(9792));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 745, DateTimeKind.Utc).AddTicks(9794));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(130));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(132));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(134));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(136));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(137));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(139));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(141));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(142));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(144));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(146));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(149));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(151));

            migrationBuilder.UpdateData(
                table: "GeneralDirectorates",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(94));

            migrationBuilder.UpdateData(
                table: "GeneralDirectorates",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(96));

            migrationBuilder.UpdateData(
                table: "GeneralDirectorates",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(97));

            migrationBuilder.UpdateData(
                table: "GeneralDirectorates",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(99));

            migrationBuilder.UpdateData(
                table: "GeneralDirectorates",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(100));

            migrationBuilder.UpdateData(
                table: "PODAttachments",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovalDate", "CreatedDate", "DocumentDate" },
                values: new object[] { new DateTime(2025, 6, 9, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(623), new DateTime(2025, 6, 7, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(624), new DateTime(2025, 6, 7, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(621) });

            migrationBuilder.UpdateData(
                table: "PODAttachments",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedDate", "DocumentDate" },
                values: new object[] { new DateTime(2025, 7, 22, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(628), new DateTime(2025, 7, 22, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(626) });

            migrationBuilder.UpdateData(
                table: "PODs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovalDate", "CreatedDate", "LastProcessedDate" },
                values: new object[] { new DateTime(2025, 7, 7, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(497), new DateTime(2025, 6, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(506), new DateTime(2025, 8, 1, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(505) });

            migrationBuilder.UpdateData(
                table: "PODs",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApprovalDate", "CreatedDate", "LastProcessedDate" },
                values: new object[] { new DateTime(2025, 6, 22, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(513), new DateTime(2025, 5, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(516), new DateTime(2025, 8, 4, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(515) });

            migrationBuilder.UpdateData(
                table: "PODs",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 22, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(521));

            migrationBuilder.UpdateData(
                table: "PODs",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 30, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(530));

            migrationBuilder.UpdateData(
                table: "PODs",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ApprovalDate", "CreatedDate", "LastProcessedDate" },
                values: new object[] { new DateTime(2025, 7, 17, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(540), new DateTime(2025, 7, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(542), new DateTime(2025, 7, 27, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(542) });

            migrationBuilder.UpdateData(
                table: "PdfTemplates",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovalDate", "CreatedDate", "LastProcessedDate" },
                values: new object[] { new DateTime(2025, 7, 7, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(661), new DateTime(2025, 7, 7, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(663), new DateTime(2025, 8, 1, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(662) });

            migrationBuilder.UpdateData(
                table: "PdfTemplates",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApprovalDate", "CreatedDate", "LastProcessedDate" },
                values: new object[] { new DateTime(2025, 6, 22, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(665), new DateTime(2025, 6, 22, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(667), new DateTime(2025, 8, 4, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(666) });

            migrationBuilder.UpdateData(
                table: "PdfTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 7, 27, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(670));

            migrationBuilder.UpdateData(
                table: "TemplateAttachments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 6, 22, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(706));

            migrationBuilder.UpdateData(
                table: "UploadedFiles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedDate", "ProcessedDate" },
                values: new object[] { new DateTime(2025, 6, 7, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(588), new DateTime(2025, 6, 7, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(586) });

            migrationBuilder.UpdateData(
                table: "UploadedFiles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedDate", "ProcessedDate" },
                values: new object[] { new DateTime(2025, 6, 22, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(592), new DateTime(2025, 6, 22, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(591) });

            migrationBuilder.UpdateData(
                table: "UploadedFiles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedDate", "ProcessedDate" },
                values: new object[] { new DateTime(2025, 7, 22, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(595), new DateTime(2025, 7, 22, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(594) });

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovalDate", "CreatedDate" },
                values: new object[] { new DateTime(2025, 2, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(209), new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(222) });

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApprovalDate", "CreatedDate" },
                values: new object[] { new DateTime(2025, 4, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(224), new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(226) });

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ApprovalDate", "CreatedDate" },
                values: new object[] { new DateTime(2025, 6, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(227), new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(228) });

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(230));

            migrationBuilder.UpdateData(
                table: "Vendors",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ApprovalDate", "CreatedDate" },
                values: new object[] { new DateTime(2025, 7, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(232), new DateTime(2025, 8, 6, 22, 56, 26, 746, DateTimeKind.Utc).AddTicks(233) });
        }
    }
}
