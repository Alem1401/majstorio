using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Majstorio.Services.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    TokenVersion = table.Column<int>(type: "int", nullable: false),
                    PasswordResetTokenHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PasswordResetTokenExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthorId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsPosts_ApplicationUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TradespersonProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradespersonProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradespersonProfiles_ApplicationUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradespersonProfiles_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradespersonProfiles_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SearchLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    ServiceCategoryId = table.Column<int>(type: "int", nullable: true),
                    Query = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MinimumRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    AvailableFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AvailableToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SearchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchLogs_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SearchLogs_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SearchLogs_ServiceCategories_ServiceCategoryId",
                        column: x => x.ServiceCategoryId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    ServiceCategoryId = table.Column<int>(type: "int", nullable: false),
                    TargetTradespersonProfileId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DesiredStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaximumBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRequests_ApplicationUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceRequests_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceRequests_ServiceCategories_ServiceCategoryId",
                        column: x => x.ServiceCategoryId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceRequests_TradespersonProfiles_TargetTradespersonProfileId",
                        column: x => x.TargetTradespersonProfileId,
                        principalTable: "TradespersonProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TradespersonServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TradespersonProfileId = table.Column<int>(type: "int", nullable: false),
                    ServiceCategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MinimumPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradespersonServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradespersonServices_ServiceCategories_ServiceCategoryId",
                        column: x => x.ServiceCategoryId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradespersonServices_TradespersonProfiles_TradespersonProfileId",
                        column: x => x.TradespersonProfileId,
                        principalTable: "TradespersonProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    TradespersonProfileId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProposedStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DecisionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_ApplicationUsers_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_TradespersonProfiles_TradespersonProfileId",
                        column: x => x.TradespersonProfileId,
                        principalTable: "TradespersonProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfferId = table.Column<int>(type: "int", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CapturedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PayPalOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PayPalCaptureId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreateOrderRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaptureRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    TradespersonProfileId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_ApplicationUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_TradespersonProfiles_TradespersonProfileId",
                        column: x => x.TradespersonProfileId,
                        principalTable: "TradespersonProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    AppointmentId = table.Column<int>(type: "int", nullable: true),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    PreviousAppointmentStatus = table.Column<int>(type: "int", nullable: true),
                    NewAppointmentStatus = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatusHistories_ApplicationUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatusHistories_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatusHistories_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PayPalRefundId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_ApplicationUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refunds_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ApplicationUsers",
                columns: new[] { "Id", "CreatedAtUtc", "Email", "FirstName", "IsActive", "LastName", "PasswordHash", "PasswordResetTokenExpiresAtUtc", "PasswordResetTokenHash", "PasswordSalt", "PhoneNumber", "ProfileImageUrl", "Role", "TokenVersion", "UserName" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "desktop@majstorio.test", "Admin", true, "Majstorio", "bA0l8+egVg1IRJnGrA7MuYQVBWcEOwuxBVeq4m2pmSM=", null, null, "F72m7JpkIi/pftay5kpuaw==", null, null, 2, 0, "desktop" },
                    { 2, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "mobile@majstorio.test", "Amra", true, "Hadžić", "fnl1ny77TbHjSENxsfvBa+48Q4XhB+YnP32zzxOYuNw=", null, null, "YmL3CeHyHDrDvWhx7TXdbw==", null, null, 0, 0, "mobile" },
                    { 3, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "tradesperson@majstorio.test", "Emir", true, "Kovačević", "EQIsPOK/a3O9k29KlSItBdCICHvpgrz30kyIEAR9PDk=", null, null, "gKsYEZzG4YZNSrSaiUKwqQ==", null, null, 1, 0, "tradesperson" },
                    { 4, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "electrician@majstorio.test", "Dino", true, "Begić", "DYcQVjcwWcuZLjC7nqMQGXUG/D+sRuhW2bLwzzuAO6I=", null, null, "YW/z0gf9C7JnAr2032vFzg==", null, null, 1, 0, "electrician" },
                    { 5, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "customer@majstorio.test", "Lejla", true, "Mehić", "La6UY7FrdnVlFH21cN5F5e/nretkhhDwM1vKpcMeoUk=", null, null, "UKDX3zrY6A1rofazD2c/KA==", null, null, 0, 0, "customer" },
                    { 6, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "applicant@majstorio.test", "Haris", true, "Delić", "73OMtiRTAG6eLAJadOSMH4qbRt6zA1qBDDsGuCiCpXw=", null, null, "HkdMEb3eCtO/5Ne5JASwMw==", null, null, 0, 0, "applicant" }
                });

            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), true, "Mostar" },
                    { 2, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), true, "Sarajevo" },
                    { 3, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), true, "Tuzla" },
                    { 4, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), true, "Zenica" },
                    { 5, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), true, "Banja Luka" },
                    { 6, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), false, "Bihać" }
                });

            migrationBuilder.InsertData(
                table: "ServiceCategories",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), true, "Vodoinstalaterski radovi" },
                    { 2, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), true, "Elektroinstalacije" },
                    { 3, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), true, "Molersko-farbarski radovi" },
                    { 4, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), true, "Stolarija" },
                    { 5, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), true, "Klima uređaji" }
                });

            migrationBuilder.InsertData(
                table: "NewsPosts",
                columns: new[] { "Id", "AuthorId", "Content", "CreatedAtUtc", "ImageUrl", "PublishedAtUtc", "Title" },
                values: new object[,]
                {
                    { 1, 1, "Pronađite provjerenog majstora u svom gradu, uporedite ponude i dogovorite termin na jednom mjestu.", new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), "https://picsum.photos/seed/majstorio-welcome/800/450", new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Dobrodošli u Majstorio" },
                    { 2, 1, "Redovan servis klima uređaja prije zime smanjuje potrošnju struje i produžava vijek uređaja.", new DateTime(2026, 10, 20, 9, 0, 0, 0, DateTimeKind.Utc), "https://picsum.photos/seed/majstorio-ac/800/450", new DateTime(2026, 10, 20, 9, 0, 0, 0, DateTimeKind.Utc), "Pripremite klima uređaj za zimu" }
                });

            migrationBuilder.InsertData(
                table: "SearchLogs",
                columns: new[] { "Id", "AvailableFromUtc", "AvailableToUtc", "CityId", "CreatedAtUtc", "MinimumRating", "Query", "SearchedAtUtc", "ServiceCategoryId", "UserId" },
                values: new object[,]
                {
                    { 1, null, null, 1, new DateTime(2026, 10, 24, 9, 0, 0, 0, DateTimeKind.Utc), 4m, null, new DateTime(2026, 10, 24, 9, 0, 0, 0, DateTimeKind.Utc), 1, 2 },
                    { 2, null, null, 1, new DateTime(2026, 10, 11, 9, 0, 0, 0, DateTimeKind.Utc), null, "klima", new DateTime(2026, 10, 11, 9, 0, 0, 0, DateTimeKind.Utc), 5, 2 },
                    { 3, null, null, 2, new DateTime(2026, 9, 30, 9, 0, 0, 0, DateTimeKind.Utc), null, "luster", new DateTime(2026, 9, 30, 9, 0, 0, 0, DateTimeKind.Utc), 2, 5 }
                });

            migrationBuilder.InsertData(
                table: "ServiceRequests",
                columns: new[] { "Id", "Address", "CityId", "CreatedAtUtc", "CustomerId", "Description", "DesiredStartUtc", "MaximumBudget", "ServiceCategoryId", "Status", "TargetTradespersonProfileId" },
                values: new object[,]
                {
                    { 1, "Kralja Tvrtka 12", 1, new DateTime(2026, 11, 1, 9, 0, 0, 0, DateTimeKind.Utc), 2, "Curi slavina u kuhinji, potrebna zamjena.", new DateTime(2026, 12, 10, 9, 0, 0, 0, DateTimeKind.Utc), 100m, 1, 0, null },
                    { 2, "Kralja Tvrtka 12", 1, new DateTime(2026, 10, 25, 9, 0, 0, 0, DateTimeKind.Utc), 2, "Zamjena bojlera od 80 litara.", new DateTime(2026, 12, 5, 9, 0, 0, 0, DateTimeKind.Utc), 300m, 1, 2, null },
                    { 3, "Titova 45", 2, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), 5, "Ugradnja lustera u dnevnom boravku.", new DateTime(2026, 10, 10, 9, 0, 0, 0, DateTimeKind.Utc), 150m, 2, 3, null },
                    { 4, "Kralja Tvrtka 12", 1, new DateTime(2026, 10, 12, 9, 0, 0, 0, DateTimeKind.Utc), 2, "Godišnji servis klima uređaja.", new DateTime(2026, 10, 20, 9, 0, 0, 0, DateTimeKind.Utc), 120m, 5, 4, null }
                });

            migrationBuilder.InsertData(
                table: "TradespersonProfiles",
                columns: new[] { "Id", "ApprovedAtUtc", "ApprovedByUserId", "CityId", "CreatedAtUtc", "Description", "IsActive", "IsApproved", "UserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 2, 9, 0, 0, 0, DateTimeKind.Utc), 1, 1, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Vodoinstalater s 12 godina iskustva. Popravke, zamjena bojlera i servis klima uređaja.", true, true, 3 },
                    { 2, new DateTime(2026, 9, 2, 9, 0, 0, 0, DateTimeKind.Utc), 1, 2, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Licencirani električar. Instalacije, rasvjeta i zamjena utičnica.", true, true, 4 },
                    { 3, null, null, 1, new DateTime(2026, 10, 28, 9, 0, 0, 0, DateTimeKind.Utc), "Moler i farbar, unutrašnji i vanjski radovi.", true, false, 6 }
                });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "CreatedAtUtc", "EventId", "IsRead", "Message", "ServiceRequestId", "Title", "UserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 27, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("8a7b6c5d-1e2f-4a3b-8c9d-0e1f2a3b4c01"), true, "Termin za zamjenu bojlera potvrđen je za 5. decembar u 9:00.", 2, "Termin potvrđen", 2 },
                    { 2, new DateTime(2026, 11, 1, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("8a7b6c5d-1e2f-4a3b-8c9d-0e1f2a3b4c02"), false, "Kupac u Mostaru traži zamjenu slavine.", 1, "Novi zahtjev u vašoj kategoriji", 3 },
                    { 3, new DateTime(2026, 10, 10, 11, 0, 0, 0, DateTimeKind.Utc), new Guid("8a7b6c5d-1e2f-4a3b-8c9d-0e1f2a3b4c03"), false, "Ugradnja lustera je završena. Ocijenite majstora.", 3, "Usluga završena", 5 },
                    { 4, new DateTime(2026, 10, 18, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("8a7b6c5d-1e2f-4a3b-8c9d-0e1f2a3b4c04"), false, "Iznos od 90 EUR vraćen je na vaš PayPal račun.", 4, "Povrat novca izvršen", 2 }
                });

            migrationBuilder.InsertData(
                table: "Offers",
                columns: new[] { "Id", "CreatedAtUtc", "DecidedAtUtc", "DecidedByUserId", "DecisionReason", "DurationMinutes", "Price", "ProposedStartUtc", "ServiceRequestId", "Status", "TradespersonProfileId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 11, 2, 9, 0, 0, 0, DateTimeKind.Utc), null, null, null, 60, 80m, new DateTime(2026, 12, 10, 9, 0, 0, 0, DateTimeKind.Utc), 1, 0, 1 },
                    { 2, new DateTime(2026, 10, 26, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 27, 9, 0, 0, 0, DateTimeKind.Utc), 2, "Najbolja ponuda.", 120, 250m, new DateTime(2026, 12, 5, 9, 0, 0, 0, DateTimeKind.Utc), 2, 1, 1 },
                    { 3, new DateTime(2026, 10, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 3, 9, 0, 0, 0, DateTimeKind.Utc), 5, null, 120, 120m, new DateTime(2026, 10, 10, 9, 0, 0, 0, DateTimeKind.Utc), 3, 1, 2 },
                    { 4, new DateTime(2026, 10, 13, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 14, 9, 0, 0, 0, DateTimeKind.Utc), 2, null, 60, 90m, new DateTime(2026, 10, 20, 9, 0, 0, 0, DateTimeKind.Utc), 4, 1, 1 }
                });

            migrationBuilder.InsertData(
                table: "StatusHistories",
                columns: new[] { "Id", "AppointmentId", "ChangedAtUtc", "ChangedByUserId", "CreatedAtUtc", "NewAppointmentStatus", "NewStatus", "Note", "PreviousAppointmentStatus", "PreviousStatus", "ServiceRequestId" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 11, 1, 9, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 11, 1, 9, 0, 0, 0, DateTimeKind.Utc), null, 0, null, null, null, 1 },
                    { 2, null, new DateTime(2026, 10, 25, 9, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 10, 25, 9, 0, 0, 0, DateTimeKind.Utc), null, 0, null, null, null, 2 },
                    { 3, null, new DateTime(2026, 10, 27, 9, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 10, 27, 9, 0, 0, 0, DateTimeKind.Utc), null, 1, null, null, 0, 2 },
                    { 5, null, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), 5, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), null, 0, null, null, null, 3 },
                    { 6, null, new DateTime(2026, 10, 3, 9, 0, 0, 0, DateTimeKind.Utc), 5, new DateTime(2026, 10, 3, 9, 0, 0, 0, DateTimeKind.Utc), null, 1, null, null, 0, 3 },
                    { 9, null, new DateTime(2026, 10, 12, 9, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 10, 12, 9, 0, 0, 0, DateTimeKind.Utc), null, 0, null, null, null, 4 },
                    { 10, null, new DateTime(2026, 10, 14, 9, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 10, 14, 9, 0, 0, 0, DateTimeKind.Utc), null, 1, null, null, 0, 4 }
                });

            migrationBuilder.InsertData(
                table: "TradespersonServices",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "EstimatedDurationMinutes", "IsActive", "MaximumPrice", "MinimumPrice", "Name", "ServiceCategoryId", "TradespersonProfileId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Otklanjanje curenja, zamjena slavina i bojlera.", 90, true, 300m, 30m, "Popravka slavina i bojlera", 1, 1 },
                    { 2, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Čišćenje, dopuna plina i redovni servis.", 60, true, 150m, 50m, "Servis klima uređaja", 5, 1 },
                    { 3, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Zamjena i ugradnja utičnica, prekidača i osigurača.", 45, true, 80m, 20m, "Zamjena utičnica i prekidača", 2, 2 },
                    { 4, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Montaža lustera, LED traka i vanjske rasvjete.", 120, true, 200m, 40m, "Ugradnja rasvjete", 2, 2 },
                    { 5, new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Gletovanje i krečenje prostorija.", 480, true, 600m, 100m, "Krečenje stanova", 3, 3 }
                });

            migrationBuilder.InsertData(
                table: "Appointments",
                columns: new[] { "Id", "CreatedAtUtc", "EndUtc", "OfferId", "StartUtc", "Status" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 27, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 5, 11, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 12, 5, 9, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 2, new DateTime(2026, 10, 3, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 10, 11, 0, 0, 0, DateTimeKind.Utc), 3, new DateTime(2026, 10, 10, 9, 0, 0, 0, DateTimeKind.Utc), 3 },
                    { 3, new DateTime(2026, 10, 14, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 20, 10, 0, 0, 0, DateTimeKind.Utc), 4, new DateTime(2026, 10, 20, 9, 0, 0, 0, DateTimeKind.Utc), 2 }
                });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "Amount", "AppointmentId", "CaptureRequestId", "CapturedAmount", "CreateOrderRequestId", "CreatedAtUtc", "Currency", "PaidAtUtc", "PayPalCaptureId", "PayPalOrderId", "Status" },
                values: new object[,]
                {
                    { 1, 120m, 2, new Guid("6f1c2a4e-0a51-4c8e-9d7b-1a2b3c4d5e02"), 120m, new Guid("6f1c2a4e-0a51-4c8e-9d7b-1a2b3c4d5e01"), new DateTime(2026, 10, 10, 11, 0, 0, 0, DateTimeKind.Utc), "EUR", new DateTime(2026, 10, 10, 12, 0, 0, 0, DateTimeKind.Utc), "SEED-CAPTURE-0001", "SEED-ORDER-0001", 1 },
                    { 2, 90m, 3, new Guid("6f1c2a4e-0a51-4c8e-9d7b-1a2b3c4d5e04"), 90m, new Guid("6f1c2a4e-0a51-4c8e-9d7b-1a2b3c4d5e03"), new DateTime(2026, 10, 15, 9, 0, 0, 0, DateTimeKind.Utc), "EUR", new DateTime(2026, 10, 15, 9, 0, 0, 0, DateTimeKind.Utc), "SEED-CAPTURE-0002", "SEED-ORDER-0002", 1 }
                });

            migrationBuilder.InsertData(
                table: "Reviews",
                columns: new[] { "Id", "AppointmentId", "Comment", "CreatedAtUtc", "CustomerId", "Rating", "TradespersonProfileId" },
                values: new object[] { 1, 2, "Brzo, uredno i tačno na vrijeme.", new DateTime(2026, 10, 11, 9, 0, 0, 0, DateTimeKind.Utc), 5, 5, 2 });

            migrationBuilder.InsertData(
                table: "StatusHistories",
                columns: new[] { "Id", "AppointmentId", "ChangedAtUtc", "ChangedByUserId", "CreatedAtUtc", "NewAppointmentStatus", "NewStatus", "Note", "PreviousAppointmentStatus", "PreviousStatus", "ServiceRequestId" },
                values: new object[,]
                {
                    { 4, 1, new DateTime(2026, 10, 27, 12, 0, 0, 0, DateTimeKind.Utc), 3, new DateTime(2026, 10, 27, 12, 0, 0, 0, DateTimeKind.Utc), 1, 2, null, 0, 1, 2 },
                    { 7, 2, new DateTime(2026, 10, 3, 12, 0, 0, 0, DateTimeKind.Utc), 4, new DateTime(2026, 10, 3, 12, 0, 0, 0, DateTimeKind.Utc), 1, 2, null, 0, 1, 3 },
                    { 8, 2, new DateTime(2026, 10, 10, 11, 0, 0, 0, DateTimeKind.Utc), 4, new DateTime(2026, 10, 10, 11, 0, 0, 0, DateTimeKind.Utc), 3, 3, null, 1, 2, 3 },
                    { 11, 3, new DateTime(2026, 10, 14, 12, 0, 0, 0, DateTimeKind.Utc), 3, new DateTime(2026, 10, 14, 12, 0, 0, 0, DateTimeKind.Utc), 1, 2, null, 0, 1, 4 },
                    { 12, 3, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Utc), 2, 4, "Kupac je otkazao termin.", 1, 2, 4 }
                });

            migrationBuilder.InsertData(
                table: "Refunds",
                columns: new[] { "Id", "Amount", "CreatedAtUtc", "IdempotencyKey", "PayPalRefundId", "PaymentId", "ProcessedAtUtc", "Reason", "RequestedByUserId", "Status" },
                values: new object[] { 1, 90m, new DateTime(2026, 10, 18, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("6f1c2a4e-0a51-4c8e-9d7b-1a2b3c4d5e05"), "SEED-REFUND-0001", 2, new DateTime(2026, 10, 18, 12, 0, 0, 0, DateTimeKind.Utc), "Termin je otkazan prije izvođenja radova.", 1, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_Email",
                table: "ApplicationUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_UserName",
                table: "ApplicationUsers",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_OfferId",
                table: "Appointments",
                column: "OfferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Name",
                table: "Cities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsPosts_AuthorId",
                table: "NewsPosts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_EventId",
                table: "Notifications",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ServiceRequestId",
                table: "Notifications",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_DecidedByUserId",
                table: "Offers",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ServiceRequestId_Accepted",
                table: "Offers",
                column: "ServiceRequestId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ServiceRequestId_TradespersonProfileId",
                table: "Offers",
                columns: new[] { "ServiceRequestId", "TradespersonProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_TradespersonProfileId",
                table: "Offers",
                column: "TradespersonProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_AppointmentId_Open",
                table: "Payments",
                column: "AppointmentId",
                unique: true,
                filter: "[Status] IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CaptureRequestId",
                table: "Payments",
                column: "CaptureRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreateOrderRequestId",
                table: "Payments",
                column: "CreateOrderRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayPalCaptureId",
                table: "Payments",
                column: "PayPalCaptureId",
                unique: true,
                filter: "[PayPalCaptureId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayPalOrderId",
                table: "Payments",
                column: "PayPalOrderId",
                unique: true,
                filter: "[PayPalOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_IdempotencyKey",
                table: "Refunds",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_PaymentId",
                table: "Refunds",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_PayPalRefundId",
                table: "Refunds",
                column: "PayPalRefundId",
                unique: true,
                filter: "[PayPalRefundId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_RequestedByUserId",
                table: "Refunds",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_AppointmentId",
                table: "Reviews",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TradespersonProfileId",
                table: "Reviews",
                column: "TradespersonProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchLogs_CityId",
                table: "SearchLogs",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchLogs_ServiceCategoryId",
                table: "SearchLogs",
                column: "ServiceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchLogs_UserId",
                table: "SearchLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategories_Name",
                table: "ServiceCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_CityId",
                table: "ServiceRequests",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_CustomerId",
                table: "ServiceRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_ServiceCategoryId",
                table: "ServiceRequests",
                column: "ServiceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_TargetTradespersonProfileId",
                table: "ServiceRequests",
                column: "TargetTradespersonProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistories_AppointmentId",
                table: "StatusHistories",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistories_ChangedByUserId",
                table: "StatusHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistories_ServiceRequestId",
                table: "StatusHistories",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TradespersonProfiles_ApprovedByUserId",
                table: "TradespersonProfiles",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TradespersonProfiles_CityId",
                table: "TradespersonProfiles",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_TradespersonProfiles_UserId",
                table: "TradespersonProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradespersonServices_ServiceCategoryId",
                table: "TradespersonServices",
                column: "ServiceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TradespersonServices_TradespersonProfileId",
                table: "TradespersonServices",
                column: "TradespersonProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsPosts");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "SearchLogs");

            migrationBuilder.DropTable(
                name: "StatusHistories");

            migrationBuilder.DropTable(
                name: "TradespersonServices");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "ServiceRequests");

            migrationBuilder.DropTable(
                name: "ServiceCategories");

            migrationBuilder.DropTable(
                name: "TradespersonProfiles");

            migrationBuilder.DropTable(
                name: "ApplicationUsers");

            migrationBuilder.DropTable(
                name: "Cities");
        }
    }
}
