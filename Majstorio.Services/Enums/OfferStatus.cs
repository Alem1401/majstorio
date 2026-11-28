namespace Majstorio.Services.Enums;

public enum OfferStatus
{
    Pending = 0,
    Accepted = 1,
    // 2 was Rejected, which the application never uses; the number stays free so stored values keep their meaning.
    Withdrawn = 3,
    Closed = 4
}
