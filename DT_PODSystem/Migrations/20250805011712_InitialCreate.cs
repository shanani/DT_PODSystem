using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DT_PODSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequestUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffectedColumns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    SecurityLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsFinancialData = table.Column<bool>(type: "bit", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ColorCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "#A54EE1"),
                    IconClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "fa fa-folder"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeneralDirectorates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ManagerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralDirectorates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Queries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "1.0"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ExecutionPriority = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastExecutedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutionCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UploadedFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SavedFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsTemporary = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CommercialRegister = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ManagerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    GeneralDirectorateId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_GeneralDirectorates_GeneralDirectorateId",
                        column: x => x.GeneralDirectorateId,
                        principalTable: "GeneralDirectorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormulaCanvases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Width = table.Column<int>(type: "int", nullable: false, defaultValue: 1200),
                    Height = table.Column<int>(type: "int", nullable: false, defaultValue: 800),
                    ZoomLevel = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1.0m),
                    CanvasState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormulaExpression = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ValidationErrors = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastValidated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "1.0"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaCanvases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormulaCanvases_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueryConstants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueryId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DataType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsConstant = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    InputType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SelectOptions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SystemSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ValidationPattern = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ValidationMessage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryConstants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryConstants_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PdfTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NamingConvention = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsFinancialData = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ProcessingPriority = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdfTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PdfTemplates_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PdfTemplates_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PdfTemplates_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "QueryOutputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueryId = table.Column<int>(type: "int", nullable: false),
                    FormulaCanvasId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DataType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FormulaExpression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExecutionOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    InputDependencies = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, defaultValue: "[]"),
                    OutputDependencies = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, defaultValue: "[]"),
                    GlobalDependencies = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, defaultValue: "[]"),
                    LocalDependencies = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, defaultValue: "[]"),
                    FormatString = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, defaultValue: "N2"),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    CurrencySymbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ValidationErrors = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastValidated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IncludeInOutput = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryOutputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryOutputs_FormulaCanvases_FormulaCanvasId",
                        column: x => x.FormulaCanvasId,
                        principalTable: "FormulaCanvases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QueryOutputs_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FieldMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    X = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    Y = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    Width = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    Height = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ValidationPattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValidationMessage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UseOCR = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OCRLanguage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, defaultValue: "eng"),
                    OCRConfidenceThreshold = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.7m),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    BorderColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "#A54EE1"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldMappings_PdfTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "PdfTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    PeriodId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OrganizedFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    ProcessingMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NeedApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasFinancialInfo = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    AnchorConfidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnchorsFound = table.Column<int>(type: "int", nullable: false),
                    AnchorsConfigured = table.Column<int>(type: "int", nullable: false),
                    AnchorsMatched = table.Column<int>(type: "int", nullable: false),
                    AnchorDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PdfTemplateId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessedFiles_PdfTemplates_PdfTemplateId",
                        column: x => x.PdfTemplateId,
                        principalTable: "PdfTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProcessedFiles_PdfTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "PdfTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateAnchors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    X = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    Y = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    Width = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    Height = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    ReferenceText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReferencePattern = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ConfidenceThreshold = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.8m),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "#00C48C"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BorderColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "#00C48C"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAnchors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateAnchors_PdfTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "PdfTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    UploadedFileId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    PdfVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HasFormFields = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SavedFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateAttachments_PdfTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "PdfTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateAttachments_UploadedFiles_UploadedFileId",
                        column: x => x.UploadedFileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessedFileId = table.Column<int>(type: "int", nullable: false),
                    FieldMappingId = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OutputValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputDataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "String"),
                    CurrencySymbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: true),
                    ExtractionConfidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CalculationConfidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ValidationErrors = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessedFields_FieldMappings_FieldMappingId",
                        column: x => x.FieldMappingId,
                        principalTable: "FieldMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessedFields_ProcessedFiles_ProcessedFileId",
                        column: x => x.ProcessedFileId,
                        principalTable: "ProcessedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueryResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueryId = table.Column<int>(type: "int", nullable: false),
                    QueryOutputId = table.Column<int>(type: "int", nullable: false),
                    ProcessedFileId = table.Column<int>(type: "int", nullable: false),
                    PeriodId = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    OutputName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CalculatedValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputDataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "Number"),
                    OriginalFormula = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedFormula = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExecutionTimeMs = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CalculationConfidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ValidationErrors = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NeedApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasFinancialData = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryResults_ProcessedFiles_ProcessedFileId",
                        column: x => x.ProcessedFileId,
                        principalTable: "ProcessedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueryResults_Queries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "Queries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QueryResults_QueryOutputs_QueryOutputId",
                        column: x => x.QueryOutputId,
                        principalTable: "QueryOutputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "ColorCode", "CreatedBy", "CreatedDate", "Description", "DisplayOrder", "IconClass", "IsActive", "ModifiedBy", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, "#A54EE1", "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8372), "Financial and accounting related documents", 1, "fa fa-dollar-sign", true, null, null, "MSP Certificates" },
                    { 2, "#4F008C", "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8375), "Legal contracts and agreements", 2, "fa fa-balance-scale", true, null, null, "Financial Documents" },
                    { 3, "#00C48C", "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8377), "Human resources and personnel documents", 3, "fa fa-users", true, null, null, "HR Documents" },
                    { 4, "#1BCED8", "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8380), "Technical specifications and documentation", 4, "fa fa-cogs", true, null, null, "Technical Documents" }
                });

            migrationBuilder.InsertData(
                table: "GeneralDirectorates",
                columns: new[] { "Id", "Code", "ContactEmail", "ContactPhone", "CreatedBy", "CreatedDate", "Description", "DisplayOrder", "IsActive", "ManagerName", "ModifiedBy", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, "FA", null, null, "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8575), "Financial management and administrative services", 1, true, null, null, null, "Finance and Administration" },
                    { 2, "IT", null, null, "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8578), "Information technology services and infrastructure", 2, true, null, null, null, "Information Technology" },
                    { 3, "HR", null, null, "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8580), "Human resources and organizational development", 3, true, null, null, null, "Human Resources" },
                    { 4, "OPS", null, null, "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8582), "Operational activities and service delivery", 4, true, null, null, null, "Operations" }
                });

            migrationBuilder.InsertData(
                table: "Vendors",
                columns: new[] { "Id", "Address", "ApprovalDate", "ApprovedBy", "Code", "CommercialRegister", "CompanyName", "ContactEmail", "ContactPerson", "ContactPhone", "CreatedBy", "CreatedDate", "IsActive", "IsApproved", "ModifiedBy", "ModifiedDate", "Name", "TaxNumber" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2025, 7, 6, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8658), "System", "TSL001", "1010123456", "Nokia", "ahmed@nokia.sa", "Ahmed Al-Rashid", "+96650112367", "System", new DateTime(2025, 6, 21, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8672), true, true, null, null, "Nokia", "300123456789003" },
                    { 2, null, new DateTime(2025, 7, 21, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8676), "System", "GSC002", "1010234567", "Ericsson Company", "sali@Ericsson.sa", "Fatima Al-Zahra", "+966542345678", "System", new DateTime(2025, 7, 16, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8677), true, true, null, null, "Ericsson", "300234567890003" },
                    { 3, null, new DateTime(2025, 7, 21, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8680), "System", "GSC003", "1010234567", "Huawei Company", "fatima@Huawei.sa", "Fatima Al-Zahra", "+966509945678", "", new DateTime(2025, 7, 16, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8681), true, true, null, null, "Huawei", "300234567890003" },
                    { 4, null, new DateTime(2025, 7, 21, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8684), "System", "GSC004", "1010234567", "Cisco Company", "mohammad@cisco.sa", "Fatima Al-Zahra", "+966502340078", "", new DateTime(2025, 7, 16, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8684), true, true, null, null, "Cisco", "300234567890003" }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Code", "ContactEmail", "ContactPhone", "CreatedBy", "CreatedDate", "Description", "DisplayOrder", "GeneralDirectorateId", "IsActive", "ManagerName", "ModifiedBy", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, "ACC", null, null, "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8612), "Financial accounting and reporting", 1, 1, true, null, null, null, "TB" },
                    { 2, "PROC", null, null, "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8615), "Procurement and vendor management", 2, 1, true, null, null, null, "SOC" },
                    { 3, "DEV", null, null, "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8617), "Software development and maintenance", 1, 2, true, null, null, null, "NOC" },
                    { 4, "INFRA", null, null, "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8620), "IT infrastructure and security", 2, 2, true, null, null, null, "Infrastructure" },
                    { 5, "TM", null, null, "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8622), "Talent acquisition and development", 1, 3, true, null, null, null, "Fixed" },
                    { 6, "ER", null, null, "System", new DateTime(2025, 8, 5, 1, 17, 11, 460, DateTimeKind.Utc).AddTicks(8624), "Employee relations and compliance", 2, 3, true, null, null, null, "Transport" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_CreatedDate",
                table: "AuditLogs",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_IsActive",
                table: "AuditLogs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_DisplayOrder",
                table: "Categories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Category_CreatedDate",
                table: "Categories",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Category_IsActive",
                table: "Categories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Department_CreatedDate",
                table: "Departments",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Department_IsActive",
                table: "Departments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_GeneralDirectorateId_DisplayOrder",
                table: "Departments",
                columns: new[] { "GeneralDirectorateId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                table: "Departments",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMapping_CreatedDate",
                table: "FieldMappings",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMapping_IsActive",
                table: "FieldMappings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMappings_DataType",
                table: "FieldMappings",
                column: "DataType");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMappings_TemplateId_DisplayOrder",
                table: "FieldMappings",
                columns: new[] { "TemplateId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldMappings_TemplateId_FieldName",
                table: "FieldMappings",
                columns: new[] { "TemplateId", "FieldName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldMappings_TemplateId_PageNumber",
                table: "FieldMappings",
                columns: new[] { "TemplateId", "PageNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaCanvas_CreatedDate",
                table: "FormulaCanvases",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaCanvas_IsActive",
                table: "FormulaCanvases",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaCanvases_QueryId",
                table: "FormulaCanvases",
                column: "QueryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneralDirectorate_CreatedDate",
                table: "GeneralDirectorates",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralDirectorate_IsActive",
                table: "GeneralDirectorates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralDirectorates_Code",
                table: "GeneralDirectorates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneralDirectorates_DisplayOrder",
                table: "GeneralDirectorates",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralDirectorates_Name",
                table: "GeneralDirectorates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PdfTemplate_CreatedDate",
                table: "PdfTemplates",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_PdfTemplate_IsActive",
                table: "PdfTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PdfTemplates_CategoryId",
                table: "PdfTemplates",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PdfTemplates_DepartmentId",
                table: "PdfTemplates",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PdfTemplates_IsFinancialData",
                table: "PdfTemplates",
                column: "IsFinancialData");

            migrationBuilder.CreateIndex(
                name: "IX_PdfTemplates_Name",
                table: "PdfTemplates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PdfTemplates_Status",
                table: "PdfTemplates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PdfTemplates_VendorId",
                table: "PdfTemplates",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedField_CreatedDate",
                table: "ProcessedFields",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedField_IsActive",
                table: "ProcessedFields",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFields_FieldMappingId",
                table: "ProcessedFields",
                column: "FieldMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFields_FieldName",
                table: "ProcessedFields",
                column: "FieldName");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFields_IsValid",
                table: "ProcessedFields",
                column: "IsValid");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFields_OutputDataType",
                table: "ProcessedFields",
                column: "OutputDataType");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFields_ProcessedFileId",
                table: "ProcessedFields",
                column: "ProcessedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFile_CreatedDate",
                table: "ProcessedFiles",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFile_IsActive",
                table: "ProcessedFiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_HasFinancialInfo",
                table: "ProcessedFiles",
                column: "HasFinancialInfo");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_NeedApproval",
                table: "ProcessedFiles",
                column: "NeedApproval");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_PdfTemplateId",
                table: "ProcessedFiles",
                column: "PdfTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_PeriodId",
                table: "ProcessedFiles",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_ProcessedDate",
                table: "ProcessedFiles",
                column: "ProcessedDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_Status",
                table: "ProcessedFiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_TemplateId",
                table: "ProcessedFiles",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_TemplateId_PeriodId",
                table: "ProcessedFiles",
                columns: new[] { "TemplateId", "PeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_Queries_ExecutionPriority",
                table: "Queries",
                column: "ExecutionPriority");

            migrationBuilder.CreateIndex(
                name: "IX_Queries_Name",
                table: "Queries",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Queries_Status",
                table: "Queries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Query_CreatedDate",
                table: "Queries",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Query_IsActive",
                table: "Queries",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QueryConstant_CreatedDate",
                table: "QueryConstants",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_QueryConstant_IsActive",
                table: "QueryConstants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QueryConstants_DisplayOrder",
                table: "QueryConstants",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_QueryConstants_IsConstant",
                table: "QueryConstants",
                column: "IsConstant");

            migrationBuilder.CreateIndex(
                name: "IX_QueryConstants_IsGlobal",
                table: "QueryConstants",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_QueryConstants_QueryId_Name",
                table: "QueryConstants",
                columns: new[] { "QueryId", "Name" },
                unique: true,
                filter: "[QueryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QueryOutput_CreatedDate",
                table: "QueryOutputs",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_QueryOutput_IsActive",
                table: "QueryOutputs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QueryOutputs_FormulaCanvasId",
                table: "QueryOutputs",
                column: "FormulaCanvasId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryOutputs_IncludeInOutput",
                table: "QueryOutputs",
                column: "IncludeInOutput");

            migrationBuilder.CreateIndex(
                name: "IX_QueryOutputs_QueryId_ExecutionOrder",
                table: "QueryOutputs",
                columns: new[] { "QueryId", "ExecutionOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_QueryOutputs_QueryId_Name",
                table: "QueryOutputs",
                columns: new[] { "QueryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueryResult_CreatedDate",
                table: "QueryResults",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_QueryResult_IsActive",
                table: "QueryResults",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_ExecutedDate",
                table: "QueryResults",
                column: "ExecutedDate");

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_HasFinancialData",
                table: "QueryResults",
                column: "HasFinancialData");

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_IsApproved",
                table: "QueryResults",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_IsValid",
                table: "QueryResults",
                column: "IsValid");

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_NeedApproval",
                table: "QueryResults",
                column: "NeedApproval");

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_OutputName",
                table: "QueryResults",
                column: "OutputName");

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_PeriodId",
                table: "QueryResults",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_ProcessedFileId",
                table: "QueryResults",
                column: "ProcessedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_QueryId",
                table: "QueryResults",
                column: "QueryId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_QueryId_PeriodId",
                table: "QueryResults",
                columns: new[] { "QueryId", "PeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_QueryId_ProcessedFileId",
                table: "QueryResults",
                columns: new[] { "QueryId", "ProcessedFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_QueryResults_QueryOutputId",
                table: "QueryResults",
                column: "QueryOutputId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAnchor_CreatedDate",
                table: "TemplateAnchors",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAnchor_IsActive",
                table: "TemplateAnchors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAnchors_TemplateId",
                table: "TemplateAnchors",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAnchors_TemplateId_DisplayOrder",
                table: "TemplateAnchors",
                columns: new[] { "TemplateId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAnchors_TemplateId_Name",
                table: "TemplateAnchors",
                columns: new[] { "TemplateId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAnchors_TemplateId_PageNumber",
                table: "TemplateAnchors",
                columns: new[] { "TemplateId", "PageNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAttachment_CreatedDate",
                table: "TemplateAttachments",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAttachment_IsActive",
                table: "TemplateAttachments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAttachments_DisplayOrder",
                table: "TemplateAttachments",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAttachments_IsPrimary",
                table: "TemplateAttachments",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAttachments_TemplateId_UploadedFileId",
                table: "TemplateAttachments",
                columns: new[] { "TemplateId", "UploadedFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAttachments_Type",
                table: "TemplateAttachments",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAttachments_UploadedFileId",
                table: "TemplateAttachments",
                column: "UploadedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFile_CreatedDate",
                table: "UploadedFiles",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFile_IsActive",
                table: "UploadedFiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_FileHash",
                table: "UploadedFiles",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_IsTemporary",
                table: "UploadedFiles",
                column: "IsTemporary");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_OriginalFileName",
                table: "UploadedFiles",
                column: "OriginalFileName");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_SavedFileName",
                table: "UploadedFiles",
                column: "SavedFileName");

            migrationBuilder.CreateIndex(
                name: "IX_Vendor_CreatedDate",
                table: "Vendors",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Vendor_IsActive",
                table: "Vendors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_Code",
                table: "Vendors",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_CommercialRegister",
                table: "Vendors",
                column: "CommercialRegister");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_IsApproved",
                table: "Vendors",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_Name",
                table: "Vendors",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_TaxNumber",
                table: "Vendors",
                column: "TaxNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ProcessedFields");

            migrationBuilder.DropTable(
                name: "QueryConstants");

            migrationBuilder.DropTable(
                name: "QueryResults");

            migrationBuilder.DropTable(
                name: "TemplateAnchors");

            migrationBuilder.DropTable(
                name: "TemplateAttachments");

            migrationBuilder.DropTable(
                name: "FieldMappings");

            migrationBuilder.DropTable(
                name: "ProcessedFiles");

            migrationBuilder.DropTable(
                name: "QueryOutputs");

            migrationBuilder.DropTable(
                name: "UploadedFiles");

            migrationBuilder.DropTable(
                name: "PdfTemplates");

            migrationBuilder.DropTable(
                name: "FormulaCanvases");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "Queries");

            migrationBuilder.DropTable(
                name: "GeneralDirectorates");
        }
    }
}
