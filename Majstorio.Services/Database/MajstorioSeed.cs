using Majstorio.Services.Enums;
using Microsoft.EntityFrameworkCore;

namespace Majstorio.Services.Database;

// Test data written by the migrations. Every value is fixed, so a new migration only changes when this file does.
// All seeded accounts use the password "test"; the salts and hashes were produced with CryptoService,
// the same hasher that login uses.
internal static class MajstorioSeed
{
    private static readonly DateTime CreatedAt = Utc(2026, 9, 1);

    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedReferenceData(modelBuilder);
        SeedUsersAndTradespeople(modelBuilder);
        SeedServiceRequests(modelBuilder);
        SeedPaymentsAndReviews(modelBuilder);
        SeedCommunication(modelBuilder);
    }

    private static void SeedReferenceData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>().HasData(
            new City { Id = 1, Name = "Mostar", IsActive = true, CreatedAtUtc = CreatedAt },
            new City { Id = 2, Name = "Sarajevo", IsActive = true, CreatedAtUtc = CreatedAt },
            new City { Id = 3, Name = "Tuzla", IsActive = true, CreatedAtUtc = CreatedAt },
            new City { Id = 4, Name = "Zenica", IsActive = true, CreatedAtUtc = CreatedAt },
            new City { Id = 5, Name = "Banja Luka", IsActive = true, CreatedAtUtc = CreatedAt },
            new City { Id = 6, Name = "Bihać", IsActive = false, CreatedAtUtc = CreatedAt });

        modelBuilder.Entity<ServiceCategory>().HasData(
            new ServiceCategory { Id = 1, Name = "Vodoinstalaterski radovi", IsActive = true, CreatedAtUtc = CreatedAt },
            new ServiceCategory { Id = 2, Name = "Elektroinstalacije", IsActive = true, CreatedAtUtc = CreatedAt },
            new ServiceCategory { Id = 3, Name = "Molersko-farbarski radovi", IsActive = true, CreatedAtUtc = CreatedAt },
            new ServiceCategory { Id = 4, Name = "Stolarija", IsActive = true, CreatedAtUtc = CreatedAt },
            new ServiceCategory { Id = 5, Name = "Klima uređaji", IsActive = true, CreatedAtUtc = CreatedAt });
    }

    private static void SeedUsersAndTradespeople(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>().HasData(
            User(1, "desktop", "Admin", "Majstorio", UserRole.Administrator,
                "F72m7JpkIi/pftay5kpuaw==", "bA0l8+egVg1IRJnGrA7MuYQVBWcEOwuxBVeq4m2pmSM="),
            User(2, "mobile", "Amra", "Hadžić", UserRole.Customer,
                "YmL3CeHyHDrDvWhx7TXdbw==", "fnl1ny77TbHjSENxsfvBa+48Q4XhB+YnP32zzxOYuNw="),
            User(3, "tradesperson", "Emir", "Kovačević", UserRole.Tradesperson,
                "gKsYEZzG4YZNSrSaiUKwqQ==", "EQIsPOK/a3O9k29KlSItBdCICHvpgrz30kyIEAR9PDk="),
            User(4, "electrician", "Dino", "Begić", UserRole.Tradesperson,
                "YW/z0gf9C7JnAr2032vFzg==", "DYcQVjcwWcuZLjC7nqMQGXUG/D+sRuhW2bLwzzuAO6I="),
            User(5, "customer", "Lejla", "Mehić", UserRole.Customer,
                "UKDX3zrY6A1rofazD2c/KA==", "La6UY7FrdnVlFH21cN5F5e/nretkhhDwM1vKpcMeoUk="),
            // Has applied to become a tradesperson; the role changes only after approval.
            User(6, "applicant", "Haris", "Delić", UserRole.Customer,
                "HkdMEb3eCtO/5Ne5JASwMw==", "73OMtiRTAG6eLAJadOSMH4qbRt6zA1qBDDsGuCiCpXw="));

        modelBuilder.Entity<TradespersonProfile>().HasData(
            new TradespersonProfile
            {
                Id = 1, UserId = 3, CityId = 1, IsActive = true, CreatedAtUtc = CreatedAt,
                Description = "Vodoinstalater s 12 godina iskustva. Popravke, zamjena bojlera i servis klima uređaja.",
                IsApproved = true, ApprovedByUserId = 1, ApprovedAtUtc = Utc(2026, 9, 2)
            },
            new TradespersonProfile
            {
                Id = 2, UserId = 4, CityId = 2, IsActive = true, CreatedAtUtc = CreatedAt,
                Description = "Licencirani električar. Instalacije, rasvjeta i zamjena utičnica.",
                IsApproved = true, ApprovedByUserId = 1, ApprovedAtUtc = Utc(2026, 9, 2)
            },
            new TradespersonProfile
            {
                Id = 3, UserId = 6, CityId = 1, IsActive = true, CreatedAtUtc = Utc(2026, 10, 28),
                Description = "Moler i farbar, unutrašnji i vanjski radovi.",
                IsApproved = false
            });

        modelBuilder.Entity<TradespersonService>().HasData(
            Service(1, 1, 1, "Popravka slavina i bojlera", "Otklanjanje curenja, zamjena slavina i bojlera.", 30, 300, 90),
            Service(2, 1, 5, "Servis klima uređaja", "Čišćenje, dopuna plina i redovni servis.", 50, 150, 60),
            Service(3, 2, 2, "Zamjena utičnica i prekidača", "Zamjena i ugradnja utičnica, prekidača i osigurača.", 20, 80, 45),
            Service(4, 2, 2, "Ugradnja rasvjete", "Montaža lustera, LED traka i vanjske rasvjete.", 40, 200, 120),
            Service(5, 3, 3, "Krečenje stanova", "Gletovanje i krečenje prostorija.", 100, 600, 480));
    }

    private static void SeedServiceRequests(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceRequest>().HasData(
            // Open request still collecting offers.
            new ServiceRequest
            {
                Id = 1, CustomerId = 2, CityId = 1, ServiceCategoryId = 1, Status = RequestStatus.Created,
                Description = "Curi slavina u kuhinji, potrebna zamjena.", Address = "Kralja Tvrtka 12",
                DesiredStartUtc = Utc(2026, 12, 10), MaximumBudget = 100, CreatedAtUtc = Utc(2026, 11, 1)
            },
            // Offer accepted and appointment confirmed.
            new ServiceRequest
            {
                Id = 2, CustomerId = 2, CityId = 1, ServiceCategoryId = 1, Status = RequestStatus.AppointmentConfirmed,
                Description = "Zamjena bojlera od 80 litara.", Address = "Kralja Tvrtka 12",
                DesiredStartUtc = Utc(2026, 12, 5), MaximumBudget = 300, CreatedAtUtc = Utc(2026, 10, 25)
            },
            // Finished, paid and reviewed.
            new ServiceRequest
            {
                Id = 3, CustomerId = 5, CityId = 2, ServiceCategoryId = 2, Status = RequestStatus.Completed,
                Description = "Ugradnja lustera u dnevnom boravku.", Address = "Titova 45",
                DesiredStartUtc = Utc(2026, 10, 10), MaximumBudget = 150, CreatedAtUtc = Utc(2026, 10, 1)
            },
            // Paid and then cancelled, so the payment was fully refunded.
            new ServiceRequest
            {
                Id = 4, CustomerId = 2, CityId = 1, ServiceCategoryId = 5, Status = RequestStatus.Cancelled,
                Description = "Godišnji servis klima uređaja.", Address = "Kralja Tvrtka 12",
                DesiredStartUtc = Utc(2026, 10, 20), MaximumBudget = 120, CreatedAtUtc = Utc(2026, 10, 12)
            });

        modelBuilder.Entity<Offer>().HasData(
            new Offer
            {
                Id = 1, ServiceRequestId = 1, TradespersonProfileId = 1, Price = 80, DurationMinutes = 60,
                ProposedStartUtc = Utc(2026, 12, 10), Status = OfferStatus.Pending, CreatedAtUtc = Utc(2026, 11, 2)
            },
            new Offer
            {
                Id = 2, ServiceRequestId = 2, TradespersonProfileId = 1, Price = 250, DurationMinutes = 120,
                ProposedStartUtc = Utc(2026, 12, 5), Status = OfferStatus.Accepted, CreatedAtUtc = Utc(2026, 10, 26),
                DecidedByUserId = 2, DecidedAtUtc = Utc(2026, 10, 27), DecisionReason = "Najbolja ponuda."
            },
            new Offer
            {
                Id = 3, ServiceRequestId = 3, TradespersonProfileId = 2, Price = 120, DurationMinutes = 120,
                ProposedStartUtc = Utc(2026, 10, 10), Status = OfferStatus.Accepted, CreatedAtUtc = Utc(2026, 10, 2),
                DecidedByUserId = 5, DecidedAtUtc = Utc(2026, 10, 3)
            },
            new Offer
            {
                Id = 4, ServiceRequestId = 4, TradespersonProfileId = 1, Price = 90, DurationMinutes = 60,
                ProposedStartUtc = Utc(2026, 10, 20), Status = OfferStatus.Accepted, CreatedAtUtc = Utc(2026, 10, 13),
                DecidedByUserId = 2, DecidedAtUtc = Utc(2026, 10, 14)
            });

        modelBuilder.Entity<Appointment>().HasData(
            new Appointment
            {
                Id = 1, OfferId = 2, Status = AppointmentStatus.Confirmed, CreatedAtUtc = Utc(2026, 10, 27),
                StartUtc = Utc(2026, 12, 5), EndUtc = Utc(2026, 12, 5, 11)
            },
            new Appointment
            {
                Id = 2, OfferId = 3, Status = AppointmentStatus.Completed, CreatedAtUtc = Utc(2026, 10, 3),
                StartUtc = Utc(2026, 10, 10), EndUtc = Utc(2026, 10, 10, 11)
            },
            new Appointment
            {
                Id = 3, OfferId = 4, Status = AppointmentStatus.Cancelled, CreatedAtUtc = Utc(2026, 10, 14),
                StartUtc = Utc(2026, 10, 20), EndUtc = Utc(2026, 10, 20, 10)
            });

        modelBuilder.Entity<StatusHistory>().HasData(
            History(1, 1, null, 2, null, RequestStatus.Created, Utc(2026, 11, 1)),
            History(2, 2, null, 2, null, RequestStatus.Created, Utc(2026, 10, 25)),
            History(3, 2, null, 2, RequestStatus.Created, RequestStatus.OfferAccepted, Utc(2026, 10, 27)),
            History(4, 2, 1, 3, RequestStatus.OfferAccepted, RequestStatus.AppointmentConfirmed, Utc(2026, 10, 27, 12),
                AppointmentStatus.Pending, AppointmentStatus.Confirmed),
            History(5, 3, null, 5, null, RequestStatus.Created, Utc(2026, 10, 1)),
            History(6, 3, null, 5, RequestStatus.Created, RequestStatus.OfferAccepted, Utc(2026, 10, 3)),
            History(7, 3, 2, 4, RequestStatus.OfferAccepted, RequestStatus.AppointmentConfirmed, Utc(2026, 10, 3, 12),
                AppointmentStatus.Pending, AppointmentStatus.Confirmed),
            History(8, 3, 2, 4, RequestStatus.AppointmentConfirmed, RequestStatus.Completed, Utc(2026, 10, 10, 11),
                AppointmentStatus.Confirmed, AppointmentStatus.Completed),
            History(9, 4, null, 2, null, RequestStatus.Created, Utc(2026, 10, 12)),
            History(10, 4, null, 2, RequestStatus.Created, RequestStatus.OfferAccepted, Utc(2026, 10, 14)),
            History(11, 4, 3, 3, RequestStatus.OfferAccepted, RequestStatus.AppointmentConfirmed, Utc(2026, 10, 14, 12),
                AppointmentStatus.Pending, AppointmentStatus.Confirmed),
            History(12, 4, 3, 2, RequestStatus.AppointmentConfirmed, RequestStatus.Cancelled, Utc(2026, 10, 18),
                AppointmentStatus.Confirmed, AppointmentStatus.Cancelled, "Kupac je otkazao termin."));
    }

    private static void SeedPaymentsAndReviews(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>().HasData(
            new Payment
            {
                Id = 1, AppointmentId = 2, Amount = 120, CapturedAmount = 120, Currency = "EUR",
                Status = PaymentStatus.Paid, PayPalOrderId = "SEED-ORDER-0001", PayPalCaptureId = "SEED-CAPTURE-0001",
                CreateOrderRequestId = new Guid("6f1c2a4e-0a51-4c8e-9d7b-1a2b3c4d5e01"),
                CaptureRequestId = new Guid("6f1c2a4e-0a51-4c8e-9d7b-1a2b3c4d5e02"),
                PaidAtUtc = Utc(2026, 10, 10, 12), CreatedAtUtc = Utc(2026, 10, 10, 11)
            },
            new Payment
            {
                Id = 2, AppointmentId = 3, Amount = 90, CapturedAmount = 90, Currency = "EUR",
                Status = PaymentStatus.Paid, PayPalOrderId = "SEED-ORDER-0002", PayPalCaptureId = "SEED-CAPTURE-0002",
                CreateOrderRequestId = new Guid("6f1c2a4e-0a51-4c8e-9d7b-1a2b3c4d5e03"),
                CaptureRequestId = new Guid("6f1c2a4e-0a51-4c8e-9d7b-1a2b3c4d5e04"),
                PaidAtUtc = Utc(2026, 10, 15), CreatedAtUtc = Utc(2026, 10, 15)
            });

        modelBuilder.Entity<Refund>().HasData(
            new Refund
            {
                Id = 1, PaymentId = 2, RequestedByUserId = 1, Amount = 90, Status = RefundStatus.Completed,
                Reason = "Termin je otkazan prije izvođenja radova.", PayPalRefundId = "SEED-REFUND-0001",
                IdempotencyKey = new Guid("6f1c2a4e-0a51-4c8e-9d7b-1a2b3c4d5e05"),
                ProcessedAtUtc = Utc(2026, 10, 18, 12), CreatedAtUtc = Utc(2026, 10, 18)
            });

        modelBuilder.Entity<Review>().HasData(
            new Review
            {
                Id = 1, AppointmentId = 2, TradespersonProfileId = 2, CustomerId = 5, Rating = 5,
                Comment = "Brzo, uredno i tačno na vrijeme.", CreatedAtUtc = Utc(2026, 10, 11)
            });
    }

    private static void SeedCommunication(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>().HasData(
            new Notification
            {
                Id = 1, UserId = 2, ServiceRequestId = 2, IsRead = true, CreatedAtUtc = Utc(2026, 10, 27, 12),
                EventId = new Guid("8a7b6c5d-1e2f-4a3b-8c9d-0e1f2a3b4c01"),
                Title = "Termin potvrđen", Message = "Termin za zamjenu bojlera potvrđen je za 5. decembar u 9:00."
            },
            new Notification
            {
                Id = 2, UserId = 3, ServiceRequestId = 1, IsRead = false, CreatedAtUtc = Utc(2026, 11, 1),
                EventId = new Guid("8a7b6c5d-1e2f-4a3b-8c9d-0e1f2a3b4c02"),
                Title = "Novi zahtjev u vašoj kategoriji", Message = "Kupac u Mostaru traži zamjenu slavine."
            },
            new Notification
            {
                Id = 3, UserId = 5, ServiceRequestId = 3, IsRead = false, CreatedAtUtc = Utc(2026, 10, 10, 11),
                EventId = new Guid("8a7b6c5d-1e2f-4a3b-8c9d-0e1f2a3b4c03"),
                Title = "Usluga završena", Message = "Ugradnja lustera je završena. Ocijenite majstora."
            },
            new Notification
            {
                Id = 4, UserId = 2, ServiceRequestId = 4, IsRead = false, CreatedAtUtc = Utc(2026, 10, 18, 12),
                EventId = new Guid("8a7b6c5d-1e2f-4a3b-8c9d-0e1f2a3b4c04"),
                Title = "Povrat novca izvršen", Message = "Iznos od 90 EUR vraćen je na vaš PayPal račun."
            });

        modelBuilder.Entity<NewsPost>().HasData(
            new NewsPost
            {
                Id = 1, AuthorId = 1, CreatedAtUtc = Utc(2026, 10, 1), PublishedAtUtc = Utc(2026, 10, 1),
                Title = "Dobrodošli u Majstorio",
                Content = "Pronađite provjerenog majstora u svom gradu, uporedite ponude i dogovorite termin na jednom mjestu.",
                ImageUrl = "https://picsum.photos/seed/majstorio-welcome/800/450"
            },
            new NewsPost
            {
                Id = 2, AuthorId = 1, CreatedAtUtc = Utc(2026, 10, 20), PublishedAtUtc = Utc(2026, 10, 20),
                Title = "Pripremite klima uređaj za zimu",
                Content = "Redovan servis klima uređaja prije zime smanjuje potrošnju struje i produžava vijek uređaja.",
                ImageUrl = "https://picsum.photos/seed/majstorio-ac/800/450"
            });

        modelBuilder.Entity<SearchLog>().HasData(
            new SearchLog
            {
                Id = 1, UserId = 2, CityId = 1, ServiceCategoryId = 1, MinimumRating = 4,
                SearchedAtUtc = Utc(2026, 10, 24), CreatedAtUtc = Utc(2026, 10, 24)
            },
            new SearchLog
            {
                Id = 2, UserId = 2, CityId = 1, ServiceCategoryId = 5, Query = "klima",
                SearchedAtUtc = Utc(2026, 10, 11), CreatedAtUtc = Utc(2026, 10, 11)
            },
            new SearchLog
            {
                Id = 3, UserId = 5, CityId = 2, ServiceCategoryId = 2, Query = "luster",
                SearchedAtUtc = Utc(2026, 9, 30), CreatedAtUtc = Utc(2026, 9, 30)
            });
    }

    private static ApplicationUser User(
        int id, string userName, string firstName, string lastName, UserRole role, string salt, string hash) => new()
    {
        Id = id,
        UserName = userName,
        Email = $"{userName}@majstorio.test",
        FirstName = firstName,
        LastName = lastName,
        Role = role,
        IsActive = true,
        PasswordSalt = salt,
        PasswordHash = hash,
        CreatedAtUtc = CreatedAt
    };

    private static TradespersonService Service(
        int id, int profileId, int categoryId, string name, string description,
        decimal minimumPrice, decimal maximumPrice, int durationMinutes) => new()
    {
        Id = id,
        TradespersonProfileId = profileId,
        ServiceCategoryId = categoryId,
        Name = name,
        Description = description,
        MinimumPrice = minimumPrice,
        MaximumPrice = maximumPrice,
        EstimatedDurationMinutes = durationMinutes,
        IsActive = true,
        CreatedAtUtc = CreatedAt
    };

    private static StatusHistory History(
        int id, int requestId, int? appointmentId, int changedByUserId,
        RequestStatus? previousStatus, RequestStatus newStatus, DateTime changedAtUtc,
        AppointmentStatus? previousAppointmentStatus = null, AppointmentStatus? newAppointmentStatus = null,
        string? note = null) => new()
    {
        Id = id,
        ServiceRequestId = requestId,
        AppointmentId = appointmentId,
        ChangedByUserId = changedByUserId,
        PreviousStatus = previousStatus,
        NewStatus = newStatus,
        PreviousAppointmentStatus = previousAppointmentStatus,
        NewAppointmentStatus = newAppointmentStatus,
        Note = note,
        ChangedAtUtc = changedAtUtc,
        CreatedAtUtc = changedAtUtc
    };

    private static DateTime Utc(int year, int month, int day, int hour = 9) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
}
