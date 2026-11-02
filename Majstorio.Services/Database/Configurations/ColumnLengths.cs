namespace Majstorio.Services.Database.Configurations;

// Shared column sizes so the configurations and the validators agree on one set of limits.
public static class ColumnLengths
{
    public const int Name = 100;
    public const int Email = 256;
    public const int Phone = 32;
    public const int Url = 2048;
    public const int Address = 300;
    public const int ShortText = 200;
    public const int LongText = 2000;
    public const int Hash = 512;
    public const int Currency = 3;
    public const int ExternalId = 64;

    public const int MoneyPrecision = 18;
    public const int MoneyScale = 2;
    public const int RatingPrecision = 3;
    public const int RatingScale = 2;
}
