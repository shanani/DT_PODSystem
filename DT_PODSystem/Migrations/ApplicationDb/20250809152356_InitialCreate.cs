using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DT_PODSystem.Migrations.ApplicationDb
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
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                name: "PODs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PODCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValueSql: "NEWID()"),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PONumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContractNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    AutomationStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Frequency = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    VendorSPOCUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GovernorSPOCUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FinanceSPOCUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_PODs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PODs_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PODs_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PODs_Vendors_VendorId",
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
                name: "PdfTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PODId = table.Column<int>(type: "int", nullable: false),
                    UploadedFileId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NamingConvention = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessingPriority = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TechnicalNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HasFormFields = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ExpectedPdfVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpectedPageCount = table.Column<int>(type: "int", nullable: true),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    PdfVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastProcessed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: true),
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
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PdfTemplates_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PdfTemplates_PODs_PODId",
                        column: x => x.PODId,
                        principalTable: "PODs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PdfTemplates_UploadedFiles_UploadedFileId",
                        column: x => x.UploadedFileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PdfTemplates_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PODAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PODId = table.Column<int>(type: "int", nullable: false),
                    UploadedFileId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocumentVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "Active"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PODAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PODAttachments_PODs_PODId",
                        column: x => x.PODId,
                        principalTable: "PODs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PODAttachments_UploadedFiles_UploadedFileId",
                        column: x => x.UploadedFileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PODEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PODId = table.Column<int>(type: "int", nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "single"),
                    EntryOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EntryData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PODEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PODEntries_PODs_PODId",
                        column: x => x.PODId,
                        principalTable: "PODs",
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
                    { 1, "#A54EE1", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(5874), "Financial and accounting related documents", 1, "fa fa-dollar-sign", true, null, null, "MSP Certificates" },
                    { 2, "#4F008C", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(5877), "Legal contracts and agreements", 2, "fa fa-balance-scale", true, null, null, "Financial Documents" },
                    { 3, "#00C48C", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(5878), "Human resources and personnel documents", 3, "fa fa-users", true, null, null, "HR Documents" },
                    { 4, "#FF6B6B", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(5880), "Legal contracts, agreements, and compliance documents", 4, "fa fa-gavel", true, null, null, "Legal Documents" },
                    { 5, "#4ECDC4", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(5881), "Purchase orders, contracts, and procurement related documents", 5, "fa fa-shopping-cart", true, null, null, "Procurement Documents" }
                });

            migrationBuilder.InsertData(
                table: "GeneralDirectorates",
                columns: new[] { "Id", "ContactEmail", "ContactPhone", "CreatedBy", "CreatedDate", "Description", "DisplayOrder", "IsActive", "ManagerName", "ModifiedBy", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, null, null, "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6024), "IT systems, software, and technology services", 1, true, null, null, null, "Information Technology" },
                    { 2, null, null, "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6026), "Financial operations, budgeting, and administrative services", 2, true, null, null, null, "Finance and Administration" },
                    { 3, null, null, "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6027), "Personnel management, training, and development", 3, true, null, null, null, "Human Resources" },
                    { 4, null, null, "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6029), "Operational activities and facility maintenance", 4, true, null, null, null, "Operations and Maintenance" },
                    { 5, null, null, "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6030), "Legal affairs, regulatory compliance, and risk management", 5, true, null, null, null, "Legal and Compliance" }
                });

            migrationBuilder.InsertData(
                table: "UploadedFiles",
                columns: new[] { "Id", "ContentType", "CreatedBy", "CreatedDate", "ExpiryDate", "FileHash", "FilePath", "FileSize", "IsActive", "MimeType", "ModifiedBy", "ModifiedDate", "OriginalFileName", "ProcessedBy", "ProcessedDate", "SavedFileName", "UploadSource" },
                values: new object[,]
                {
                    { 1, "application/pdf", "System", new DateTime(2025, 6, 10, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6314), null, "sha256_erp_contract_hash_001", "/uploads/documents/2025/01/erp_contract_20250101_001.pdf", 2456789L, true, "application/pdf", null, null, "ERP_Contract_Main.pdf", "System", new DateTime(2025, 6, 10, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6312), "erp_contract_20250101_001.pdf", "POD" },
                    { 2, "application/pdf", "System", new DateTime(2025, 6, 25, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6317), null, "sha256_fin_template_hash_001", "/uploads/templates/2025/01/fin_report_template_20250102_001.pdf", 1234567L, true, "application/pdf", null, null, "Financial_Report_Template.pdf", "System", new DateTime(2025, 6, 25, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6316), "fin_report_template_20250102_001.pdf", "Wizard" },
                    { 3, "application/pdf", "System", new DateTime(2025, 7, 25, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6320), null, "sha256_hr_policy_hash_001", "/uploads/documents/2025/01/hr_policy_20250103_001.pdf", 987654L, true, "application/pdf", null, null, "HR_Policy_Document.pdf", "System", new DateTime(2025, 7, 25, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6319), "hr_policy_20250103_001.pdf", "POD" }
                });

            migrationBuilder.InsertData(
                table: "Vendors",
                columns: new[] { "Id", "Address", "ApprovalDate", "ApprovedBy", "CommercialRegister", "CompanyName", "ContactEmail", "ContactPerson", "ContactPhone", "CreatedBy", "CreatedDate", "IsActive", "IsApproved", "ModifiedBy", "ModifiedDate", "Name", "TaxNumber" },
                values: new object[,]
                {
                    { 1, "King Fahd Road, Riyadh 12345, Saudi Arabia", new DateTime(2025, 2, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6099), "System Admin", "1010123456", "STS Co. Ltd.", "ahmad@sts.sa", "Ahmad Al-Riyadh", "+966-11-2345678", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6120), true, true, null, null, "Saudi Technology Solutions", "300012345600003" },
                    { 2, "Olaya District, Riyadh 11564, Saudi Arabia", new DateTime(2025, 4, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6123), "Procurement Manager", "1010234567", "GCG International", "mariam@gcg.com", "Mariam Al-Khalil", "+966-11-3456789", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6124), true, true, null, null, "Gulf Consulting Group", "300012345600004" },
                    { 3, "King Abdullah Financial District, Riyadh 13519, Saudi Arabia", new DateTime(2025, 6, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6126), "IT Director", "1010345678", "DTP Solutions LLC", "mohammed@dtp.sa", "Mohammed Al-Faisal", "+966-11-4567890", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6127), true, true, null, null, "Digital Transformation Partners", "300012345600005" }
                });

            migrationBuilder.InsertData(
                table: "Vendors",
                columns: new[] { "Id", "Address", "ApprovalDate", "ApprovedBy", "CommercialRegister", "CompanyName", "ContactEmail", "ContactPerson", "ContactPhone", "CreatedBy", "CreatedDate", "IsActive", "ModifiedBy", "ModifiedDate", "Name", "TaxNumber" },
                values: new object[] { 4, "Al-Malaz District, Riyadh 11432, Saudi Arabia", null, null, "1010456789", "ABS Company", "aisha@abs.sa", "Aisha Al-Mutairi", "+966-11-5678901", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6129), true, null, null, "Arabian Business Services", "300012345600006" });

            migrationBuilder.InsertData(
                table: "Vendors",
                columns: new[] { "Id", "Address", "ApprovalDate", "ApprovedBy", "CommercialRegister", "CompanyName", "ContactEmail", "ContactPerson", "ContactPhone", "CreatedBy", "CreatedDate", "IsActive", "IsApproved", "ModifiedBy", "ModifiedDate", "Name", "TaxNumber" },
                values: new object[] { 5, "Diplomatic Quarter, Riyadh 11693, Saudi Arabia", new DateTime(2025, 7, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6131), "Finance Director", "1010567890", "AAC Saudi Arabia", "hassan@aac.sa", "Hassan Al-Zahrani", "+966-11-6789012", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6132), true, true, null, null, "Advanced Analytics Corp", "300012345600007" });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "ContactEmail", "ContactPhone", "CreatedBy", "CreatedDate", "Description", "DisplayOrder", "GeneralDirectorateId", "IsActive", "ManagerName", "ModifiedBy", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, "ahmed.rashid@company.sa", "+966-11-1234567", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6050), "Application development and maintenance", 1, 1, true, "Ahmed Al-Rashid", null, null, "Software Development" },
                    { 2, "sara.mahmoud@company.sa", "+966-11-1234568", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6053), "IT infrastructure, networking, and security", 2, 1, true, "Sara Al-Mahmoud", null, null, "Infrastructure and Networks" },
                    { 3, "omar.fahad@company.sa", "+966-11-1234569", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6054), "Business intelligence and data analysis", 3, 1, true, "Omar Al-Fahad", null, null, "Data Analytics" },
                    { 4, "fatima.zahra@company.sa", "+966-11-1234570", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6056), "Budget planning and financial forecasting", 1, 2, true, "Fatima Al-Zahra", null, null, "Financial Planning" },
                    { 5, "khalid.otaibi@company.sa", "+966-11-1234571", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6059), "Vendor payments and expense management", 2, 2, true, "Khalid Al-Otaibi", null, null, "Accounts Payable" },
                    { 6, "noura.saud@company.sa", "+966-11-1234572", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6061), "Cash management and financial investments", 3, 2, true, "Noura Al-Saud", null, null, "Treasury" },
                    { 7, "maha.ghamdi@company.sa", "+966-11-1234573", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6062), "Recruitment and onboarding", 1, 3, true, "Maha Al-Ghamdi", null, null, "Talent Acquisition" },
                    { 8, "ibrahim.harbi@company.sa", "+966-11-1234574", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6064), "Training and professional development", 2, 3, true, "Ibrahim Al-Harbi", null, null, "Employee Development" },
                    { 9, "abdullah.mutairi@company.sa", "+966-11-1234575", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6065), "Building maintenance and facility operations", 1, 4, true, "Abdullah Al-Mutairi", null, null, "Facility Management" },
                    { 10, "reem.johani@company.sa", "+966-11-1234576", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6067), "Physical and information security", 2, 4, true, "Reem Al-Johani", null, null, "Security Operations" },
                    { 11, "yousef.dosari@company.sa", "+966-11-1234577", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6069), "Contract negotiation and management", 1, 5, true, "Yousef Al-Dosari", null, null, "Contract Management" },
                    { 12, "layla.shammari@company.sa", "+966-11-1234578", "System", new DateTime(2025, 8, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6070), "Regulatory compliance and auditing", 2, 5, true, "Layla Al-Shammari", null, null, "Regulatory Compliance" }
                });

            migrationBuilder.InsertData(
                table: "PODs",
                columns: new[] { "Id", "ApprovalDate", "ApprovedBy", "AutomationStatus", "CategoryId", "ContractNumber", "CreatedBy", "CreatedDate", "DepartmentId", "Description", "FinanceSPOCUsername", "Frequency", "GovernorSPOCUsername", "IsActive", "IsFinancialData", "LastProcessedDate", "ModifiedBy", "ModifiedDate", "Name", "PODCode", "PONumber", "ProcessedCount", "ProcessingPriority", "RequiresApproval", "Status", "VendorId", "VendorSPOCUsername", "Version" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 7, 10, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6226), "IT Director", 3, 1, "CTR-ERP-2025", "System", new DateTime(2025, 6, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6236), 1, "Complete ERP system implementation for financial and operational modules", "fatima.finance", 1, "sara.governor", true, true, new DateTime(2025, 8, 4, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6235), null, null, "ERP System Implementation", "POD-ERP-2025-001", "PO-2025-IT-001", 15, 8, true, 4, 1, "ahmad.vendor", "1.0" },
                    { 2, new DateTime(2025, 6, 25, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6243), "Finance Director", 1, 2, "CTR-FIN-2025-A", "System", new DateTime(2025, 5, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6245), 4, "Automated processing of monthly financial reports and compliance documents", "noura.finance", 1, "khalid.governor", true, true, new DateTime(2025, 8, 7, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6245), null, null, "Financial Reporting Automation", "POD-FIN-2025-002", "PO-2025-FIN-002", 8, 9, true, 4, 2, "mariam.vendor", "1.2" }
                });

            migrationBuilder.InsertData(
                table: "PODs",
                columns: new[] { "Id", "ApprovalDate", "ApprovedBy", "AutomationStatus", "CategoryId", "ContractNumber", "CreatedBy", "CreatedDate", "DepartmentId", "Description", "FinanceSPOCUsername", "Frequency", "GovernorSPOCUsername", "IsActive", "LastProcessedDate", "ModifiedBy", "ModifiedDate", "Name", "PODCode", "PONumber", "ProcessingPriority", "RequiresApproval", "Status", "VendorId", "VendorSPOCUsername", "Version" },
                values: new object[] { 3, null, null, 2, 3, null, "System", new DateTime(2025, 7, 25, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6251), 7, "Employee performance reviews and development tracking system", "fatima.finance", 2, "maha.governor", true, null, null, null, "HR Performance Management", "POD-HR-2025-003", null, 6, true, 2, 3, "mohammed.vendor", "1.0" });

            migrationBuilder.InsertData(
                table: "PODs",
                columns: new[] { "Id", "ApprovalDate", "ApprovedBy", "AutomationStatus", "CategoryId", "ContractNumber", "CreatedBy", "CreatedDate", "DepartmentId", "Description", "FinanceSPOCUsername", "Frequency", "GovernorSPOCUsername", "IsActive", "IsFinancialData", "LastProcessedDate", "ModifiedBy", "ModifiedDate", "Name", "PODCode", "PONumber", "ProcessingPriority", "Status", "VendorId", "VendorSPOCUsername", "Version" },
                values: new object[] { 4, null, null, 1, 5, "CTR-MAINT-2025", "System", new DateTime(2025, 8, 2, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6259), 9, "Processing of facility maintenance requests and contract compliance", "khalid.finance", 1, "abdullah.governor", true, true, null, null, null, "Facility Maintenance Contracts", "POD-OPS-2025-004", "PO-2025-OPS-004", 4, 1, null, null, "1.0" });

            migrationBuilder.InsertData(
                table: "PODs",
                columns: new[] { "Id", "ApprovalDate", "ApprovedBy", "AutomationStatus", "CategoryId", "ContractNumber", "CreatedBy", "CreatedDate", "DepartmentId", "Description", "FinanceSPOCUsername", "Frequency", "GovernorSPOCUsername", "IsActive", "IsFinancialData", "LastProcessedDate", "ModifiedBy", "ModifiedDate", "Name", "PODCode", "PONumber", "ProcessedCount", "ProcessingPriority", "RequiresApproval", "Status", "VendorId", "VendorSPOCUsername", "Version" },
                values: new object[] { 5, new DateTime(2025, 7, 20, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6267), "Legal Director", 3, 4, null, "System", new DateTime(2025, 7, 9, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6269), 12, "Regulatory compliance reports and legal documentation processing", "noura.finance", 2, "layla.governor", true, true, new DateTime(2025, 7, 30, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6269), null, null, "Legal Compliance Reporting", "POD-LEG-2025-005", null, 3, 7, true, 4, 5, "hassan.vendor", "1.1" });

            migrationBuilder.InsertData(
                table: "PODAttachments",
                columns: new[] { "Id", "ApprovalDate", "ApprovalNotes", "ApprovedBy", "CreatedBy", "CreatedDate", "Description", "DisplayName", "DisplayOrder", "DocumentDate", "DocumentNumber", "DocumentStatus", "DocumentVersion", "ExpiryDate", "IsActive", "IsPrimary", "IssuedBy", "ModifiedBy", "ModifiedDate", "PODId", "RequiresApproval", "Type", "UploadedFileId" },
                values: new object[] { 1, new DateTime(2025, 6, 12, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6342), null, "Legal Director", "System", new DateTime(2025, 6, 10, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6343), "Primary contract document for ERP system implementation", "Main ERP Implementation Contract", 1, new DateTime(2025, 6, 10, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6340), "CTR-ERP-2025-001", "Active", "1.0", null, true, true, "Legal Department", null, null, 1, true, 1, 1 });

            migrationBuilder.InsertData(
                table: "PODAttachments",
                columns: new[] { "Id", "ApprovalDate", "ApprovalNotes", "ApprovedBy", "CreatedBy", "CreatedDate", "Description", "DisplayName", "DisplayOrder", "DocumentDate", "DocumentNumber", "DocumentStatus", "DocumentVersion", "ExpiryDate", "IsActive", "IsPrimary", "IssuedBy", "ModifiedBy", "ModifiedDate", "PODId", "Type", "UploadedFileId" },
                values: new object[] { 2, null, null, null, "System", new DateTime(2025, 7, 25, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6346), "Standard operating procedures for HR performance management", "HR Policy and Procedures", 1, new DateTime(2025, 7, 25, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6345), "SOP-HR-2025-001", "Active", "2.1", null, true, true, "HR Department", null, null, 3, 3, 3 });

            migrationBuilder.InsertData(
                table: "PdfTemplates",
                columns: new[] { "Id", "ApprovalDate", "ApprovedBy", "CategoryId", "CreatedBy", "CreatedDate", "DepartmentId", "ExpectedPageCount", "ExpectedPdfVersion", "IsActive", "LastProcessed", "LastProcessedDate", "ModifiedBy", "ModifiedDate", "NamingConvention", "PODId", "PageCount", "PdfVersion", "ProcessedCount", "ProcessingPriority", "ProcessingStatus", "Status", "TechnicalNotes", "Title", "UploadedFileId", "VendorId", "Version" },
                values: new object[] { 1, new DateTime(2025, 7, 10, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6367), "IT Director", null, "System", new DateTime(2025, 7, 10, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6370), null, 3, "1.7", true, null, new DateTime(2025, 8, 4, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6368), null, null, "ERP_INV_{YYYYMM}", 1, 3, "1.7", 15, 8, "Active", 2, "Requires OCR preprocessing for invoice amounts", "ERP Invoice Processing Template", 2, null, "1.2" });

            migrationBuilder.InsertData(
                table: "PdfTemplates",
                columns: new[] { "Id", "ApprovalDate", "ApprovedBy", "CategoryId", "CreatedBy", "CreatedDate", "DepartmentId", "ExpectedPageCount", "ExpectedPdfVersion", "HasFormFields", "IsActive", "LastProcessed", "LastProcessedDate", "ModifiedBy", "ModifiedDate", "NamingConvention", "PODId", "PageCount", "PdfVersion", "ProcessedCount", "ProcessingPriority", "ProcessingStatus", "Status", "TechnicalNotes", "Title", "UploadedFileId", "VendorId", "Version" },
                values: new object[] { 2, new DateTime(2025, 6, 25, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6373), "Finance Director", null, "System", new DateTime(2025, 6, 25, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6374), null, 5, "1.6", true, true, null, new DateTime(2025, 8, 7, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6373), null, null, "FIN_RPT_{YYYYMM}_{DD}", 2, null, null, 8, 9, null, 2, "Multi-page template with dynamic table extraction", "Financial Report Template", null, null, "2.0" });

            migrationBuilder.InsertData(
                table: "PdfTemplates",
                columns: new[] { "Id", "ApprovalDate", "ApprovedBy", "CategoryId", "CreatedBy", "CreatedDate", "DepartmentId", "ExpectedPageCount", "ExpectedPdfVersion", "HasFormFields", "IsActive", "LastProcessed", "LastProcessedDate", "ModifiedBy", "ModifiedDate", "NamingConvention", "PODId", "PageCount", "PdfVersion", "ProcessingPriority", "ProcessingStatus", "TechnicalNotes", "Title", "UploadedFileId", "VendorId", "Version" },
                values: new object[] { 3, null, null, null, "System", new DateTime(2025, 7, 30, 15, 23, 56, 174, DateTimeKind.Utc).AddTicks(6377), null, 8, "1.7", true, true, null, null, null, null, "LEG_COMP_{YYYY}Q{Q}", 5, null, null, 7, null, "Quarterly compliance template with signature verification", "Legal Compliance Template", null, null, "1.0" });

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
                name: "IX_PdfTemplates_NamingConvention",
                table: "PdfTemplates",
                column: "NamingConvention");

            migrationBuilder.CreateIndex(
                name: "IX_PdfTemplates_PODId",
                table: "PdfTemplates",
                column: "PODId");

            migrationBuilder.CreateIndex(
                name: "IX_PdfTemplates_Status",
                table: "PdfTemplates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PdfTemplates_UploadedFileId",
                table: "PdfTemplates",
                column: "UploadedFileId",
                unique: true,
                filter: "[UploadedFileId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PdfTemplates_VendorId",
                table: "PdfTemplates",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_PODAttachment_CreatedDate",
                table: "PODAttachments",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_PODAttachment_IsActive",
                table: "PODAttachments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PODAttachments_DisplayOrder",
                table: "PODAttachments",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_PODAttachments_DocumentDate",
                table: "PODAttachments",
                column: "DocumentDate");

            migrationBuilder.CreateIndex(
                name: "IX_PODAttachments_DocumentNumber",
                table: "PODAttachments",
                column: "DocumentNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PODAttachments_DocumentStatus",
                table: "PODAttachments",
                column: "DocumentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PODAttachments_ExpiryDate",
                table: "PODAttachments",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_PODAttachments_IsPrimary",
                table: "PODAttachments",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_PODAttachments_PODId_UploadedFileId",
                table: "PODAttachments",
                columns: new[] { "PODId", "UploadedFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PODAttachments_Type",
                table: "PODAttachments",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_PODAttachments_UploadedFileId",
                table: "PODAttachments",
                column: "UploadedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_PODEntries_Category",
                table: "PODEntries",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_PODEntries_EntryOrder",
                table: "PODEntries",
                column: "EntryOrder");

            migrationBuilder.CreateIndex(
                name: "IX_PODEntries_EntryType",
                table: "PODEntries",
                column: "EntryType");

            migrationBuilder.CreateIndex(
                name: "IX_PODEntries_PODId",
                table: "PODEntries",
                column: "PODId");

            migrationBuilder.CreateIndex(
                name: "IX_PODEntry_CreatedDate",
                table: "PODEntries",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_PODEntry_IsActive",
                table: "PODEntries",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_POD_CreatedDate",
                table: "PODs",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_POD_IsActive",
                table: "PODs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PODs_AutomationStatus",
                table: "PODs",
                column: "AutomationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PODs_CategoryId",
                table: "PODs",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PODs_ContractNumber",
                table: "PODs",
                column: "ContractNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PODs_DepartmentId",
                table: "PODs",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PODs_Frequency",
                table: "PODs",
                column: "Frequency");

            migrationBuilder.CreateIndex(
                name: "IX_PODs_IsFinancialData",
                table: "PODs",
                column: "IsFinancialData");

            migrationBuilder.CreateIndex(
                name: "IX_PODs_Name",
                table: "PODs",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PODs_PODCode",
                table: "PODs",
                column: "PODCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PODs_PONumber",
                table: "PODs",
                column: "PONumber");

            migrationBuilder.CreateIndex(
                name: "IX_PODs_Status",
                table: "PODs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PODs_VendorId",
                table: "PODs",
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
                name: "IX_QueryConstants_IsGlobal",
                table: "QueryConstants",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_QueryConstants_Name",
                table: "QueryConstants",
                column: "Name");

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
                name: "IX_TemplateAnchors_DisplayOrder",
                table: "TemplateAnchors",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAnchors_TemplateId",
                table: "TemplateAnchors",
                column: "TemplateId");

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
                name: "IX_UploadedFile_CreatedDate",
                table: "UploadedFiles",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFile_IsActive",
                table: "UploadedFiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_ExpiryDate",
                table: "UploadedFiles",
                column: "ExpiryDate");

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
                column: "SavedFileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_UploadSource",
                table: "UploadedFiles",
                column: "UploadSource");

            migrationBuilder.CreateIndex(
                name: "IX_Vendor_CreatedDate",
                table: "Vendors",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Vendor_IsActive",
                table: "Vendors",
                column: "IsActive");

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
                name: "PODAttachments");

            migrationBuilder.DropTable(
                name: "PODEntries");

            migrationBuilder.DropTable(
                name: "ProcessedFields");

            migrationBuilder.DropTable(
                name: "QueryConstants");

            migrationBuilder.DropTable(
                name: "QueryResults");

            migrationBuilder.DropTable(
                name: "TemplateAnchors");

            migrationBuilder.DropTable(
                name: "FieldMappings");

            migrationBuilder.DropTable(
                name: "ProcessedFiles");

            migrationBuilder.DropTable(
                name: "QueryOutputs");

            migrationBuilder.DropTable(
                name: "PdfTemplates");

            migrationBuilder.DropTable(
                name: "FormulaCanvases");

            migrationBuilder.DropTable(
                name: "PODs");

            migrationBuilder.DropTable(
                name: "UploadedFiles");

            migrationBuilder.DropTable(
                name: "Queries");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "GeneralDirectorates");
        }
    }
}
