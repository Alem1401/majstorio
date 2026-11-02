using System.Text.RegularExpressions;
using Majstorio.Services.Database;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

// Checks the schema EF generates for SQL Server; no database connection is opened.
public class SqlServerSchemaTests
{
    private static readonly string Script = CreateScript();

    [Theory]
    [InlineData("IX_TradespersonProfiles_UserId", "TradespersonProfiles", "[UserId]")]
    [InlineData("IX_Appointments_OfferId", "Appointments", "[OfferId]")]
    [InlineData("IX_Reviews_AppointmentId", "Reviews", "[AppointmentId]")]
    [InlineData("IX_Refunds_PaymentId", "Refunds", "[PaymentId]")]
    [InlineData("IX_Cities_Name", "Cities", "[Name]")]
    [InlineData("IX_ServiceCategories_Name", "ServiceCategories", "[Name]")]
    [InlineData("IX_ApplicationUsers_Email", "ApplicationUsers", "[Email]")]
    [InlineData("IX_ApplicationUsers_UserName", "ApplicationUsers", "[UserName]")]
    [InlineData("IX_Offers_ServiceRequestId_TradespersonProfileId", "Offers", "[ServiceRequestId], [TradespersonProfileId]")]
    [InlineData("IX_Notifications_EventId", "Notifications", "[EventId]")]
    [InlineData("IX_Payments_CreateOrderRequestId", "Payments", "[CreateOrderRequestId]")]
    [InlineData("IX_Payments_CaptureRequestId", "Payments", "[CaptureRequestId]")]
    [InlineData("IX_Refunds_IdempotencyKey", "Refunds", "[IdempotencyKey]")]
    public void UniqueIndexes_AreCreated(string name, string table, string columns)
    {
        Assert.Contains($"CREATE UNIQUE INDEX [{name}] ON [{table}] ({columns})", Script);
    }

    [Theory]
    [InlineData("IX_Offers_ServiceRequestId_Accepted", "Offers", "[ServiceRequestId]", "WHERE [Status] = 1")]
    [InlineData("IX_Payments_AppointmentId_Open", "Payments", "[AppointmentId]", "WHERE [Status] IN (0, 1)")]
    [InlineData("IX_Payments_PayPalOrderId", "Payments", "[PayPalOrderId]", "WHERE [PayPalOrderId] IS NOT NULL")]
    [InlineData("IX_Payments_PayPalCaptureId", "Payments", "[PayPalCaptureId]", "WHERE [PayPalCaptureId] IS NOT NULL")]
    [InlineData("IX_Refunds_PayPalRefundId", "Refunds", "[PayPalRefundId]", "WHERE [PayPalRefundId] IS NOT NULL")]
    public void FilteredUniqueIndexes_AreCreated(string name, string table, string columns, string filter)
    {
        Assert.Contains($"CREATE UNIQUE INDEX [{name}] ON [{table}] ({columns}) {filter}", Script);
    }

    [Fact]
    public void OnlyOwnedChildrenCascade()
    {
        var cascades = Regex.Matches(Script, @"CONSTRAINT \[(FK_[^\]]+)\][^,;]*?ON DELETE CASCADE")
            .Select(match => match.Groups[1].Value)
            .Order()
            .ToList();

        Assert.Equal(
        [
            "FK_Notifications_ServiceRequests_ServiceRequestId",
            "FK_StatusHistories_ServiceRequests_ServiceRequestId",
            "FK_TradespersonServices_TradespersonProfiles_TradespersonProfileId",
        ], cascades);
    }

    [Fact]
    public void MoneyColumns_HaveExplicitPrecision()
    {
        Assert.Contains("[Price] decimal(18,2) NOT NULL", Script);
        Assert.Contains("[MaximumBudget] decimal(18,2) NOT NULL", Script);
        Assert.Contains("[CapturedAmount] decimal(18,2) NULL", Script);
        Assert.Contains("[MinimumRating] decimal(3,2) NULL", Script);
    }

    private static string CreateScript()
    {
        using var ctx = new MajstorioDbContext(new DbContextOptionsBuilder<MajstorioDbContext>()
            .UseSqlServer("Server=unused;Database=230070").Options);
        return ctx.Database.GenerateCreateScript();
    }
}
