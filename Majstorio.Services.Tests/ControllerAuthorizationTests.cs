using System.Reflection;
using Majstorio.Services.Constants;
using Majstorio.WebAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Majstorio.Services.Tests;

// Guards the attributes that decide who may call the endpoints of the request, offer and appointment flow.
public class ControllerAuthorizationTests
{
    [Theory]
    [InlineData(typeof(ServiceRequestsController))]
    [InlineData(typeof(TradespersonProfilesController))]
    [InlineData(typeof(OffersController))]
    [InlineData(typeof(AppointmentsController))]
    [InlineData(typeof(AccountController))]
    [InlineData(typeof(MyReviewsController))]
    [InlineData(typeof(NotificationsController))]
    public void FlowControllers_RequireAToken(Type controller)
    {
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(controller.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void OnlyTheAccountEndpoints_AreOpenWithoutAToken()
    {
        var controllers = typeof(AuthController).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Microsoft.AspNetCore.Mvc.ControllerBase)))
            .ToList();
        Assert.NotEmpty(controllers);

        foreach (var controller in controllers)
        {
            Assert.Null(controller.GetCustomAttribute<AllowAnonymousAttribute>());
            var actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var action in actions)
            {
                var isOpen = action.GetCustomAttribute<AllowAnonymousAttribute>() != null
                    || (controller.GetCustomAttribute<AuthorizeAttribute>() == null
                        && action.GetCustomAttribute<AuthorizeAttribute>() == null);
                if (isOpen)
                    Assert.True(controller == typeof(AuthController),
                        $"{controller.Name}.{action.Name} can be called without a token.");
            }
        }
    }

    [Theory]
    [InlineData(typeof(UsersController))]
    [InlineData(typeof(DashboardController))]
    [InlineData(typeof(ReportsController))]
    [InlineData(typeof(ReviewsController))]
    public void AdministrationControllers_RequireTheAdministratorRole(Type controller)
    {
        Assert.Equal(RoleNames.Administrator, controller.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Theory]
    [InlineData(typeof(CitiesController))]
    [InlineData(typeof(ServiceCategoriesController))]
    [InlineData(typeof(NewsPostController))]
    [InlineData(typeof(OffersController))]
    [InlineData(typeof(AppointmentsController))]
    public void ReferenceDataAndOverviews_AreChangedOrListedOnlyByAdministrators(Type controller)
    {
        var restricted = new[] { "Create", "Update", "Delete" };
        var overview = controller == typeof(OffersController) || controller == typeof(AppointmentsController);

        foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var adminOnly = overview
                ? action.Name is "Get" or "GetById" or "Delete"
                : restricted.Contains(action.Name);
            if (adminOnly)
                Assert.Equal(RoleNames.Administrator, action.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        }
    }

    [Theory]
    [InlineData(typeof(OffersController), nameof(OffersController.GetMine))]
    [InlineData(typeof(OffersController), nameof(OffersController.Create))]
    [InlineData(typeof(OffersController), nameof(OffersController.Update))]
    [InlineData(typeof(OffersController), nameof(OffersController.Withdraw))]
    [InlineData(typeof(OffersController), nameof(OffersController.Accept))]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.GetMine))]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.Complete))]
    [InlineData(typeof(AuthController), nameof(AuthController.Logout))]
    [InlineData(typeof(AuthController), nameof(AuthController.ChangePassword))]
    public void ActionsThatChangeOrExposeOwnedRecords_AreNotAnonymous(Type controller, string action)
    {
        var method = controller.GetMethod(action)!;

        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.True(method.GetCustomAttribute<AuthorizeAttribute>() != null
            || controller.GetCustomAttribute<AuthorizeAttribute>() != null);
    }

    [Theory]
    [InlineData(typeof(TradespersonProfilesController), nameof(TradespersonProfilesController.Approve), RoleNames.Administrator)]
    [InlineData(typeof(TradespersonProfilesController), nameof(TradespersonProfilesController.SetActive), RoleNames.Administrator)]
    [InlineData(typeof(OffersController), nameof(OffersController.Create), RoleNames.Tradesperson)]
    [InlineData(typeof(OffersController), nameof(OffersController.Withdraw), RoleNames.Tradesperson)]
    [InlineData(typeof(AppointmentsController), nameof(AppointmentsController.Complete), RoleNames.Tradesperson)]
    public void RoleRestrictedActions_NameTheirRole(Type controller, string action, string role)
    {
        Assert.Equal(role, controller.GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    // Only customers rate jobs; the account endpoints are for every signed-in user, whatever the role.
    [Fact]
    public void CustomerReviews_AreForCustomers_AndTheAccountIsForAnySignedInUser()
    {
        Assert.Equal(RoleNames.Customer, typeof(MyReviewsController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Null(typeof(AccountController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }
}
