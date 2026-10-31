using Majstorio.Model.Requests.Appointment;
using Majstorio.Model.Results;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;
using Majstorio.Services.Mapping;
using Majstorio.Services.Services;
using Majstorio.Services.Validators.Appointment;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Majstorio.Services.Tests;

public class AppointmentServiceTests : IDisposable
{
    private readonly MajstorioDbContext _ctx;
    private readonly AppointmentService _service;
    private readonly DateTime _start = DateTime.UtcNow.AddDays(2);

    public AppointmentServiceTests()
    {
        _ctx = new MajstorioDbContext(new DbContextOptionsBuilder<MajstorioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = new TypeAdapterConfig();
        new AppointmentMappingProfile().Register(config);
        _service = new AppointmentService(_ctx, new Mapper(config));
    }

    [Fact]
    public async Task Create_MissingOffer_ReturnsNotFound()
    {
        var result = await _service.CreateAppointmentAsync(CreateRequest(999));

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(await _ctx.Appointments.ToListAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void CreateValidator_EndNotAfterStart_ReturnsValidation(int durationMinutes)
    {
        var request = CreateRequest(1);
        request.EndUtc = request.StartUtc.AddMinutes(durationMinutes);

        var result = new AppointmentCreateRequestValidator().Validate(request);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.EndUtc));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void CreateValidator_NonUtcTime_ReturnsValidation(DateTimeKind kind)
    {
        var request = CreateRequest(1);
        request.StartUtc = DateTime.SpecifyKind(request.StartUtc, kind);

        var result = new AppointmentCreateRequestValidator().Validate(request);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.StartUtc));
    }

    [Fact]
    public void CreateValidator_PastStart_ReturnsValidation()
    {
        var request = CreateRequest(1);
        request.StartUtc = DateTime.UtcNow.AddHours(-2);
        request.EndUtc = request.StartUtc.AddHours(1);

        var result = new AppointmentCreateRequestValidator().Validate(request);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.StartUtc));
    }

    [Theory]
    [InlineData(OfferStatus.Pending)]
    [InlineData(OfferStatus.Rejected)]
    [InlineData(OfferStatus.Withdrawn)]
    [InlineData(OfferStatus.Closed)]
    public async Task Create_OfferNotAccepted_ReturnsConflict(OfferStatus status)
    {
        var offer = await AddOfferAsync(status: status);

        Assert.Equal(ErrorType.Conflict,
            (await _service.CreateAppointmentAsync(CreateRequest(offer.Id))).ErrorType);
    }

    [Fact]
    public async Task Create_DuplicateOffer_ReturnsConflictEvenWhenCancelled()
    {
        var offer = await AddOfferAsync();
        await AddAppointmentAsync(offer, AppointmentStatus.Cancelled);

        Assert.Equal(ErrorType.Conflict,
            (await _service.CreateAppointmentAsync(CreateRequest(offer.Id))).ErrorType);
        Assert.Single(await _ctx.Appointments.ToListAsync());
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public async Task Create_OverlappingCustomerOrTradesperson_ReturnsConflict(int customerId, int tradespersonId)
    {
        var bookedOffer = await AddOfferAsync();
        await AddAppointmentAsync(bookedOffer);
        var offer = await AddOfferAsync(customerId, tradespersonId);
        var request = CreateRequest(offer.Id);
        request.StartUtc = _start.AddMinutes(30);
        request.EndUtc = _start.AddMinutes(90);

        Assert.Equal(ErrorType.Conflict, (await _service.CreateAppointmentAsync(request)).ErrorType);
    }

    [Fact]
    public async Task Create_UnrelatedParticipants_CanBookSameTime()
    {
        await AddAppointmentAsync(await AddOfferAsync());
        var offer = await AddOfferAsync(2, 2);

        Assert.True((await _service.CreateAppointmentAsync(CreateRequest(offer.Id))).IsSuccess);
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(60)]
    public async Task Create_AdjacentTime_IsAllowed(int offsetMinutes)
    {
        await AddAppointmentAsync(await AddOfferAsync());
        var offer = await AddOfferAsync();
        var request = CreateRequest(offer.Id);
        request.StartUtc = _start.AddMinutes(offsetMinutes);
        request.EndUtc = request.StartUtc.AddHours(1);

        Assert.True((await _service.CreateAppointmentAsync(request)).IsSuccess);
    }

    [Theory]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.Completed)]
    public async Task Create_InactiveAppointment_DoesNotBlockTime(AppointmentStatus status)
    {
        await AddAppointmentAsync(await AddOfferAsync(), status);
        var offer = await AddOfferAsync();

        Assert.True((await _service.CreateAppointmentAsync(CreateRequest(offer.Id))).IsSuccess);
    }

    [Fact]
    public async Task Crud_PersistsAndMapsAppointments()
    {
        var offer = await AddOfferAsync();
        Assert.True((await _service.CreateAppointmentAsync(CreateRequest(offer.Id))).IsSuccess);
        var appointment = await _ctx.Appointments.SingleAsync();
        var createdAt = appointment.CreatedAtUtc;
        var update = UpdateRequest(appointment);
        update.StartUtc = _start.AddHours(2);
        update.EndUtc = _start.AddHours(3);
        update.Status = (int)AppointmentStatus.Confirmed;

        Assert.True((await _service.UpdateAppointmentAsync(update)).IsSuccess);
        _ctx.ChangeTracker.Clear();
        var response = await _service.GetAppointmentByIdAsync(appointment.Id);
        Assert.True(response.IsSuccess);
        Assert.Equal(offer.Id, response.Value!.OfferId);
        Assert.Equal(update.StartUtc, response.Value.StartUtc);
        Assert.Equal(update.EndUtc, response.Value.EndUtc);
        Assert.Equal(update.Status, response.Value.Status);
        Assert.Equal(createdAt, (await _ctx.Appointments.SingleAsync()).CreatedAtUtc);
        Assert.Single((await _service.GetAllAppointmentsAsync()).Value!);
        Assert.True((await _service.DeleteAppointmentAsync(new() { Id = appointment.Id })).IsSuccess);
        Assert.Empty((await _service.GetAllAppointmentsAsync()).Value!);
    }

    [Fact]
    public async Task Update_UnchangedSchedule_DoesNotConflictWithItself()
    {
        var appointment = await AddAppointmentAsync(await AddOfferAsync());

        Assert.True((await _service.UpdateAppointmentAsync(UpdateRequest(appointment))).IsSuccess);
    }

    [Fact]
    public async Task Update_MissingOffer_ReturnsNotFoundWithoutChangingAppointment()
    {
        var appointment = await AddAppointmentAsync(await AddOfferAsync());
        var request = UpdateRequest(appointment);
        request.OfferId = 999;

        Assert.Equal(ErrorType.NotFound, (await _service.UpdateAppointmentAsync(request)).ErrorType);
        Assert.NotEqual(request.OfferId, appointment.OfferId);
    }

    [Fact]
    public async Task Update_Overlap_ReturnsConflictWithoutChangingSchedule()
    {
        await AddAppointmentAsync(await AddOfferAsync());
        var appointment = await AddAppointmentAsync(await AddOfferAsync(), start: _start.AddHours(2));
        var request = UpdateRequest(appointment);
        request.StartUtc = _start;
        request.EndUtc = _start.AddHours(1);

        Assert.Equal(ErrorType.Conflict, (await _service.UpdateAppointmentAsync(request)).ErrorType);
        Assert.Equal(_start.AddHours(2), appointment.StartUtc);
    }

    [Fact]
    public async Task Update_Cancel_ReleasesTimeForAnotherOffer()
    {
        var appointment = await AddAppointmentAsync(await AddOfferAsync());
        var request = UpdateRequest(appointment);
        request.Status = (int)AppointmentStatus.Cancelled;
        Assert.True((await _service.UpdateAppointmentAsync(request)).IsSuccess);
        var offer = await AddOfferAsync();

        Assert.True((await _service.CreateAppointmentAsync(CreateRequest(offer.Id))).IsSuccess);
    }

    [Theory]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.Completed)]
    public async Task Update_TerminalStatus_CannotBeReopened(AppointmentStatus status)
    {
        var appointment = await AddAppointmentAsync(await AddOfferAsync(), status);
        var request = UpdateRequest(appointment);
        request.Status = (int)AppointmentStatus.Pending;

        Assert.Equal(ErrorType.Conflict, (await _service.UpdateAppointmentAsync(request)).ErrorType);
        Assert.Equal(status, appointment.Status);
    }

    [Fact]
    public void UpdateValidator_InvalidStatus_ReturnsValidation()
    {
        var request = new AppointmentUpdateRequest
        {
            Id = 1, OfferId = 1, StartUtc = _start, EndUtc = _start.AddHours(1)
        };
        request.Status = 999;

        var result = new AppointmentUpdateRequestValidator().Validate(request);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Status));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void UpdateValidator_EndNotAfterStart_ReturnsValidation(int durationMinutes)
    {
        var request = new AppointmentUpdateRequest { Id = 1, OfferId = 1, StartUtc = _start };
        request.EndUtc = request.StartUtc.AddMinutes(durationMinutes);

        var result = new AppointmentUpdateRequestValidator().Validate(request);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.EndUtc));
    }

    [Fact]
    public async Task Update_RescheduleIntoPast_ReturnsValidation()
    {
        var appointment = await AddAppointmentAsync(await AddOfferAsync());
        var request = UpdateRequest(appointment);
        request.StartUtc = DateTime.UtcNow.AddHours(-2);
        request.EndUtc = request.StartUtc.AddHours(1);

        Assert.Equal(ErrorType.Validation, (await _service.UpdateAppointmentAsync(request)).ErrorType);
        Assert.Equal(_start, appointment.StartUtc);
    }

    [Fact]
    public async Task Update_OfferAlreadyBooked_ReturnsConflict()
    {
        var booked = await AddAppointmentAsync(await AddOfferAsync());
        var appointment = await AddAppointmentAsync(await AddOfferAsync(2, 2));
        var request = UpdateRequest(appointment);
        request.OfferId = booked.OfferId;

        Assert.Equal(ErrorType.Conflict, (await _service.UpdateAppointmentAsync(request)).ErrorType);
        Assert.NotEqual(booked.OfferId, appointment.OfferId);
    }

    [Theory]
    [InlineData(AppointmentStatus.Confirmed, AppointmentStatus.Pending)]
    [InlineData(AppointmentStatus.Pending, AppointmentStatus.Completed)]
    public async Task Update_InvalidStatusTransition_ReturnsConflict(
        AppointmentStatus currentStatus, AppointmentStatus newStatus)
    {
        var appointment = await AddAppointmentAsync(await AddOfferAsync(), currentStatus);
        var request = UpdateRequest(appointment);
        request.Status = (int)newStatus;

        Assert.Equal(ErrorType.Conflict, (await _service.UpdateAppointmentAsync(request)).ErrorType);
        Assert.Equal(currentStatus, appointment.Status);
    }

    [Theory]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.Completed)]
    public async Task Update_TerminalAppointment_CannotBeRescheduled(AppointmentStatus status)
    {
        var appointment = await AddAppointmentAsync(await AddOfferAsync(), status);
        var request = UpdateRequest(appointment);
        request.StartUtc = _start.AddHours(2);
        request.EndUtc = _start.AddHours(3);

        Assert.Equal(ErrorType.Conflict, (await _service.UpdateAppointmentAsync(request)).ErrorType);
        Assert.Equal(_start, appointment.StartUtc);
    }

    [Fact]
    public async Task Update_CompletedBeforeEnd_ReturnsValidation()
    {
        var appointment = await AddAppointmentAsync(await AddOfferAsync(), AppointmentStatus.Confirmed);
        var request = UpdateRequest(appointment);
        request.Status = (int)AppointmentStatus.Completed;

        Assert.Equal(ErrorType.Validation, (await _service.UpdateAppointmentAsync(request)).ErrorType);
    }

    [Fact]
    public async Task Update_ConfirmedAppointmentAfterEnd_CanBeCompleted()
    {
        var appointment = await AddAppointmentAsync(await AddOfferAsync(),
            AppointmentStatus.Confirmed, DateTime.UtcNow.AddHours(-2));
        var request = UpdateRequest(appointment);
        request.Status = (int)AppointmentStatus.Completed;

        Assert.True((await _service.UpdateAppointmentAsync(request)).IsSuccess);
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
    }

    [Fact]
    public async Task MissingAppointment_ReturnsNotFoundForReadUpdateAndDelete()
    {
        Assert.Equal(ErrorType.NotFound, (await _service.GetAppointmentByIdAsync(999)).ErrorType);
        Assert.Equal(ErrorType.NotFound,
            (await _service.UpdateAppointmentAsync(new() { Id = 999 })).ErrorType);
        Assert.Equal(ErrorType.NotFound,
            (await _service.DeleteAppointmentAsync(new() { Id = 999 })).ErrorType);
    }

    [Fact]
    public void Validators_RejectInvalidIdsTimesAndStatus()
    {
        Assert.False(new AppointmentCreateRequestValidator().Validate(new AppointmentCreateRequest()).IsValid);
        Assert.False(new AppointmentDeleteRequestValidator().Validate(new AppointmentDeleteRequest()).IsValid);
        var request = new AppointmentUpdateRequest
        {
            Id = 1, OfferId = 1, StartUtc = _start, EndUtc = _start, Status = 999
        };
        var result = new AppointmentUpdateRequestValidator().Validate(request);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.EndUtc));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Status));
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Validators_RejectMissingOrNonUtcEndTime(DateTimeKind kind)
    {
        var create = CreateRequest(1);
        create.EndUtc = kind == DateTimeKind.Utc
            ? default : DateTime.SpecifyKind(create.EndUtc, kind);
        var update = new AppointmentUpdateRequest
        {
            Id = 1, OfferId = 1, StartUtc = create.StartUtc, EndUtc = create.EndUtc
        };

        Assert.Contains(new AppointmentCreateRequestValidator().Validate(create).Errors,
            error => error.PropertyName == nameof(create.EndUtc));
        Assert.Contains(new AppointmentUpdateRequestValidator().Validate(update).Errors,
            error => error.PropertyName == nameof(update.EndUtc));
    }

    [Fact]
    public void UpdateValidator_AllowsPastScheduleForCancellationOrCompletion()
    {
        var request = new AppointmentUpdateRequest
        {
            Id = 1, OfferId = 1, StartUtc = DateTime.UtcNow.AddHours(-2),
            EndUtc = DateTime.UtcNow.AddHours(-1), Status = (int)AppointmentStatus.Completed
        };

        Assert.True(new AppointmentUpdateRequestValidator().Validate(request).IsValid);
        request.Status = (int)AppointmentStatus.Cancelled;
        Assert.True(new AppointmentUpdateRequestValidator().Validate(request).IsValid);
    }

    private AppointmentCreateRequest CreateRequest(int offerId) => new()
    {
        OfferId = offerId, StartUtc = _start, EndUtc = _start.AddHours(1)
    };

    private static AppointmentUpdateRequest UpdateRequest(Appointment appointment) => new()
    {
        Id = appointment.Id, OfferId = appointment.OfferId,
        StartUtc = appointment.StartUtc, EndUtc = appointment.EndUtc, Status = (int)appointment.Status
    };

    private async Task<Offer> AddOfferAsync(
        int customerId = 1, int tradespersonId = 1, OfferStatus status = OfferStatus.Accepted)
    {
        var offer = new Offer
        {
            Status = status, TradespersonProfileId = tradespersonId,
            ServiceRequest = new ServiceRequest { CustomerId = customerId }
        };
        _ctx.Offers.Add(offer);
        await _ctx.SaveChangesAsync();
        return offer;
    }

    private async Task<Appointment> AddAppointmentAsync(
        Offer offer, AppointmentStatus status = AppointmentStatus.Pending, DateTime? start = null)
    {
        var appointment = new Appointment
        {
            OfferId = offer.Id, StartUtc = start ?? _start,
            EndUtc = (start ?? _start).AddHours(1), Status = status
        };
        _ctx.Appointments.Add(appointment);
        await _ctx.SaveChangesAsync();
        return appointment;
    }

    public void Dispose() => _ctx.Dispose();
}
