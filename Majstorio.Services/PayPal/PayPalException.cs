namespace Majstorio.Services.PayPal;

// PayPal refused the call or could not be reached.
public class PayPalException : Exception
{
    public PayPalException(string message) : base(message)
    {
    }
}
