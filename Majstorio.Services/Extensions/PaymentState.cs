using Majstorio.Model.Responses.Appointment;
using Majstorio.Services.Database;
using Majstorio.Services.Enums;

namespace Majstorio.Services.Extensions;

public static class PaymentState
{
    // The appointment must be loaded with its payments and their refunds.
    public static void Fill(AppointmentResponse response, Appointment appointment)
    {
        var paid = appointment.Payments.FirstOrDefault(payment => payment.Status == PaymentStatus.Paid);
        response.IsPaid = paid != null;
        response.IsRefunded = paid?.Refund != null;
    }
}
