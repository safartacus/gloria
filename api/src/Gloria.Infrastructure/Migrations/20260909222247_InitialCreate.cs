using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gloria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ChangesJson = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RoleCode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PreviousHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "import_batches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessDay = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RowsRead = table.Column<int>(type: "INTEGER", nullable: false),
                    RowsInserted = table.Column<int>(type: "INTEGER", nullable: false),
                    RowsDuplicate = table.Column<int>(type: "INTEGER", nullable: false),
                    RowsFailed = table.Column<int>(type: "INTEGER", nullable: false),
                    RowsReconciled = table.Column<int>(type: "INTEGER", nullable: false),
                    RunByUsername = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "periods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ClosedByUsername = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rule_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HandlerKey = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rule_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "source_systems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_systems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeNo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Hotel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    HireDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TerminationDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employees_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "import_errors",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImportBatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceSystemCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RowNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RawContent = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_errors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_import_errors_import_batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commission_rules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RuleGroupCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    RuleTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    scope_department_code = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    scope_product_code_pattern = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    scope_source_system_code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Rate = table.Column<double>(type: "REAL", nullable: true),
                    AmountPerTransaction = table.Column<double>(type: "REAL", nullable: true),
                    BracketBasis = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    CreatedByUsername = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commission_rules_rule_types_RuleTypeId",
                        column: x => x.RuleTypeId,
                        principalTable: "rule_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commission_results",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PeriodId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalCommission = table.Column<double>(type: "REAL", nullable: false),
                    SaleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RefundCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UnmatchedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CalculatedByUsername = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commission_results_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commission_results_periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_records",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    SourceSystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceTransactionNo = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    amount = table.Column<double>(type: "REAL", nullable: false),
                    currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRefund = table.Column<bool>(type: "INTEGER", nullable: false),
                    OriginalSourceTransactionNo = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ImportBatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    RawRecordJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sale_records_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_records_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_records_import_batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_records_source_systems_SourceSystemId",
                        column: x => x.SourceSystemId,
                        principalTable: "source_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MustChangePassword = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_users_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bracket_tiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommissionRuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    LowerBound = table.Column<double>(type: "REAL", nullable: false),
                    UpperBound = table.Column<double>(type: "REAL", nullable: true),
                    Rate = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bracket_tiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bracket_tiers_commission_rules_CommissionRuleId",
                        column: x => x.CommissionRuleId,
                        principalTable: "commission_rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commission_result_steps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommissionResultId = table.Column<long>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    SaleRecordId = table.Column<long>(type: "INTEGER", nullable: false),
                    CommissionRuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    CommissionRuleVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    RuleTypeCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Base = table.Column<double>(type: "REAL", nullable: false),
                    AppliedRate = table.Column<double>(type: "REAL", nullable: true),
                    AppliedAmount = table.Column<double>(type: "REAL", nullable: true),
                    StepAmount = table.Column<double>(type: "REAL", nullable: false),
                    RunningTotal = table.Column<double>(type: "REAL", nullable: false),
                    IsRefund = table.Column<bool>(type: "INTEGER", nullable: false),
                    Explanation = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission_result_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commission_result_steps_commission_results_CommissionResultId",
                        column: x => x.CommissionResultId,
                        principalTable: "commission_results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commission_result_steps_sale_records_SaleRecordId",
                        column: x => x.SaleRecordId,
                        principalTable: "sale_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "departments",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "SPA", true, "SPA" },
                    { 2, "F&B", true, "F&B" },
                    { 3, "ÖN BÜRO", true, "ÖN BÜRO" },
                    { 4, "GUEST RELATIONS", true, "GUEST RELATIONS" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "Id", "Code", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "ADMIN", "Manages commission rules, periods and imports", "Administrator" },
                    { 2, "ACCOUNTING", "Sees every employee's commission", "Accounting" },
                    { 3, "EMPLOYEE", "Sees only their own commission", "Employee" }
                });

            migrationBuilder.InsertData(
                table: "rule_types",
                columns: new[] { "Id", "Code", "HandlerKey", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "FIXED_PERCENTAGE", "FIXED_PERCENTAGE", true, "Fixed percentage of sale amount" },
                    { 2, "TIERED_BRACKET", "TIERED_BRACKET", true, "Tiered bracket on monthly cumulative base" },
                    { 3, "FIXED_AMOUNT", "FIXED_AMOUNT", true, "Fixed amount per transaction" }
                });

            migrationBuilder.InsertData(
                table: "source_systems",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { 1, "PMS", "Fidelio Property Management System" },
                    { 2, "POS", "Flyby Point of Sale" },
                    { 3, "ERP", "Oracle JD Edwards" }
                });

            migrationBuilder.InsertData(
                table: "employees",
                columns: new[] { "Id", "DepartmentId", "EmployeeNo", "FullName", "HireDate", "Hotel", "TerminationDate" },
                values: new object[,]
                {
                    { 1, 1, "P1001", "Ayşe Demir", new DateOnly(2024, 3, 1), "GSR", null },
                    { 2, 1, "P1002", "Mehmet Kaya", new DateOnly(2024, 3, 1), "GSR", null },
                    { 3, 1, "P1003", "Elif Şahin", new DateOnly(2024, 3, 1), "GGR", null },
                    { 4, 2, "P1004", "Burak Yıldız", new DateOnly(2024, 3, 1), "GSR", null },
                    { 5, 2, "P1005", "Zeynep Arslan", new DateOnly(2024, 3, 1), "GGR", null },
                    { 6, 2, "P1006", "Can Öztürk", new DateOnly(2024, 3, 1), "GVR", null },
                    { 7, 3, "P1007", "Selin Çelik", new DateOnly(2024, 3, 1), "GSR", null },
                    { 8, 3, "P1008", "Emre Koç", new DateOnly(2024, 3, 1), "GGR", null },
                    { 9, 4, "P1009", "Deniz Aydın", new DateOnly(2024, 3, 1), "GSR", null },
                    { 10, 4, "P1010", "Merve Polat", new DateOnly(2024, 3, 1), "GVR", null },
                    { 11, 1, "P1011", "Okan Doğan", new DateOnly(2024, 3, 1), "GVR", null },
                    { 12, 2, "P1012", "Gamze Kurt", new DateOnly(2026, 8, 18), "GSR", null },
                    { 13, 1, "P1013", "Hakan Ateş", new DateOnly(2023, 5, 15), "GSR", new DateOnly(2026, 8, 12) }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CreatedAt", "EmployeeId", "IsActive", "MustChangePassword", "PasswordHash", "RoleId", "Username" },
                values: new object[,]
                {
                    { 14, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 1, "admin" },
                    { 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 2, "accounting" },
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1001" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1002" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1003" },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1004" },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1005" },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1006" },
                    { 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1007" },
                    { 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1008" },
                    { 9, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1009" },
                    { 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1010" },
                    { 11, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 11, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1011" },
                    { 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 12, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1012" },
                    { 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 13, true, true, "pbkdf2$sha256$210000$nOan+1AIsm/cikA4hv/VFw==$KAFE57kVkwhcNiN72y87dYLSEJ1g/dhvKK2bu9XM3y0=", 3, "P1013" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_EntityName_EntityId",
                table: "audit_entries",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_TimestampUtc",
                table: "audit_entries",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_bracket_tiers_CommissionRuleId_Order",
                table: "bracket_tiers",
                columns: new[] { "CommissionRuleId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commission_result_steps_CommissionResultId_Order",
                table: "commission_result_steps",
                columns: new[] { "CommissionResultId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_commission_result_steps_SaleRecordId",
                table: "commission_result_steps",
                column: "SaleRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_commission_results_EmployeeId",
                table: "commission_results",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_commission_results_PeriodId_EmployeeId",
                table: "commission_results",
                columns: new[] { "PeriodId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_EffectiveFrom_EffectiveTo",
                table: "commission_rules",
                columns: new[] { "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_RuleGroupCode_Version",
                table: "commission_rules",
                columns: new[] { "RuleGroupCode", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_RuleTypeId",
                table: "commission_rules",
                column: "RuleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_departments_Code",
                table: "departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_DepartmentId",
                table: "employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_EmployeeNo",
                table: "employees",
                column: "EmployeeNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_import_errors_ImportBatchId",
                table: "import_errors",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_periods_Year_Month",
                table: "periods",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_roles_Code",
                table: "roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rule_types_Code",
                table: "rule_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sale_records_DepartmentId",
                table: "sale_records",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_sale_records_EmployeeId_Year_Month",
                table: "sale_records",
                columns: new[] { "EmployeeId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_sale_records_ImportBatchId",
                table: "sale_records",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_sale_records_ProductCode",
                table: "sale_records",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_sale_records_SourceSystemId_SourceTransactionNo",
                table: "sale_records",
                columns: new[] { "SourceSystemId", "SourceTransactionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_source_systems_Code",
                table: "source_systems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_EmployeeId",
                table: "users",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_RoleId",
                table: "users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);

            // Starter rule set so a fresh install can import + calculate and see real numbers.
            // Manage these from the "Prim Kuralları" screen — no code change needed.
            // Product-code patterns: '_' is a literal character, only '%' is a wildcard.
            migrationBuilder.Sql(@"
INSERT INTO commission_rules
    (""Id"", ""RuleGroupCode"", ""Version"", ""Name"", ""RuleTypeId"",
     scope_department_code, scope_product_code_pattern, scope_source_system_code,
     ""Priority"", ""EffectiveFrom"", ""EffectiveTo"", ""IsActive"",
     ""Rate"", ""AmountPerTransaction"", ""BracketBasis"", ""CreatedByUsername"", ""CreatedAt"")
VALUES
    (1, 'SPA_SERVICE', 1, 'SPA hizmetleri %5',        1, NULL, 'SPA_%', NULL, 10, '2026-01-01', NULL, 1, 0.05, NULL, NULL,                     'system', '2026-01-01 00:00:00'),
    (2, 'BUGGY_RENT',  1, 'Buggy islem basina 50 TL', 3, NULL, 'BUG_%', NULL, 10, '2026-01-01', NULL, 1, NULL, 50,   NULL,                     'system', '2026-01-01 00:00:00'),
    (3, 'ALC_TIER',    1, 'A la carte kademeli',      2, NULL, 'ALC_%', NULL, 10, '2026-01-01', NULL, 1, NULL, NULL, 'MONTHLY_CUMULATIVE_BASE', 'system', '2026-01-01 00:00:00'),
    (4, 'PAVILLON',    1, 'Pavillon %4',              1, NULL, 'PAV_%', NULL, 10, '2026-01-01', NULL, 1, 0.04, NULL, NULL,                     'system', '2026-01-01 00:00:00'),
    (5, 'POS_OUTLET',  1, 'POS outlet %3',            1, NULL, NULL,    'POS', 50, '2026-01-01', NULL, 1, 0.03, NULL, NULL,                     'system', '2026-01-01 00:00:00');

INSERT INTO bracket_tiers (""Id"", ""CommissionRuleId"", ""Order"", ""LowerBound"", ""UpperBound"", ""Rate"")
VALUES
    (1, 3, 1, 0,      100000, 0.03),
    (2, 3, 2, 100000, 250000, 0.05),
    (3, 3, 3, 250000, NULL,   0.07);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM bracket_tiers WHERE \"CommissionRuleId\" IN (1,2,3,4,5);");
            migrationBuilder.Sql("DELETE FROM commission_rules WHERE \"Id\" IN (1,2,3,4,5);");

            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.DropTable(
                name: "bracket_tiers");

            migrationBuilder.DropTable(
                name: "commission_result_steps");

            migrationBuilder.DropTable(
                name: "import_errors");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "commission_rules");

            migrationBuilder.DropTable(
                name: "commission_results");

            migrationBuilder.DropTable(
                name: "sale_records");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "rule_types");

            migrationBuilder.DropTable(
                name: "periods");

            migrationBuilder.DropTable(
                name: "import_batches");

            migrationBuilder.DropTable(
                name: "source_systems");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "departments");
        }
    }
}
